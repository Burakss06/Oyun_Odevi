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
        None,          // Sağlam
        BarcodeAnomaly,// Barkod Hatası
        WrongColor,    // Yanlış Renk
        SizeAnomaly    // Boyut Hatası
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

    public PalletTrigger.PalletType GetTargetPallet()
    {
        if (GameManager.Instance == null || GameManager.Instance.DailyRules == null)
        {
            return currentDefect == DefectType.None ? PalletTrigger.PalletType.Kabul : PalletTrigger.PalletType.Ret;
        }

        if (DayManager.Instance == null)
        {
            return PalletTrigger.PalletType.Kabul;
        }

        DayConfig config = DayManager.Instance.GetCurrentDayConfig();
        
        PalletTrigger.PalletType shapeRule = PalletTrigger.PalletType.Kabul;
        if (GameManager.Instance.DailyRules.TryGetValue(Shape, out var shapePallet))
        {
            shapeRule = shapePallet;
        }

        bool hasRetCondition = false;

        // 1. Şekil kuralı Ret ise
        if (shapeRule == PalletTrigger.PalletType.Ret)
        {
            hasRetCondition = true;
        }

        // 2. Barkod hatası varsa
        if (config.allowBarcodeDefect && currentDefect == DefectType.BarcodeAnomaly)
        {
            hasRetCondition = true;
        }

        // 3. Boyut hatası varsa
        if (config.allowSizeAnomalyDefect && currentDefect == DefectType.SizeAnomaly)
        {
            hasRetCondition = true;
        }

        // 4. Renk kuralı kontrolü
        if (config.allowWrongColorDefect)
        {
            if (currentDefect == DefectType.WrongColor && GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Ret)
            {
                // Yeşil yasaklıysa ve yeşilse
                hasRetCondition = true;
            }
            else if (currentDefect != DefectType.WrongColor && GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Kabul)
            {
                // Sadece yeşil kabul ediliyorsa ve normal renkse
                hasRetCondition = true;
            }
        }

        // 5. Ağırlık kuralı kontrolü
        if (config.allowWeightDefect)
        {
            if (weight >= 10.0f && GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Ret)
            {
                // Ağır yasaklıysa ve ağırsa
                hasRetCondition = true;
            }
            else if (weight < 10.0f && GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Kabul)
            {
                // Sadece ağır kabul ediliyorsa ve hafifse
                hasRetCondition = true;
            }
        }

        return hasRetCondition ? PalletTrigger.PalletType.Ret : PalletTrigger.PalletType.Kabul;
    }

    public bool IsDefective
    {
        get
        {
            if (isMysteryBox) return false;
            return GetTargetPallet() == PalletTrigger.PalletType.Ret;
        }
    }

    [Header("Görsel Efekt Ayarları")]
    [SerializeField] private Color wrongColorTint = new Color(0.1f, 0.9f, 0.1f); // Yeşil boyalı hatalı kutu rengi

    private bool isEvaluated = false;
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
            // Tüm özellikleri Kabul olacak şekilde ayarla
            currentDefect = DefectType.None;

            if (config.allowWrongColorDefect)
            {
                currentDefect = (GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Kabul) 
                    ? DefectType.WrongColor 
                    : DefectType.None;
            }

            if (config.allowWeightDefect)
            {
                weight = (GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Kabul)
                    ? Random.Range(10.0f, 15.0f)
                    : Random.Range(3.0f, 9.9f);
            }
            else
            {
                weight = Random.Range(3.0f, 8.0f);
            }
        }
        else
        {
            // RET KUTUSU ÜRETİMİ
            // Önce kutunun tüm özelliklerini Kabul (kusursuz) olarak başlat
            currentDefect = DefectType.None;
            weight = Random.Range(3.0f, 9.9f);

            if (config.allowWrongColorDefect && GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Kabul)
            {
                currentDefect = DefectType.WrongColor; // Kabul olması için yeşil olması gerek
            }
            if (config.allowWeightDefect && GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Kabul)
            {
                weight = Random.Range(10.0f, 15.0f); // Kabul olması için ağır olması gerek
            }

            // Bu kutuyu bozmak (Ret yapmak) için kullanabileceğimiz yöntemlerin listesini çıkar
            List<System.Action> violateActions = new List<System.Action>();

            // 1. Şekil kuralı ihlali (Eğer spawner zaten Ret olan bir şekil seçmişse, bu kutu zaten Ret'tir)
            bool shapeIsRet = false;
            if (GameManager.Instance != null && GameManager.Instance.DailyRules != null &&
                GameManager.Instance.DailyRules.TryGetValue(Shape, out var shapePallet))
            {
                shapeIsRet = (shapePallet == PalletTrigger.PalletType.Ret);
            }
            else
            {
                shapeIsRet = (Shape == BoxShape.Opened);
            }

            if (shapeIsRet)
            {
                // Şekil zaten Ret, hiçbir şeyi bozmasak da olur
                violateActions.Add(() => { });
            }

            // 2. Barkod kuralı ihlali
            if (config.allowBarcodeDefect)
            {
                violateActions.Add(() => {
                    currentDefect = DefectType.BarcodeAnomaly;
                });
            }

            // 3. Boyut kuralı ihlali
            if (config.allowSizeAnomalyDefect)
            {
                violateActions.Add(() => {
                    currentDefect = DefectType.SizeAnomaly;
                });
            }

            // 4. Renk kuralı ihlali
            if (config.allowWrongColorDefect)
            {
                if (GameManager.Instance.ColorDefectRule == PalletTrigger.PalletType.Ret)
                {
                    // Yeşil yasaksa, yeşil yaparak boz
                    violateActions.Add(() => {
                        currentDefect = DefectType.WrongColor;
                    });
                }
                else
                {
                    // Sadece yeşil kabul ediliyorsa, normal renk (yeşil olmayan) yaparak boz
                    violateActions.Add(() => {
                        currentDefect = DefectType.None;
                    });
                }
            }

            // 5. Ağırlık kuralı ihlali
            if (config.allowWeightDefect)
            {
                if (GameManager.Instance.WeightDefectRule == PalletTrigger.PalletType.Ret)
                {
                    // Ağır yasaksa, ağır yaparak boz
                    violateActions.Add(() => {
                        weight = Random.Range(10.0f, 15.0f);
                    });
                }
                else
                {
                    // Sadece ağır kabul ediliyorsa, hafif yaparak boz
                    violateActions.Add(() => {
                        weight = Random.Range(3.0f, 9.9f);
                    });
                }
            }

            // En az bir kuralı ihlal et
            if (violateActions.Count > 0)
            {
                int firstIndex = Random.Range(0, violateActions.Count);
                violateActions[firstIndex]();

                // %25 şansla ikinci bir kuralı da ihlal edebiliriz (çoklu hata)
                if (violateActions.Count > 1 && Random.value < 0.25f)
                {
                    int secondIndex;
                    do { secondIndex = Random.Range(0, violateActions.Count); }
                    while (secondIndex == firstIndex);
                    violateActions[secondIndex]();
                }
            }
            else
            {
                // Herhangi bir kural aktif değilse (örn. Day 1, ama Day 1 spawner zaten Opened'ı Ret olarak seçer)
                currentDefect = DefectType.BarcodeAnomaly;
            }
        }

        // Barkod ata
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

        // Açık kutuların içini doldur
        if (Shape == BoxShape.Opened || Shape == BoxShape.Unfolded)
        {
            FillBoxWithOfficeProps();
        }

        ApplyVisualDefect();
    }


    private void CreateBarcodeUI()
    {
        Renderer boxRenderer = GetComponentInChildren<Renderer>();
        Bounds bounds = (boxRenderer != null) ? boxRenderer.bounds : new Bounds(transform.position, Vector3.one);

        float halfDepthLocal = bounds.extents.z / transform.lossyScale.z;

        GameObject labelObj = new GameObject("BarcodeLabel");
        labelObj.transform.SetParent(transform);

        float halfWidthLocal  = bounds.extents.x / transform.lossyScale.x;
        float halfHeightLocal = bounds.extents.y / transform.lossyScale.y;
        
        labelObj.transform.localPosition = new Vector3(
            halfWidthLocal  * 0.55f,   // sağa kaydır
           -halfHeightLocal * 0.55f,   // aşağı kaydır
           -(halfDepthLocal + 0.001f)  // ön yüzeye yapıştır
        );
        labelObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        labelObj.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.1f;
        tm.fontSize = 35;

        string displayNum = (BarcodeNumber != "") ? BarcodeNumber : Random.Range(1000000, 9999999).ToString();
        tm.text = "|| | ||| | ||\n" + displayNum;
        tm.color = Color.black;

        MeshRenderer meshRen = labelObj.GetComponent<MeshRenderer>();
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

    private void CreateMysteryUI()
    {
        Renderer boxRenderer = GetComponentInChildren<Renderer>();
        Bounds bounds = (boxRenderer != null) ? boxRenderer.bounds : new Bounds(transform.position, Vector3.one);

        float halfX = bounds.extents.x / transform.lossyScale.x;
        float halfY = bounds.extents.y / transform.lossyScale.y;
        float halfZ = bounds.extents.z / transform.lossyScale.z;

        // 6 yüzeyin pozisyon ve rotasyonları
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0, -(halfZ + 0.001f)),
            new Vector3(0, 0, (halfZ + 0.001f)),
            new Vector3(-(halfX + 0.001f), 0, 0),
            new Vector3((halfX + 0.001f), 0, 0),
            new Vector3(0, (halfY + 0.001f), 0),
            new Vector3(0, -(halfY + 0.001f), 0)
        };
        
        Vector3[] rotations = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 180, 0),
            new Vector3(0, 90, 0),
            new Vector3(0, -90, 0),
            new Vector3(90, 0, 0),
            new Vector3(-90, 0, 0)
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
            tm.fontSize = 120;
            tm.fontStyle = FontStyle.Bold;
            
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
            return;
        }

        int itemCount = 1;
        Vector3 innerExtents = new Vector3(0.20f, 0.16f, 0.13f); 
        float bottomY = -innerExtents.y + 0.02f;

        for (int i = 0; i < itemCount; i++)
        {
            GameObject prefab;
            do
            {
                prefab = officePropsCache[Random.Range(0, officePropsCache.Length)];
            } 
            while (prefab.name.ToLower().Contains("book"));
            
            int spawnCount = 1;

            for (int s = 0; s < spawnCount; s++)
            {
                GameObject item = Instantiate(prefab, contentsRoot.transform, false);
                
                Collider[] cols = item.GetComponentsInChildren<Collider>();
                foreach (Collider c in cols) Destroy(c);

                item.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                
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

                    bounds = renderers[0].bounds;
                    for (int j = 1; j < renderers.Length; j++) bounds.Encapsulate(renderers[j].bounds);

                    float worldOffsetToBottom = item.transform.position.y - bounds.min.y;
                    float localYOffset = worldOffsetToBottom / transform.lossyScale.y;

                    float posX = Random.Range(-0.04f, 0.04f);
                    float posZ = Random.Range(-0.04f, 0.04f);

                    item.transform.localPosition = new Vector3(posX, bottomY + localYOffset, posZ);
                }
            }
        }
    }

    private void ApplyVisualDefect()
    {
        if (DayManager.Instance != null && DayManager.Instance.GetCurrentDayConfig().allowBarcodeDefect && !isMysteryBox)
        {
            CreateBarcodeUI();
        }

        if (isMysteryBox)
        {
            CreateMysteryUI();
        }

        switch (currentDefect)
        {
            case DefectType.None:
                break;

            case DefectType.BarcodeAnomaly:
                break;

            case DefectType.WrongColor:
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer ren in renderers)
                {
                    if (ren is LineRenderer) continue;
                    ren.material.color = wrongColorTint;
                }
                break;

            case DefectType.SizeAnomaly:
                float scaleMultiplier = (Random.value > 0.5f) ? 0.70f : 1.24f;
                transform.localScale = originalScale * scaleMultiplier;
                
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
                ren.material.SetColor("_EmissionColor", Color.yellow * 2.5f);
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
        // Keskin Göz özelliği
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

        // Sürpriz kutu renk animasyonu
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

        // Banttan aşağı düşme kontrolü
        if (!isEvaluated && transform.position.y < -4.0f)
        {
            isEvaluated = true;
            
            if (GameManager.Instance != null)
            {
                if (IsDefective)
                {
                    GameManager.Instance.AddIncorrectChoice();
                    Debug.Log($"Kusurlu kutu ({currentDefect}) düştü, hata arttı.");
                }
                else
                {
                    GameManager.Instance.BoxMissed();
                    Debug.Log("Sağlam kutu düştü.");
                }
            }

            Destroy(gameObject);
        }
    }
}
