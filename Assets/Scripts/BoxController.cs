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

            // Kutunun gitmesi gereken hedef paleti hesaplayalım
            PalletTrigger.PalletType targetPallet = PalletTrigger.PalletType.Kabul;

            if (DayManager.Instance != null)
            {
                DayConfig config = DayManager.Instance.GetCurrentDayConfig();
                
                if (config.allowBarcodeDefect && currentDefect == DefectType.BarcodeAnomaly)
                {
                    targetPallet = PalletTrigger.PalletType.Ret;
                }
                else if (config.allowSizeAnomalyDefect && currentDefect == DefectType.SizeAnomaly)
                {
                    targetPallet = PalletTrigger.PalletType.Ret;
                }
                else if (config.allowWrongColorDefect && currentDefect == DefectType.WrongColor)
                {
                    targetPallet = GameManager.Instance.ColorDefectRule;
                }
                else if (config.allowWeightDefect && weight >= 10.0f)
                {
                    targetPallet = GameManager.Instance.WeightDefectRule;
                }
                else
                {
                    if (GameManager.Instance.DailyRules.TryGetValue(Shape, out var shapePallet))
                    {
                        targetPallet = shapePallet;
                    }
                }
            }

            // Eğer hedef palet Ret ise kutu defoludur (banttan düşerse hata sayılır)
            return targetPallet == PalletTrigger.PalletType.Ret;
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

    public void InitializeBox(DayConfig config, PalletTrigger.PalletType targetPallet)
    {
        if (isMysteryBox)
        {
            currentDefect = DefectType.None;
            weight = Random.Range(3.0f, 8.0f);
        }
        else if (targetPallet == PalletTrigger.PalletType.Kabul)
        {
            // KABUL kutusu olmalı:
            // 1. Barkod ve Boyut hatası kesinlikle olmamalı (çünkü bunlar her zaman Ret)
            currentDefect = DefectType.None;

            // 2. Renk kuralı kontrolü
            if (config.allowWrongColorDefect)
            {
                if (GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Kabul)
                {
                    // Eğer Yeşil kutular Kabul ediliyorsa, %40 şansla yeşil yapabiliriz
                    currentDefect = (Random.value < 0.4f) ? DefectType.WrongColor : DefectType.None;
                }
                else
                {
                    // Yeşil kutular Ret ediliyorsa, kesinlikle yeşil yapma
                    currentDefect = DefectType.None;
                }
            }

            // 3. Ağırlık kuralı kontrolü
            if (config.allowWeightDefect)
            {
                if (GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Kabul)
                {
                    // Eğer ağır kutular Kabul ediliyorsa, %40 şansla ağır yapabiliriz
                    weight = (Random.value < 0.4f) ? Random.Range(10.0f, 15.0f) : Random.Range(3.0f, 9.9f);
                }
                else
                {
                    // Ağır kutular Ret ediliyorsa, kesinlikle normal ağırlık yap
                    weight = Random.Range(3.0f, 9.9f);
                }
            }
            else
            {
                weight = Random.Range(3.0f, 8.0f);
            }
        }
        else
        {
            // RET kutusu olmalı:
            // Eğer kutunun kendi şekli zaten Ret ise, herhangi bir ek kusura ihtiyacı olmayabilir.
            // Ama yine de çeşitlilik olsun diye kusurlar ekleyebiliriz, ancak bu kusurlar Kabul'e gitmemeli!
            
            bool isRetByShape = false;
            if (GameManager.Instance != null && GameManager.Instance.DailyRules != null &&
                GameManager.Instance.DailyRules.TryGetValue(Shape, out var shapePallet))
            {
                isRetByShape = (shapePallet == PalletTrigger.PalletType.Ret);
            }
            else
            {
                isRetByShape = (Shape == BoxShape.Opened); // 1. Gün varsayılan
            }

            if (isRetByShape)
            {
                // Zaten şekli yüzünden Ret gidiyor. Kusursuz olabilir veya Ret'e giden kusurlar alabilir.
                currentDefect = DefectType.None;
                weight = Random.Range(3.0f, 9.9f);

                // %30 ihtimalle Ret olan diğer kusurlardan birini ekle
                if (Random.value < 0.3f)
                {
                    List<DefectType> allowedRetDefects = new List<DefectType>();
                    if (config.allowBarcodeDefect) allowedRetDefects.Add(DefectType.BarcodeAnomaly);
                    if (config.allowSizeAnomalyDefect) allowedRetDefects.Add(DefectType.SizeAnomaly);
                    if (config.allowWrongColorDefect && GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Ret)
                    {
                        allowedRetDefects.Add(DefectType.WrongColor);
                    }

                    if (allowedRetDefects.Count > 0)
                    {
                        currentDefect = allowedRetDefects[Random.Range(0, allowedRetDefects.Count)];
                    }
                }

                if (config.allowWeightDefect && GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Ret && Random.value < 0.3f)
                {
                    weight = Random.Range(10.0f, 15.0f);
                }
            }
            else
            {
                // Şekli Kabul'e gidiyor, bu yüzden onu Ret yapmak için MUTLAKA Ret'e giden bir kusur vermeliyiz!
                List<DefectType> possibleRetDefects = new List<DefectType>();
                if (config.allowBarcodeDefect) possibleRetDefects.Add(DefectType.BarcodeAnomaly);
                if (config.allowSizeAnomalyDefect) possibleRetDefects.Add(DefectType.SizeAnomaly);
                if (config.allowWrongColorDefect && GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Ret)
                {
                    possibleRetDefects.Add(DefectType.WrongColor);
                }

                bool gaveDefect = false;
                if (possibleRetDefects.Count > 0 && Random.value < 0.7f)
                {
                    currentDefect = possibleRetDefects[Random.Range(0, possibleRetDefects.Count)];
                    gaveDefect = true;
                    weight = Random.Range(3.0f, 9.9f);
                }

                if (!gaveDefect && config.allowWeightDefect && GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Ret)
                {
                    // Ağırlıkla Ret yap
                    weight = Random.Range(10.0f, 15.0f);
                    currentDefect = DefectType.None;
                    gaveDefect = true;
                }
                
                if (!gaveDefect)
                {
                    // Eğer başka hiçbir şekilde Ret yapamıyorsak (örn. 1. Gün ya da henüz kusur açılmamışsa),
                    // Barkod hatası verelim (her zaman aktiftir veya her zaman Ret'tir)
                    currentDefect = DefectType.BarcodeAnomaly;
                    weight = Random.Range(3.0f, 9.9f);
                }
            }
        }

        // Barkod günü aktifse tüm kutulara barkod numarası ata
        if (config.allowBarcodeDefect && GameManager.Instance != null)
        {
            string validNum = GameManager.Instance.ValidBarcodeNumber;
            if (currentDefect == DefectType.BarcodeAnomaly)
            {
                string invalid;
                do { invalid = Random.Range(1000000, 9999999).ToString(); }
                while (invalid == validNum);
                BarcodeNumber = invalid;
            }
            else
            {
                BarcodeNumber = validNum;
            }
        }

        // Kutu kapağı açıksa (Opened veya Unfolded), içini kodla doldur
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

        // 6 yüzey için pozisyon ve rotasyonlar (Ön, Arka, Sol, Sağ, Üst, Alt)
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0, -(halfZ + 0.001f)), // Ön
            new Vector3(0, 0, (halfZ + 0.001f)),  // Arka
            new Vector3(-(halfX + 0.001f), 0, 0), // Sol
            new Vector3((halfX + 0.001f), 0, 0),  // Sağ
            new Vector3(0, (halfY + 0.001f), 0),  // Üst
            new Vector3(0, -(halfY + 0.001f), 0)  // Alt
        };
        
        Vector3[] rotations = new Vector3[]
        {
            new Vector3(0, 0, 0),    // Ön
            new Vector3(0, 180, 0),  // Arka
            new Vector3(0, 90, 0),   // Sol
            new Vector3(0, -90, 0),  // Sağ
            new Vector3(90, 0, 0),   // Üst
            new Vector3(-90, 0, 0)   // Alt
        };

        for (int i = 0; i < 6; i++)
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

        // Sürpriz kutunun gökkuşağı rengi geçişi efekti
        if (isMysteryBox)
        {
            float speed = 0.5f;
            float hue = Mathf.Repeat(Time.time * speed, 1f);
            Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer ren in renderers)
            {
                if (ren is LineRenderer || ren.gameObject.name.Contains("Barcode") || ren.gameObject.name.Contains("MysteryMark")) continue;
                ren.material.color = rainbowColor;
            }
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
