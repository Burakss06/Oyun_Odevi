using UnityEngine;
using System.Collections.Generic;

public class BoxController : MonoBehaviour
{
    public enum BoxShape
    {
        Closed,
        Opened,
        Unfolded
    }

    public enum DefectType
    {
        None,          // Sağlam (Kusursuz)
        BarcodeAnomaly,// Hatalı Barkod
        WrongColor,    // Yanlış Renk
        SizeAnomaly    // Boyut Hatası (Çok büyük veya çok küçük)
    }

    [Header("Kusur Bilgisi")]
    [SerializeField] private DefectType currentDefect = DefectType.None;
    public DefectType CurrentDefect => currentDefect;
    
    public BoxShape Shape { get; set; } = BoxShape.Closed;
    public bool isMysteryBox = false;
    public string BarcodeNumber { get; private set; } = "";

    [Header("Ağırlık Bilgisi")]
    [SerializeField] private float weight = 5.0f;
    public float Weight => weight;

    public bool IsDefective
    {
        get
        {
            // Eğer oyun kuralları atanmamışsa varsayılan
            if (GameManager.Instance == null || GameManager.Instance.DailyRules == null)
            {
                return currentDefect != DefectType.None;
            }

            // Sürpriz kutular banttan düşerse hata sayılmasın, oyuncu palete koyup şansını denemelidir
            if (isMysteryBox) return false;

            // Kutu şekli kurala göre Ret'e mi gitmeli?
            bool shouldGoToRetByShape = false;
            if (GameManager.Instance.DailyRules.TryGetValue(Shape, out var targetPallet))
            {
                shouldGoToRetByShape = (targetPallet == PalletTrigger.PalletType.Ret);
            }

            // Aktif renk ve boyut hataları da her zaman Ret'e gitmelidir
            bool hasActiveDefect = false;
            if (DayManager.Instance != null)
            {
                DayConfig config = DayManager.Instance.GetCurrentDayConfig();
                if (config.allowBarcodeDefect && currentDefect == DefectType.BarcodeAnomaly) hasActiveDefect = true;
                if (config.allowWrongColorDefect && currentDefect == DefectType.WrongColor) hasActiveDefect = true;
                if (config.allowSizeAnomalyDefect && currentDefect == DefectType.SizeAnomaly) hasActiveDefect = true;
                // Ağırlık hatası: 10 kg ve üzeri kutular defolu sayılır
                if (config.allowWeightDefect && weight >= 10.0f) hasActiveDefect = true;
            }

            return shouldGoToRetByShape || hasActiveDefect;
        }
    }

    [Header("Görsel Efekt Ayarları")]
    [SerializeField] private Color wrongColorTint = new Color(0.1f, 0.9f, 0.1f); // Yanlış renk kuralı artık yeşil (oyuncuyu ters köşe yapmak için)

    private bool isEvaluated = false; // Palete konup değerlendirildi mi?
    public bool IsEvaluated => isEvaluated;
    private Vector3 originalScale;
    private Rigidbody rb;

    public static bool IsSharpEyeActive = false;
    private bool isHighlighted = false;
    private static GameObject[] officePropsCache;

