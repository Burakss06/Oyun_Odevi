using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menüsünü runtime'da premium ve profesyonel tasarıma uygun şekilde oluşturur.
/// Genel olarak boyutu ve yazıları biraz daha büyütülmüş, ferah ve geniş ekran dostu arayüz.
/// Slider'ın eni genişletilmiş ve sürükleme tutacağı (handle) dairesel bir noktaya dönüştürülmüştür.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    // Oluşturulan referanslar (GameManager bunlara erişecek)
    [HideInInspector] public Button resumeButton;
    [HideInInspector] public Button muteButton;
    [HideInInspector] public Slider sensitivitySlider;
    [HideInInspector] public TextMeshProUGUI sensitivityValueText;

    private TextMeshProUGUI muteButtonText;
    private bool isBuilt = false;

    private void Awake()
    {
        if (!isBuilt) BuildUI();
    }

    public void BuildUI()
    {
        if (isBuilt) return;
        isBuilt = true;

        // Mevcut tüm child objeleri temizle
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // --- ANA OVERLAY (Tam ekran arka plan) ---
        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image overlayImg = gameObject.GetComponent<Image>();
        if (overlayImg == null) overlayImg = gameObject.AddComponent<Image>();
        overlayImg.color = new Color(0.03f, 0.03f, 0.05f, 0.65f); // Koyu sinematik overlay
        overlayImg.raycastTarget = true;

        // ============================================================
        // ANA KART PANELİ (Koyu panel kutusu - rounded corners)
        // ============================================================
        GameObject cardObj = MakeObj("MenuCard", transform);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(550f, 550f);
        cardRect.anchoredPosition = new Vector2(0f, 20f);

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd"); // Yuvarlatılmış köşe
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.12f, 0.12f, 0.15f, 0.96f);
        cardImg.raycastTarget = true;

        // Kenarlık (border)
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // ============================================================
        // İÇERİK DÜZENİ (Vertical Layout Group)
        // ============================================================
        GameObject contentRoot = MakeObj("ContentRoot", cardObj.transform);
        RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(35f, 35f); // İç boşluklar (padding)
        contentRect.offsetMax = new Vector2(-35f, -35f);

        VerticalLayoutGroup contentLayout = contentRoot.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 22f; // Elemanlar arası boşluk

        // ============================================================
        // 1) BAŞLIK: "OYUN DURDURULDU"
        // ============================================================
        GameObject titleObj = MakeObj("TitleText", contentRoot.transform);
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "OYUN DURDURULDU";
        titleTMP.fontSize = 34f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.67f, 0f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
        titleTMP.raycastTarget = false;
        
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 50f;

        // Seperator (İnce çizgi)
        GameObject separator = MakeObj("Separator", contentRoot.transform);
        Image sepImg = separator.AddComponent<Image>();
        sepImg.color = new Color(1f, 1f, 1f, 0.1f);
        sepImg.raycastTarget = false;
        
        LayoutElement sepLE = separator.AddComponent<LayoutElement>();
        sepLE.preferredHeight = 3f;

        // ============================================================
        // 2) FARE HASSASİYETİ YAZISI
        // ============================================================
        GameObject sensLabel = MakeObj("SensLabel", contentRoot.transform);
        TextMeshProUGUI sensLabelTMP = sensLabel.AddComponent<TextMeshProUGUI>();
        sensLabelTMP.text = "FARE HASSASİYETİ";
        sensLabelTMP.fontSize = 18f;
        sensLabelTMP.fontStyle = FontStyles.Bold;
        sensLabelTMP.color = new Color(1f, 1f, 1f, 0.55f);
        sensLabelTMP.alignment = TextAlignmentOptions.Center;
        sensLabelTMP.raycastTarget = false;

        LayoutElement sensLabelLE = sensLabel.AddComponent<LayoutElement>();
        sensLabelLE.preferredHeight = 25f;

        // ============================================================
        // 3) SLIDER + DEĞER SATIRI (Horizontal Layout)
        // ============================================================
        GameObject sliderRow = MakeObj("SliderRow", contentRoot.transform);
        HorizontalLayoutGroup sliderRowHL = sliderRow.AddComponent<HorizontalLayoutGroup>();
        sliderRowHL.childAlignment = TextAnchor.MiddleCenter;
        sliderRowHL.childControlWidth = true; // Slider eninin doğru yerleşmesi için true yapıldı!
        sliderRowHL.childControlHeight = true;
        sliderRowHL.childForceExpandWidth = false;
        sliderRowHL.childForceExpandHeight = false;
        sliderRowHL.spacing = 18f;

        LayoutElement sliderRowLE = sliderRow.AddComponent<LayoutElement>();
        sliderRowLE.preferredHeight = 40f;

        // Slider
        GameObject sliderGO = BuildSlider(sliderRow.transform);
        sensitivitySlider = sliderGO.GetComponent<Slider>();
        LayoutElement sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.preferredWidth = 340f; // Slider genişliği
        sliderLE.preferredHeight = 28f;

        // Değer metni
        GameObject valueObj = MakeObj("ValueText", sliderRow.transform);
        sensitivityValueText = valueObj.AddComponent<TextMeshProUGUI>();
        sensitivityValueText.text = "0.9";
        sensitivityValueText.fontSize = 24f;
        sensitivityValueText.fontStyle = FontStyles.Bold;
        sensitivityValueText.color = new Color(1f, 0.67f, 0f, 1f);
        sensitivityValueText.alignment = TextAlignmentOptions.Center;
        sensitivityValueText.raycastTarget = false;

        LayoutElement valueLE = valueObj.AddComponent<LayoutElement>();
        valueLE.preferredWidth = 55f;
        valueLE.preferredHeight = 35f;

        // Ayırıcı
        GameObject btnSeparator = MakeObj("BtnSeparator", contentRoot.transform);
        Image btnSepImg = btnSeparator.AddComponent<Image>();
        btnSepImg.color = new Color(1f, 1f, 1f, 0.05f);
        btnSepImg.raycastTarget = false;
        
        LayoutElement btnSepLE = btnSeparator.AddComponent<LayoutElement>();
        btnSepLE.preferredHeight = 3f;

        // ============================================================
        // 4) DİKEY BUTONLAR
        // ============================================================
        // Devam Et Butonu
        resumeButton = CreateStyledButton(contentRoot.transform, "ResumeBtn", "Devam Et", new Color(0.16f, 0.62f, 0.25f, 1f));

        // Müziği Kapa Butonu
        muteButton = CreateStyledButton(contentRoot.transform, "MuteBtn", "Müziği Kapa", new Color(0.22f, 0.24f, 0.28f, 1f));
        muteButtonText = muteButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    // ================================================================
    // YARDIMCI METOTLAR
    // ================================================================

    private GameObject MakeObj(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private GameObject BuildSlider(Transform parent)
    {
        GameObject sliderGO = MakeObj("Slider", parent);

        // Background (track)
        GameObject bg = MakeObj("Background", sliderGO.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(1f, 1f, 1f, 0.1f);
        bgImg.raycastTarget = true;
        
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.38f);
        bgRT.anchorMax = new Vector2(1f, 0.62f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = MakeObj("Fill Area", sliderGO.transform);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.38f);
        faRT.anchorMax = new Vector2(1f, 0.62f);
        faRT.offsetMin = new Vector2(5f, 0f);
        faRT.offsetMax = new Vector2(-5f, 0f);

        GameObject fill = MakeObj("Fill", fillArea.transform);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(1f, 0.67f, 0f, 0.65f);
        fillImg.raycastTarget = false;
        
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Handle Slide Area
        GameObject handleArea = MakeObj("Handle Slide Area", sliderGO.transform);
        RectTransform haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10f, 0f);
        haRT.offsetMax = new Vector2(-10f, 0f);

        // Handle (Görünmez Taşıyıcı / Dummy Handle)
        GameObject handleDummy = MakeObj("Handle", handleArea.transform);
        RectTransform handleRT = handleDummy.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(0f, 1f);
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;

        // Görsel Daire (Visual Knob) - Dummy'nin içinde ortalanmış ve esnemeyen daire
        GameObject handleVisual = MakeObj("HandleVisual", handleDummy.transform);
        Image handleImg = handleVisual.AddComponent<Image>();
        handleImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        handleImg.color = Color.white;
        handleImg.raycastTarget = true;
        handleImg.preserveAspect = true;

        RectTransform visualRT = handleVisual.GetComponent<RectTransform>();
        visualRT.anchorMin = new Vector2(0.5f, 0.5f);
        visualRT.anchorMax = new Vector2(0.5f, 0.5f);
        visualRT.pivot = new Vector2(0.5f, 0.5f);
        visualRT.sizeDelta = new Vector2(24f, 24f); // "bi tık yine büyük olsun çok küçük olmasın ama büyükte olmasın"
        visualRT.anchoredPosition = Vector2.zero;

        // Slider bileşeni
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0.1f;
        slider.maxValue = 5f;
        slider.value = 0.9f;

        return sliderGO;
    }

    private Button CreateStyledButton(Transform parent, string name, string label, Color bgColor)
    {
        GameObject btnGO = MakeObj(name, parent);
        LayoutElement btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 62f;
        btnLE.preferredWidth = 420f;

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        btnImg.type = Image.Type.Sliced;
        btnImg.color = bgColor;
        btnImg.raycastTarget = true;

        Button btn = btnGO.AddComponent<Button>();
        
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor + new Color(0.08f, 0.08f, 0.08f, 0f);
        cb.pressedColor = bgColor - new Color(0.12f, 0.12f, 0.12f, 0f);
        cb.selectedColor = cb.highlightedColor;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.targetGraphic = btnImg;

        Outline btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0f, 0f, 0f, 0.15f);
        btnOutline.effectDistance = new Vector2(1f, -1f);

        // Buton Metni
        GameObject txtGO = MakeObj("Text", btnGO.transform);
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 25f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        return btn;
    }

    public void UpdateMuteText(bool isMuted)
    {
        if (muteButtonText != null)
        {
            muteButtonText.text = isMuted ? "Müzik Aç" : "Müziği Kapa";
        }
    }
}
