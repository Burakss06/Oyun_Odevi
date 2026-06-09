using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIThemeEnhancer : MonoBehaviour
{
    private void Start()
    {
        // 1 saniye sonra çalıştır ki diğer scriptler (örn: PauseMenuUI) kurulumlarını tamamlasın
        Invoke("EnhanceUI", 0.5f);
    }

    private void EnhanceUI()
    {
        Debug.Log("[UIThemeEnhancer] UI Teması İyileştiriliyor...");

        // Tüm Canvas'ları bul ve UI elemanlarına hover ve tasarım ekle
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            // Hover efektlerini butonlara ekle
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn.GetComponent<UIAnimator.ButtonHoverEffect>() == null)
                {
                    btn.gameObject.AddComponent<UIAnimator.ButtonHoverEffect>();
                }
            }

            // Eğer bu bir ana menü veya bilgi paneli ise, arkasına koyu bir overlay (Dim) ekleyelim
            // Panellerin adlarını kontrol et
            Transform hudPanel = canvas.transform.Find("HUDPanel");
            Transform briefingPanel = canvas.transform.Find("BriefingPanel");
            Transform reportPanel = canvas.transform.Find("ReportPanel");
            Transform gameOverPanel = canvas.transform.Find("GameOverPanel");
            Transform pausePanel = canvas.transform.Find("PausePanel");

            if (briefingPanel != null) SetupPanelTheme(briefingPanel);
            if (reportPanel != null) SetupPanelTheme(reportPanel);
            if (gameOverPanel != null) SetupPanelTheme(gameOverPanel);
            if (pausePanel != null) SetupPanelTheme(pausePanel);

            // HUD Tasarım iyileştirmesi
            if (hudPanel != null)
            {
                EnhanceHUD(hudPanel);
            }
        }
    }

    private void SetupPanelTheme(Transform panelTransform)
    {
        // Panelin arkasına karanlık overlay ekle
        // Eğer panelin kendisi zaten tam ekran bir Image ise rengini değiştir, yoksa ekle
        Image panelImage = panelTransform.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelTransform.gameObject.AddComponent<Image>();
        }

        if (panelImage != null)
        {
            // Şık, modern koyu gri / antrasit bir arkaplan (Endüstriyel & Temiz Görünüm)
            panelImage.color = new Color(0.12f, 0.13f, 0.15f, 0.98f);
        }

        // Panel içindeki metinlerin tipografisini geliştir
        TextMeshProUGUI[] texts = panelTransform.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI txt in texts)
        {
            // Başlıklara hafif gölge ekle ve rengini sarımsı/turuncu tonlarında vurgula
            if (txt.gameObject.name.ToLower().Contains("title"))
            {
                txt.fontStyle = FontStyles.Bold;
                txt.color = new Color(1.0f, 0.75f, 0.1f); // Modern uyarı sarısı
                
                // Başlıkların arkasına siyah, belirgin bir gölge ekle
                txt.outlineWidth = 0.15f;
                txt.outlineColor = new Color32(0, 0, 0, 255);
            }
        }
    }

    private void EnhanceHUD(Transform hudTransform)
    {
        // HUD elemanlarının okunabilirliğini artırmak için arkalarına hafif siyah barlar ekle
        TextMeshProUGUI[] texts = hudTransform.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI txt in texts)
        {
            if (txt.gameObject.name.Contains("Text"))
            {
                // Shadow efekti ver
                // Eğer yoksa outline/shadow material kullanmak zor olabilir, o yüzden en basiti arkasına karanlık panel koymak veya gölge açmak
                txt.fontStyle = FontStyles.Bold;
                
                // Unity TextMeshPro Outline
                txt.outlineWidth = 0.2f;
                txt.outlineColor = new Color32(0, 0, 0, 255);
            }
        }
    }
}
