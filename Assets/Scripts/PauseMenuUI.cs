using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menüsünü runtime'da tasarıma uygun şekilde oluşturur.
/// Tam ekran yarı-saydam overlay, ortada başlık, ortada slider, altta yeşil buton barı.
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

        // --- ANA PANEL (tam ekran overlay) ---
        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();
        // Tam ekranı kapla
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Yarı-saydam siyah overlay
        Image overlayImg = gameObject.GetComponent<Image>();
        if (overlayImg == null) overlayImg = gameObject.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImg.raycastTarget = true;

        // ============================================================
        // Ana içerik konteyneri - dikey layout, ekranın ortasında
        // ============================================================
        GameObject contentRoot = MakeObj("ContentRoot", transform);
        RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(500f, 380f);
        contentRect.anchoredPosition = new Vector2(0f, 30f); // Biraz yukarıda

        VerticalLayoutGroup contentLayout = contentRoot.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 0f;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);

        // ============================================================
        // 1) BAŞLIK: "OYUN DURDURULDU"
        // ============================================================
        GameObject titleObj = MakeObj("TitleText", contentRoot.transform);
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "OYUN DURDURULDU";
        titleTMP.fontSize = 44f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.67f, 0f, 1f); // Turuncu
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 70f;

        // Spacer
        AddSpacer(contentRoot.transform, 50f);

        // ============================================================
        // 2) FARE HASSASİYETİ LABEL
        // ============================================================
        GameObject sensLabel = MakeObj("SensLabel", contentRoot.transform);
        TextMeshProUGUI sensLabelTMP = sensLabel.AddComponent<TextMeshProUGUI>();
        sensLabelTMP.text = "FARE HASSASİYETİ";
        sensLabelTMP.fontSize = 17f;
        sensLabelTMP.fontStyle = FontStyles.Bold;
        sensLabelTMP.color = new Color(1f, 1f, 1f, 0.95f);
        sensLabelTMP.alignment = TextAlignmentOptions.Center;
        LayoutElement sensLabelLE = sensLabel.AddComponent<LayoutElement>();
        sensLabelLE.preferredHeight = 28f;

        // ============================================================
        // 3) SLIDER + DEĞER SATIRI
        // ============================================================
        GameObject sliderRow = MakeObj("SliderRow", contentRoot.transform);
        HorizontalLayoutGroup sliderRowHL = sliderRow.AddComponent<HorizontalLayoutGroup>();
        sliderRowHL.childAlignment = TextAnchor.MiddleCenter;
        sliderRowHL.childControlWidth = false;
        sliderRowHL.childControlHeight = true;
        sliderRowHL.childForceExpandWidth = false;
        sliderRowHL.childForceExpandHeight = false;
        sliderRowHL.spacing = 12f;
        sliderRowHL.padding = new RectOffset(20, 20, 0, 0);
        LayoutElement sliderRowLE = sliderRow.AddComponent<LayoutElement>();
        sliderRowLE.preferredHeight = 30f;

        // Slider
        GameObject sliderGO = BuildSlider(sliderRow.transform);
        LayoutElement sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.preferredWidth = 340f;
        sliderLE.preferredHeight = 20f;

        // Değer metni
        GameObject valueObj = MakeObj("ValueText", sliderRow.transform);
        sensitivityValueText = valueObj.AddComponent<TextMeshProUGUI>();
        sensitivityValueText.text = "0,9";
        sensitivityValueText.fontSize = 18f;
        sensitivityValueText.fontStyle = FontStyles.Bold;
        sensitivityValueText.color = new Color(1f, 0.67f, 0f, 1f);
        sensitivityValueText.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement valueLE = valueObj.AddComponent<LayoutElement>();
        valueLE.preferredWidth = 50f;
        valueLE.preferredHeight = 28f;

        // Spacer
        AddSpacer(contentRoot.transform, 80f);

        // ============================================================
        // 4) ALT YEŞİL BAR (DEVAM ET | Müziği Kapa)
        // ============================================================
        GameObject bar = MakeObj("ButtonBar", contentRoot.transform);
        Image barImg = bar.AddComponent<Image>();
        barImg.color = new Color(0.16f, 0.62f, 0.25f, 1f); // Yeşil
        LayoutElement barLE = bar.AddComponent<LayoutElement>();
        barLE.preferredHeight = 46f;
        barLE.preferredWidth = 400f;

        HorizontalLayoutGroup barHL = bar.AddComponent<HorizontalLayoutGroup>();
        barHL.childAlignment = TextAnchor.MiddleCenter;
        barHL.childControlWidth = true;
        barHL.childControlHeight = true;
        barHL.childForceExpandWidth = true;
        barHL.childForceExpandHeight = true;
        barHL.spacing = 0f;
        barHL.padding = new RectOffset(0, 0, 0, 0);

        // ContentSizeFitter ile barın layout'a uymasını sağla
        ContentSizeFitter barFitter = bar.AddComponent<ContentSizeFitter>();
        barFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        barFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // DEVAM ET butonu
        resumeButton = MakeBarButton(bar.transform, "ResumeBtn", "DEVAM ET");

        // Dikey ayırıcı
        GameObject divider = MakeObj("Divider", bar.transform);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(1f, 1f, 1f, 0.25f);
        LayoutElement divLE = divider.AddComponent<LayoutElement>();
        divLE.preferredWidth = 2f;
        divLE.flexibleWidth = 0f;

        // Müziği Kapa butonu
        muteButton = MakeBarButton(bar.transform, "MuteBtn", "Müziği Kapa");
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

    private void AddSpacer(Transform parent, float h)
    {
        GameObject sp = MakeObj("Spacer", parent);
        LayoutElement le = sp.AddComponent<LayoutElement>();
        le.preferredHeight = h;
    }

    private GameObject BuildSlider(Transform parent)
    {
        GameObject sliderGO = MakeObj("Slider", parent);

        // Background (track)
        GameObject bg = MakeObj("Background", sliderGO.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.45f, 0.45f, 0.5f, 0.7f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.35f);
        bgRT.anchorMax = new Vector2(1f, 0.65f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = MakeObj("Fill Area", sliderGO.transform);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.35f);
        faRT.anchorMax = new Vector2(1f, 0.65f);
        faRT.offsetMin = new Vector2(5f, 0f);
        faRT.offsetMax = new Vector2(-5f, 0f);

        GameObject fill = MakeObj("Fill", fillArea.transform);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.35f, 0.35f, 0.4f, 0.5f);
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

        // Handle
        GameObject handle = MakeObj("Handle", handleArea.transform);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(18f, 18f);
        handleRT.anchorMin = new Vector2(0f, 0.5f);
        handleRT.anchorMax = new Vector2(0f, 0.5f);

        // Slider bileşeni
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0.1f;
        slider.maxValue = 5f;
        slider.value = 0.9f;

        sensitivitySlider = slider;
        return sliderGO;
    }

    private Button MakeBarButton(Transform parent, string name, string label)
    {
        GameObject btnGO = MakeObj(name, parent);

        // Şeffaf arka plan (yeşil bar zaten arka planda)
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(1, 1, 1, 0);
        cb.highlightedColor = new Color(1, 1, 1, 0.1f);
        cb.pressedColor = new Color(0, 0, 0, 0.15f);
        cb.selectedColor = new Color(1, 1, 1, 0);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.targetGraphic = btnImg;

        // Metin
        GameObject txtGO = MakeObj("Text", btnGO.transform);
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
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
