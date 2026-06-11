using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIThemeEnhancer : MonoBehaviour
{
    private void Start()
    {
        // Start as a coroutine to spread UI styling work across multiple frames
        StartCoroutine(EnhanceUICoroutine());
    }

    private IEnumerator EnhanceUICoroutine()
    {
        // Wait 0.5s to let other scripts complete their initialization
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[UIThemeEnhancer] UI Teması İyileştiriliyor (Coroutine)...");

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;

            // Add hover effects to buttons, yielding every few buttons to prevent lag
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            int buttonCount = 0;
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;
                if (btn.GetComponent<UIAnimator.ButtonHoverEffect>() == null)
                {
                    btn.gameObject.AddComponent<UIAnimator.ButtonHoverEffect>();
                }
                
                buttonCount++;
                if (buttonCount % 3 == 0) // Process 3 buttons per frame
                {
                    yield return null;
                }
            }

            Transform hudPanel = canvas.transform.Find("HUDPanel");
            Transform briefingPanel = canvas.transform.Find("BriefingPanel");
            Transform reportPanel = canvas.transform.Find("ReportPanel");
            Transform gameOverPanel = canvas.transform.Find("GameOverPanel");
            Transform pausePanel = canvas.transform.Find("PausePanel");

            if (briefingPanel != null)
            {
                yield return StartCoroutine(SetupPanelThemeCoroutine(briefingPanel));
            }
            if (reportPanel != null)
            {
                yield return StartCoroutine(SetupPanelThemeCoroutine(reportPanel));
            }
            if (gameOverPanel != null)
            {
                yield return StartCoroutine(SetupPanelThemeCoroutine(gameOverPanel));
            }
            if (pausePanel != null)
            {
                yield return StartCoroutine(SetupPanelThemeCoroutine(pausePanel));
            }

            if (hudPanel != null)
            {
                yield return StartCoroutine(EnhanceHUDCoroutine(hudPanel));
            }
        }
    }

    private IEnumerator SetupPanelThemeCoroutine(Transform panelTransform)
    {
        if (panelTransform == null) yield break;

        // Add dark overlay background to panels
        Image panelImage = panelTransform.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelTransform.gameObject.AddComponent<Image>();
        }

        if (panelImage != null)
        {
            panelImage.color = new Color(0.12f, 0.13f, 0.15f, 0.98f);
        }

        // Improve typography of panel texts
        TextMeshProUGUI[] texts = panelTransform.GetComponentsInChildren<TextMeshProUGUI>(true);
        int textCount = 0;
        foreach (TextMeshProUGUI txt in texts)
        {
            if (txt == null) continue;

            if (txt.gameObject.name.ToLower().Contains("title"))
            {
                txt.fontStyle = FontStyles.Bold;
                txt.color = new Color(1.0f, 0.75f, 0.1f); // Modern warning yellow
                
                txt.outlineWidth = 0.15f;
                txt.outlineColor = new Color32(0, 0, 0, 255);
            }

            textCount++;
            if (textCount % 5 == 0) // Process 5 texts per frame
            {
                yield return null;
            }
        }
    }

    private IEnumerator EnhanceHUDCoroutine(Transform hudTransform)
    {
        if (hudTransform == null) yield break;

        TextMeshProUGUI[] texts = hudTransform.GetComponentsInChildren<TextMeshProUGUI>(true);
        int textCount = 0;
        foreach (TextMeshProUGUI txt in texts)
        {
            if (txt == null) continue;

            if (txt.gameObject.name.Contains("Text"))
            {
                txt.fontStyle = FontStyles.Bold;
                txt.outlineWidth = 0.2f;
                txt.outlineColor = new Color32(0, 0, 0, 255);
            }

            textCount++;
            if (textCount % 5 == 0) // Process 5 texts per frame
            {
                yield return null;
            }
        }
    }
}
