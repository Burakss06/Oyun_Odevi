using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIAnimator : MonoBehaviour
{
    // Butonlar için Hover efekti ekleyen yardımcı bileşen
    public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 originalScale;
        private Color originalColor;
        private Image buttonImage;
        
        public float scaleMultiplier = 1.05f;
        public float clickScaleMultiplier = 0.95f;
        public float transitionSpeed = 15f;
        
        private Vector3 targetScale;
        private bool isInitialized = false;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (isInitialized) return;
            originalScale = transform.localScale;
            targetScale = originalScale;
            buttonImage = GetComponent<Image>();
            if (buttonImage != null) originalColor = buttonImage.color;
            isInitialized = true;
        }

        private void Update()
        {
            if (isInitialized)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!GetComponent<Button>().interactable) return;
            targetScale = originalScale * scaleMultiplier;
            if (buttonImage != null)
            {
                // Biraz parlaklaştır
                float h, s, v;
                Color.RGBToHSV(originalColor, out h, out s, out v);
                buttonImage.color = Color.HSVToRGB(h, s, Mathf.Clamp01(v * 1.2f));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
            if (buttonImage != null) buttonImage.color = originalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!GetComponent<Button>().interactable) return;
            targetScale = originalScale * clickScaleMultiplier;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!GetComponent<Button>().interactable) return;
            targetScale = originalScale * scaleMultiplier;
        }

        private void OnDisable()
        {
            if (isInitialized)
            {
                transform.localScale = originalScale;
                targetScale = originalScale;
                if (buttonImage != null) buttonImage.color = originalColor;
            }
        }
    }

    // CanvasGroup animasyon fonksiyonları
    public static IEnumerator FadeInAndScale(GameObject panel, float duration = 0.25f)
    {
        panel.SetActive(true);
        
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        
        cg.alpha = 0f;
        
        // Sadece içindeki çocuk elemanı (pencereyi) scale et, arka plan overlay varsa onu scale etme
        Transform windowTransform = panel.transform;
        if (panel.transform.childCount > 0)
        {
            // Panel içinde arka plan resmi ve asıl pencere ayrıysa, asıl pencereyi büyüt
            // Varsayılan olarak panelin kendisini scale ediyoruz
            windowTransform = panel.transform;
        }
        
        Vector3 originalScale = Vector3.one;
        windowTransform.localScale = originalScale * 0.8f;
        
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            // Ease out cubic
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            cg.alpha = Mathf.Lerp(0f, 1f, easeT);
            windowTransform.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, easeT);
            
            yield return null;
        }
        
        cg.alpha = 1f;
        windowTransform.localScale = originalScale;
    }

    public static IEnumerator FadeOutAndScale(GameObject panel, float duration = 0.2f)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        
        Transform windowTransform = panel.transform;
        Vector3 originalScale = windowTransform.localScale;
        
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            // Ease in
            float easeT = t * t;
            
            cg.alpha = Mathf.Lerp(1f, 0f, easeT);
            windowTransform.localScale = Vector3.Lerp(originalScale, originalScale * 0.9f, easeT);
            
            yield return null;
        }
        
        cg.alpha = 0f;
        panel.SetActive(false);
        windowTransform.localScale = originalScale; // Sıfırla
    }
}