    private void Awake()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
    }

    public void InitializeBox(DayConfig config)
    {
        if (isMysteryBox)
        {
            currentDefect = DefectType.None;
        }
        else
        {
            // Kutunun defolu olup olmayacağına karar ver
            float randomVal = Random.value;
            if (randomVal <= config.defectSpawnChance)
            {
                // O gün aktif olan kusur türlerini listele
                List<DefectType> allowedDefects = new List<DefectType>();
                if (config.allowBarcodeDefect) allowedDefects.Add(DefectType.BarcodeAnomaly);
                if (config.allowWrongColorDefect) allowedDefects.Add(DefectType.WrongColor);
                if (config.allowSizeAnomalyDefect) allowedDefects.Add(DefectType.SizeAnomaly);

                if (allowedDefects.Count > 0)
                {
                    // Aktif kusurlardan birini rastgele seç
                    currentDefect = allowedDefects[Random.Range(0, allowedDefects.Count)];
                }
                else
                {
                    currentDefect = DefectType.None;
                }
            }
            else
            {
                currentDefect = DefectType.None;
            }
        }

        // Ağırlık ataması
        if (config.allowWeightDefect)
        {
            // 6. gün ve sonrası: %40 ağır kutu, %60 normal kutu
            if (Random.value < 0.4f)
            {
                weight = Random.Range(10.0f, 15.0f); // Ağır (defolu)
            }
            else
            {
                weight = Random.Range(3.0f, 9.9f); // Normal
            }
        }
        else
        {
            weight = Random.Range(3.0f, 8.0f); // Standart günler
        }

        // Barkod günü aktifse tüm kutulara barkod numarası ata
        if (config.allowBarcodeDefect && GameManager.Instance != null)
        {
            string validNum = GameManager.Instance.ValidBarcodeNumber;
            if (currentDefect == DefectType.BarcodeAnomaly)
            {
                // Hatalı kutu: geçerli numaradan farklı rastgele 7 haneli numara
                string invalid;
                do { invalid = Random.Range(1000000, 9999999).ToString(); }
                while (invalid == validNum);
                BarcodeNumber = invalid;
            }
            else
            {
                // Sağlam kutu: geçerli barkod numarası
                BarcodeNumber = validNum;
            }
        }

        // Kutu kapağı açıksa (Opened veya Unfolded), içini kodla doldur
        // Bunu görsel kusur uygulanmadan ÖNCE yapıyoruz, böylece boyut hatası (SizeAnomaly)
        // tüm kutuyu ve içindeki eşyaları orantılı olarak küçültür/büyütür, eşyalar taşmaz.
        if (Shape == BoxShape.Opened || Shape == BoxShape.Unfolded)
        {
            FillBoxWithOfficeProps();
        }

        if (Shape == BoxShape.Unfolded)
        {
            // Kutunun altını kapatma işlemini kod yerine Unity üzerinden manuel Quad ekleyerek çözeceğiz.
        }

        // Kusurun görsel etkilerini uygula
        ApplyVisualDefect();
    }

    private void CreateBarcodeUI()
    {
        // Kutunun gerçek fiziksel boyutlarını al (Renderer'dan) - Bu yöntem daha önce doğru çalışıyordu
        Renderer boxRenderer = GetComponentInChildren<Renderer>();
        Bounds bounds = (boxRenderer != null) ? boxRenderer.bounds : new Bounds(transform.position, Vector3.one);

        // Yerel Z boyutu ile ön yüzeye tam yapış
        float halfDepthLocal = bounds.extents.z / transform.lossyScale.z;

        // TextMesh nesnesi oluştur
        GameObject labelObj = new GameObject("BarcodeLabel");
        labelObj.transform.SetParent(transform);

        // Sağ alt köşeye yerleştir (X: sağa, Y: aşağıya doğru kaydır)
        float halfWidthLocal  = bounds.extents.x / transform.lossyScale.x;
        float halfHeightLocal = bounds.extents.y / transform.lossyScale.y;
        
        // İlk çalışan versiyondaki pozisyon hesabı
        labelObj.transform.localPosition = new Vector3(
            halfWidthLocal  * 0.55f,   // sağ
           -halfHeightLocal * 0.55f,   // aşağı
           -(halfDepthLocal + 0.001f)  // ön yüzey
        );
        labelObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        labelObj.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.1f;
        tm.fontSize = 35; // Kullanıcı biraz daha büyük istemişti, 26'dan 35'e çıkardım

        // Tüm barkodlar aynı renk ve aynı format - sadece numara farklı
        string displayNum = (BarcodeNumber != "") ? BarcodeNumber : Random.Range(1000000, 9999999).ToString();
        tm.text = "|| | ||| | ||\n" + displayNum;
        tm.color = Color.black; // Tam siyah

        MeshRenderer meshRen = labelObj.GetComponent<MeshRenderer>();
        if (meshRen != null)
        {
            // Derinlik testini düzelten custom shader'ı uygula
            Shader depthShader = Shader.Find("Custom/TextDepthTested");
            if (depthShader != null)
            {
                Material mat = new Material(meshRen.sharedMaterial);
                mat.shader = depthShader;
                meshRen.material = mat;
            }
            
            meshRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRen.receiveShadows = false;
        }
    }

    private void CreateMysteryUI()
    {
        Renderer boxRenderer = GetComponentInChildren<Renderer>();
        Bounds bounds = (boxRenderer != null) ? boxRenderer.bounds : new Bounds(transform.position, Vector3.one);

        float halfX = bounds.extents.x / transform.lossyScale.x;
        float halfY = bounds.extents.y / transform.lossyScale.y;
        float halfZ = bounds.extents.z / transform.lossyScale.z;

        // 4 yüzey için pozisyon ve rotasyonlar (Ön, Arka, Sol, Sağ)
        // Barkoda denk gelmemesi için yükseklik merkezde (0), barkod ise aşağıda (-0.55).
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0, -(halfZ + 0.001f)), // Ön
            new Vector3(0, 0, (halfZ + 0.001f)),  // Arka
            new Vector3(-(halfX + 0.001f), 0, 0), // Sol
            new Vector3((halfX + 0.001f), 0, 0)   // Sağ
        };
        
        Vector3[] rotations = new Vector3[]
        {
            new Vector3(0, 0, 0),    // Ön
            new Vector3(0, 180, 0),  // Arka
            new Vector3(0, 90, 0),   // Sol (düzeltildi)
            new Vector3(0, -90, 0)   // Sağ (düzeltildi)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject qMark = new GameObject("MysteryMark_" + i);
            qMark.transform.SetParent(transform);
            
            qMark.transform.localPosition = positions[i];
            qMark.transform.localRotation = Quaternion.Euler(rotations[i]);
            qMark.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            TextMesh tm = qMark.AddComponent<TextMesh>();
            tm.text = "?";
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.1f;
            tm.fontSize = 120; // Devasa ve dikkat çekici
            tm.fontStyle = FontStyle.Bold;
            
            // Renk değiştiren Gökkuşağı efektini ekle
            qMark.AddComponent<RainbowTextEffect>();

            MeshRenderer meshRen = qMark.GetComponent<MeshRenderer>();
            if (meshRen != null)
            {
                Shader depthShader = Shader.Find("Custom/TextDepthTested");
                if (depthShader != null)
                {
                    Material mat = new Material(meshRen.sharedMaterial);
                    mat.shader = depthShader;
                    meshRen.material = mat;
                }
                meshRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRen.receiveShadows = false;
            }
        }
    }

    private void FillBoxWithOfficeProps()
    {
        if (officePropsCache == null)
        {
            officePropsCache = Resources.LoadAll<GameObject>("OfficeProps");
        }

        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = GetComponentInChildren<BoxCollider>();
        if (col == null) return;

        GameObject contentsRoot = new GameObject("Contents");
        contentsRoot.transform.SetParent(transform, false);
        contentsRoot.transform.localPosition = col.center;

        if (officePropsCache == null || officePropsCache.Length == 0)
        {
            Debug.LogWarning("OfficeProps not found in Resources! Returning.");
            return;
        }

        int itemCount = 1; // Sadece 1 nesne koy (iç içe geçmeyi önlemek için)
        Vector3 innerExtents = new Vector3(0.20f, 0.16f, 0.13f); 
        float bottomY = -innerExtents.y + 0.02f;

        for (int i = 0; i < itemCount; i++)
        {
            GameObject prefab;
            do
            {
                prefab = officePropsCache[Random.Range(0, officePropsCache.Length)];
            } 
            while (prefab.name.ToLower().Contains("book")); // Kitapları eledik
            
            int spawnCount = 1;

            for (int s = 0; s < spawnCount; s++)
            {
                GameObject item = Instantiate(prefab, contentsRoot.transform, false);
                
                // Remove all colliders so it doesn't affect physics
                Collider[] cols = item.GetComponentsInChildren<Collider>();
                foreach (Collider c in cols) Destroy(c);

                item.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // Calculate bounds to scale appropriately
                Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                
                // Fix Pink Material (URP Upgrade)
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null) urpShader = Shader.Find("Standard");
                
                Color[] colors = {
                    new Color(0.2f, 0.4f, 0.8f), new Color(0.8f, 0.2f, 0.2f),
                    new Color(0.2f, 0.7f, 0.3f), new Color(0.9f, 0.7f, 0.1f),
                    new Color(0.6f, 0.2f, 0.8f), new Color(1.0f, 0.5f, 0.0f),
                    Color.white, Color.gray
                };
                Color randomColor = colors[Random.Range(0, colors.Length)];

                foreach (Renderer r in renderers)
                {
                    if (r != null && r.sharedMaterials != null && r.sharedMaterials.Length > 0)
                    {
                        Material[] newMaterials = new Material[r.sharedMaterials.Length];
                        for (int k = 0; k < r.sharedMaterials.Length; k++)
                        {
                            Material oldMat = r.sharedMaterials[k];
                            Material newMat = new Material(urpShader);
                            
                            if (oldMat != null && oldMat.HasProperty("_BaseMap") && oldMat.GetTexture("_BaseMap") != null)
                            {
                                newMat.SetTexture("_BaseMap", oldMat.GetTexture("_BaseMap"));
                                newMat.mainTexture = oldMat.GetTexture("_BaseMap");
                            }
                            else if (oldMat != null && oldMat.HasProperty("_MainTex") && oldMat.mainTexture != null)
                            {
                                newMat.mainTexture = oldMat.mainTexture;
                                newMat.SetTexture("_BaseMap", oldMat.mainTexture);
                            }
                            else
                            {
                                newMat.color = randomColor;
                            }
                            newMaterials[k] = newMat;
                        }
                        r.materials = newMaterials;
                    }
                }

                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);

                    float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (maxDim > 0.001f)
                    {
                        float targetSize = Random.Range(0.24f, 0.28f); 
                        float scaleFactor = targetSize / maxDim;
                        item.transform.localScale = new Vector3(
                            item.transform.localScale.x * scaleFactor, 
                            item.transform.localScale.y * scaleFactor * Random.Range(1.4f, 1.8f), 
                            item.transform.localScale.z * scaleFactor
                        );
                    }

                    // Recalculate bounds after scaling to find the bottom
                    bounds = renderers[0].bounds;
                    for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);

                    // Offset to place the bottom of the mesh at bottomY
                    float worldOffsetToBottom = item.transform.position.y - bounds.min.y;
                    float localYOffset = worldOffsetToBottom / transform.lossyScale.y;

                    float posX = Random.Range(-0.04f, 0.04f);
                    float posZ = Random.Range(-0.04f, 0.04f);

                    item.transform.localPosition = new Vector3(posX, bottomY + localYOffset, posZ);
                }
            }
        }
    }

    private void FillBoxWithItems()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = GetComponentInChildren<BoxCollider>();
        if (col == null) return;

        GameObject contentsRoot = new GameObject("Contents");
        contentsRoot.transform.SetParent(transform, false);
        contentsRoot.transform.localPosition = col.center;

        int itemCount = Random.Range(4, 8); // Daha fazla eşya, daha dolu görünsün
        
        // ÖNEMLİ DÜZELTME: Açık kutuların collider'ı kapakları da içerdiği için dışarı taşıyorlardı.
        // Bu yüzden standart bir kutunun gerçek iç hacmini (extents) sabit olarak tanımlıyoruz.
        Vector3 innerExtents = new Vector3(0.20f, 0.16f, 0.13f); 
        float bottomY = -innerExtents.y + 0.02f; // Kutunun tabanı (çok az yukarıdan başlat)

        Color[] colors = {
            new Color(0.2f, 0.4f, 0.8f),
            new Color(0.8f, 0.2f, 0.2f),
            new Color(0.2f, 0.7f, 0.3f),
            new Color(0.9f, 0.7f, 0.1f),
            new Color(0.6f, 0.2f, 0.8f),
            new Color(1.0f, 0.5f, 0.0f)
        };

        System.Collections.Generic.List<Bounds> placedItems = new System.Collections.Generic.List<Bounds>();

        for (int i = 0; i < itemCount; i++)
        {
            PrimitiveType type = (Random.value > 0.5f) ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            
            // Eşyaları oransal değil sabit ebatlarla belirledik, böylece hem dolgun hem gerçekçi olacak
            float scaleX = Random.Range(0.12f, 0.18f);
            float scaleY = Random.Range(0.08f, 0.14f); // Eskisinden çok daha kalın ve dolgun
            float scaleZ = Random.Range(0.12f, 0.18f);

            float maxRadius = Mathf.Max(scaleX, scaleZ) * 0.75f; 

            // İç duvardan güvenli mesafe
            float maxPosX = Mathf.Max(0, innerExtents.x - maxRadius);
            float maxPosZ = Mathf.Max(0, innerExtents.z - maxRadius);

            bool isPlaced = false;
            Vector3 finalPos = Vector3.zero;
            Bounds proposedBounds = new Bounds();

            // Çakışmayan bir yer bulmak için en fazla 25 kez dene
            for (int attempt = 0; attempt < 25; attempt++)
            {
                float posX = Random.Range(-maxPosX, maxPosX);
                float posZ = Random.Range(-maxPosZ, maxPosZ);
                
                // Kutunun daha dolu görünmesi için eşyaların üst üste binmesine olanak tanı (Y ekseninde)
                float posY = bottomY + (scaleY * 0.5f) + Random.Range(0f, 0.15f);

                finalPos = new Vector3(posX, posY, posZ);
                // Çarpışma kutusunu biraz daralttık ki eşyalar birbirine iyice yanaşsın, kutu dolsun
                proposedBounds = new Bounds(finalPos, new Vector3(scaleX, scaleY, scaleZ) * 1.05f); 

                bool overlaps = false;
                foreach (Bounds b in placedItems)
                {
                    if (b.Intersects(proposedBounds))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    isPlaced = true;
                    placedItems.Add(proposedBounds);
                    break;
                }
            }

            // Eğer sığmazsa bu objeyi atla
            if (!isPlaced) continue;

            GameObject item = GameObject.CreatePrimitive(type);
            Destroy(item.GetComponent<Collider>());
            item.transform.SetParent(contentsRoot.transform, false);
            item.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            item.transform.localPosition = finalPos;
            
            item.transform.localRotation = Quaternion.Euler(
                (type == PrimitiveType.Cube) ? Random.Range(-10f, 10f) : 90f,
                Random.Range(0f, 360f),
                (type == PrimitiveType.Cube) ? Random.Range(-10f, 10f) : 0f
            );

            MeshRenderer ren = item.GetComponent<MeshRenderer>();
            if (ren != null)
            {
                Material mat = new Material(ren.sharedMaterial);
                mat.color = colors[Random.Range(0, colors.Length)];
                ren.material = mat;
            }
        }
    }

    private void ApplyVisualDefect()
    {
        // Barkod günü ise her kutuya barkod etiketi ekle (Sürpriz kutular hariç)
        if (DayManager.Instance != null && DayManager.Instance.GetCurrentDayConfig().allowBarcodeDefect && !isMysteryBox)
        {
            CreateBarcodeUI();
        }

        // Sürpriz kutu ise özel "?" tasarımlarını ekle
        if (isMysteryBox)
        {
            CreateMysteryUI();
        }

        switch (currentDefect)
        {
            case DefectType.None:
                // Kusursuz kutu
                break;

            case DefectType.BarcodeAnomaly:
                // Barkod hatası CreateBarcodeUI içinde halledildi
                break;

            case DefectType.WrongColor:
                // Yanlış Renk Görünümü:
                // Kutunun tüm mesh renderer'larının materyal renklerini değiştirerek yanlış renge boya
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer ren in renderers)
                {
                    // Çizgi çizen LineRenderer'ları atla
                    if (ren is LineRenderer) continue;

                    // Materyali klonla ve rengi değiştir
                    ren.material.color = wrongColorTint;
                }
                break;

            case DefectType.SizeAnomaly:
                // Boyut Hatası Görünümü:
                // Kutu ya çok küçük (0.70x) ya da çok büyük (1.24x) olmalı (%50 ihtimal)
                // Bu sayede cam tünelden dışarı taşmaz ve bantta takılmaz.
                float scaleMultiplier = (Random.value > 0.5f) ? 0.70f : 1.24f;
                transform.localScale = originalScale * scaleMultiplier;
                
                // Rigidbody kütlesini boyutuna göre güncelle
                if (rb != null)
                {
                    rb.mass *= scaleMultiplier;
                }
                break;
        }
    }

    public void MarkAsEvaluated()
    {
        isEvaluated = true;
    }

    private void ApplyHighlight(bool enable)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            if (ren is LineRenderer) continue;

            if (enable)
            {
                ren.material.EnableKeyword("_EMISSION");
                ren.material.SetColor("_EmissionColor", Color.yellow * 2.5f); // Parlak sarı neon efekti
            }
            else
            {
                ren.material.DisableKeyword("_EMISSION");
                ren.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private void Update()
    {
        // Keskin Göz (Sharp Eye) mantığı: Aktifse ve hatalıysa parlat
        if (IsSharpEyeActive && IsDefective && !isHighlighted)
        {
            isHighlighted = true;
            ApplyHighlight(true);
        }
        else if (!IsSharpEyeActive && isHighlighted)
        {
            isHighlighted = false;
            ApplyHighlight(false);
        }

        // Eğer değerlendirilmediyse ve banttan aşağı veya sahne dışına düştüyse (Y pozisyonu çok düşükse)
        if (!isEvaluated && transform.position.y < -4.0f)
        {
            isEvaluated = true;
            
            if (GameManager.Instance != null)
            {
                if (IsDefective)
                {
                    // Oyuncu hatalı/defolu bir kutuyu kaçırdı! Ceza puanı/hata sayacı artar.
                    GameManager.Instance.AddIncorrectChoice();
                    Debug.Log($"Kusurlu kutu ({currentDefect}) banttan düştü! Hata sayacı arttı.");
                }
                else
                {
                    // Sağlam kutu bant sonuna kadar gitti, bu doğru bir süreç (kaçırma sayılmaz, normal akış)
                    GameManager.Instance.BoxMissed();
                    Debug.Log("Sağlam kutu başarıyla kontrolü geçti ve banttan düştü.");
                }
            }

            // Objeyi yok et
            Destroy(gameObject);
        }
    }
}
