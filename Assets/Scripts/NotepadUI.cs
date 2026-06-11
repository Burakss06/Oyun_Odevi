using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class NotepadUI : MonoBehaviour
{
    private GameObject notepadPanel;
    private TextMeshProUGUI rulesTextComponent;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;

    private bool isInitialized = false;
    private float targetX = 350f; // Default off-screen position
    private float currentX = 350f;
    private float transitionSpeed = 15f; // Fast, smooth slide-in/out

    private void Start()
    {
        InitializeNotepad();
    }

    private void InitializeNotepad()
    {
        if (isInitialized) return;
        isInitialized = true;

        // --- ANA PANEL (Sağ Alt Kenara Hizalı Not Defteri Kağıdı) ---
        notepadPanel = new GameObject("NotepadPanel", typeof(RectTransform));
        notepadPanel.transform.SetParent(transform, false);

        panelRect = notepadPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f); // Sağ Alt
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.sizeDelta = new Vector2(320f, 400f);
        panelRect.anchoredPosition = new Vector2(350f, 25f); // Başlangıçta ekran dışı

        canvasGroup = notepadPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Kağıt Arka Plan Görseli
        Image paperImg = notepadPanel.AddComponent<Image>();
        paperImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        paperImg.type = Image.Type.Sliced;
        paperImg.color = new Color(0.99f, 0.985f, 0.96f, 1f); // Fildişi/Krem kağıt rengi

        // Kağıt Kenarlık / Gölge
        Outline paperOutline = notepadPanel.AddComponent<Outline>();
        paperOutline.effectColor = new Color(0f, 0f, 0f, 0.12f);
        paperOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow paperShadow = notepadPanel.AddComponent<Shadow>();
        paperShadow.effectColor = new Color(0f, 0f, 0f, 0.15f);
        paperShadow.effectDistance = new Vector2(4f, -4f);

        // Kırmızı Defter Çizgisi (Sol marjin)
        GameObject redLine = new GameObject("RedLine", typeof(RectTransform));
        redLine.transform.SetParent(notepadPanel.transform, false);
        RectTransform redLineRT = redLine.GetComponent<RectTransform>();
        redLineRT.anchorMin = new Vector2(0f, 0f);
        redLineRT.anchorMax = new Vector2(0f, 1f);
        redLineRT.pivot = new Vector2(0f, 0.5f);
        redLineRT.anchoredPosition = new Vector2(30f, 0f);
        redLineRT.sizeDelta = new Vector2(1.5f, 0f);
        redLineRT.offsetMin = new Vector2(30f, 0f);
        redLineRT.offsetMax = new Vector2(31.5f, 0f);
        
        Image redLineImg = redLine.AddComponent<Image>();
        redLineImg.color = new Color(0.9f, 0.45f, 0.45f, 0.6f);

        // Yatay Çizgiler Konteyneri
        GameObject linesContainer = new GameObject("HorizontalLines", typeof(RectTransform));
        linesContainer.transform.SetParent(notepadPanel.transform, false);
        RectTransform linesRT = linesContainer.GetComponent<RectTransform>();
        linesRT.anchorMin = Vector2.zero;
        linesRT.anchorMax = Vector2.one;
        linesRT.offsetMin = Vector2.zero;
        linesRT.offsetMax = Vector2.zero;

        // Defterin yatay çizgilerini yerleştir
        float startY = 320f;
        float lineSpacing = 24.5f; // Bir önceki hizalama durumuna geri döndürüldü
        int lineCount = 12;
        for (int i = 0; i < lineCount; i++)
        {
            GameObject line = new GameObject($"Line_{i}", typeof(RectTransform));
            line.transform.SetParent(linesContainer.transform, false);
            RectTransform lineRT = line.GetComponent<RectTransform>();
            lineRT.anchorMin = new Vector2(0f, 0f);
            lineRT.anchorMax = new Vector2(1f, 0f);
            lineRT.pivot = new Vector2(0f, 0f);
            lineRT.anchoredPosition = new Vector2(31f, startY - (i * lineSpacing));
            lineRT.sizeDelta = new Vector2(-43f, 1f); // Genişlik: DefterGenişliği - 43

            Image lineImg = line.AddComponent<Image>();
            lineImg.color = new Color(0.80f, 0.85f, 0.95f, 0.35f); // Çok yumuşak mavi çizgiler
        }

        // Deri Üst Klips Bandı
        GameObject headerClip = new GameObject("HeaderClip", typeof(RectTransform));
        headerClip.transform.SetParent(notepadPanel.transform, false);
        RectTransform headerRT = headerClip.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f); // Pivot 0.5f yapıldı ki metal klips ortada dursun
        headerRT.offsetMin = Vector2.zero; // Stretched anchors için offset'leri sıfırla (Kaymayı önler)
        headerRT.offsetMax = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0f, 22f); // Genişlik tam ekran eni, yükseklik 22
        headerRT.anchoredPosition = Vector2.zero;
        
        Image headerImg = headerClip.AddComponent<Image>();
        headerImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        headerImg.type = Image.Type.Sliced;
        headerImg.color = new Color(0.24f, 0.16f, 0.14f, 1f); // Kahverengi deri bant

        // Metal Kıskaç
        GameObject metalClip = new GameObject("MetalClip", typeof(RectTransform));
        metalClip.transform.SetParent(headerClip.transform, false);
        RectTransform metalRT = metalClip.GetComponent<RectTransform>();
        metalRT.anchorMin = new Vector2(0.5f, 0.5f);
        metalRT.anchorMax = new Vector2(0.5f, 0.5f);
        metalRT.pivot = new Vector2(0.5f, 0.5f);
        metalRT.anchoredPosition = new Vector2(0f, -5f);
        metalRT.sizeDelta = new Vector2(75f, 8f);
        
        Image metalImg = metalClip.AddComponent<Image>();
        metalImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        metalImg.type = Image.Type.Sliced;
        metalImg.color = new Color(0.66f, 0.66f, 0.68f, 1f); // Metalik gri kıskaç

        // Başlık Yazısı
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform));
        titleGO.transform.SetParent(notepadPanel.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(36f, -28f);
        titleRT.sizeDelta = new Vector2(-51f, 30f); // Genişlik: DefterGenişliği - 51

        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "GÜNLÜK KURALLAR";
        titleText.fontSize = 22f; // Boyut 20f'ten 22f'e yükseltildi
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.12f, 0.15f, 0.2f, 1f);
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.raycastTarget = false;

        // Başlık Altı Çizgisi
        GameObject titleSep = new GameObject("TitleSep", typeof(RectTransform));
        titleSep.transform.SetParent(notepadPanel.transform, false);
        RectTransform sepRT = titleSep.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0f, 1f);
        sepRT.anchorMax = new Vector2(1f, 1f);
        sepRT.pivot = new Vector2(0f, 1f);
        sepRT.anchoredPosition = new Vector2(36f, -56f);
        sepRT.sizeDelta = new Vector2(-51f, 1.5f);
        
        Image sepImg = titleSep.AddComponent<Image>();
        sepImg.color = new Color(0.12f, 0.15f, 0.2f, 0.18f);
        sepImg.raycastTarget = false;

        // Kural Listesi Metin Kutusu
        GameObject contentGO = new GameObject("RulesText", typeof(RectTransform));
        contentGO.transform.SetParent(notepadPanel.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.offsetMin = new Vector2(36f, 30f); // Sol marjin 36, Alt marjin 30
        contentRT.offsetMax = new Vector2(-15f, -65f); // Sağ marjin -15, Üst marjin -65 (başlık altı)

        rulesTextComponent = contentGO.AddComponent<TextMeshProUGUI>();
        rulesTextComponent.fontSize = 18f; // Boyut 18f olarak korunuyor
        rulesTextComponent.lineSpacing = 16f; // Bir önceki satır yüksekliği hizalamasına geri dönüldü
        rulesTextComponent.fontStyle = FontStyles.Bold;
        rulesTextComponent.color = new Color(0.15f, 0.18f, 0.22f, 1f);
        rulesTextComponent.alignment = TextAlignmentOptions.TopLeft;
        rulesTextComponent.text = "Yükleniyor...";
        rulesTextComponent.raycastTarget = false;

        // Alt Bilgi Notu (Kullanıcı Kılavuzu)
        GameObject infoGO = new GameObject("InfoText", typeof(RectTransform));
        infoGO.transform.SetParent(notepadPanel.transform, false);
        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0f, 0f);
        infoRT.anchorMax = new Vector2(1f, 0f);
        infoRT.pivot = new Vector2(0f, 0f);
        infoRT.anchoredPosition = new Vector2(36f, 8f);
        infoRT.sizeDelta = new Vector2(-51f, 22f); // Yükseklik 18f'ten 22f'e yükseltildi

        TextMeshProUGUI infoText = infoGO.AddComponent<TextMeshProUGUI>();
        infoText.text = "* TAB tuşunu bırakınca kapanır";
        infoText.fontSize = 13f; // Boyut 9.5f'ten 13f'e yükseltildi (Okunur hale getirildi)
        infoText.fontStyle = FontStyles.Italic;
        infoText.color = new Color(0.45f, 0.5f, 0.6f, 0.8f);
        infoText.alignment = TextAlignmentOptions.Left;
        infoText.raycastTarget = false;
    }

    private bool wasTabPressedLastFrame = false;

    private void Update()
    {
        if (!isInitialized) return;

        // Oyun oynanıyor olmalı ve Tab tuşuna basılı tutuluyor olmalı
        bool isPlaying = GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing;
        bool isTabPressed = Keyboard.current != null && Keyboard.current.tabKey.isPressed;

        if (isPlaying && isTabPressed)
        {
            targetX = -25f; // Ekrana gir (Sağ alt kenardan 25px içeride)
            if (!wasTabPressedLastFrame)
            {
                UpdateRulesText();
            }
        }
        else
        {
            targetX = 350f; // Ekranın sağına çık (gizlen)
        }

        wasTabPressedLastFrame = isTabPressed;

        // Yumuşak yatay hareket (X kaydırılır, Y koordinatı sabit 25f)
        currentX = Mathf.Lerp(currentX, targetX, Time.unscaledDeltaTime * transitionSpeed);
        panelRect.anchoredPosition = new Vector2(currentX, 25f);

        // Yumuşak opaklık geçişi (alpha)
        float targetAlpha = (targetX < 0) ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * transitionSpeed);
    }

    private void UpdateRulesText()
    {
        if (GameManager.Instance == null || DayManager.Instance == null) return;

        int currentDay = DayManager.Instance.CurrentDay;
        string text = "";

        // Başlık (GÜN X KURALLARI)
        text += $"<color=#1565C0><size=115%><b>GÜN {currentDay} KURALLARI</b></size></color>\n\n";

        // Kutu sınıflandırma kuralları
        if (GameManager.Instance.DailyRules != null && GameManager.Instance.DailyRules.Count > 0)
        {
            foreach (var rule in GameManager.Instance.DailyRules)
            {
                string shapeName = "";
                if (rule.Key == BoxController.BoxShape.Closed) shapeName = "Kapalı Kutu";
                else if (rule.Key == BoxController.BoxShape.Opened) shapeName = "Açık Kutu";
                else if (rule.Key == BoxController.BoxShape.Unfolded) shapeName = "Düz Karton";

                string palletName = (rule.Value == PalletTrigger.PalletType.Kabul) 
                    ? "<b><color=#2E7D32>KABUL</color></b>" 
                    : "<b><color=#C62828>RET</color></b>";

                text += $"• {shapeName} ➔ {palletName}\n";
            }
        }
        else
        {
            // 1. Gün varsayılan kuralları
            string kabulColor = "<b><color=#2E7D32>KABUL</color></b>";
            string retColor = "<b><color=#C62828>RET</color></b>";
            text += $"• Kapalı Kutu ➔ {kabulColor}\n";
            text += $"• Açık Kutu ➔ {retColor}\n";
        }

        // Günlük fiziksel hata kuralları
        DayConfig config = DayManager.Instance.GetCurrentDayConfig();
        if (config.allowBarcodeDefect && GameManager.Instance != null)
        {
            string validNum = GameManager.Instance.ValidBarcodeNumber;
            text += $"• Geçerli Barkod: <b><color=#1565C0>{validNum}</color></b>\n";
            text += $"• Farklı numaralı barkod ➔ <b><color=#C62828>RET</color></b>\n";
        }
        if (config.allowWrongColorDefect)
        {
            text += $"• <color=#2E7D32>Yeşil Boyalı</color> ➔ <b><color=#C62828>RET</color></b>\n";
        }
        if (config.allowSizeAnomalyDefect)
        {
            text += $"• Boyut Hatası ➔ <b><color=#C62828>RET</color></b>\n";
        }
        if (config.allowWeightDefect)
        {
            text += $"• Ağır Kutu (≥10kg) ➔ <b><color=#C62828>RET</color></b> (Tart!)\n";
        }
        if (currentDay == 7)
        {
            text += $"• Mor Kutu ➔ Sürpriz (%50 Şans)\n";
        }

        rulesTextComponent.text = text;
    }
}
