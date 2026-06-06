using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın ortasına sadece küçük, temiz ve dinamik bir nokta ekler.
/// Kutuya bakıldığında yeşil olur ve hafifçe büyür. Kutu taşınırken küçülür/solur.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("Nokta Boyut Ayarları")]
    [SerializeField] private float normalDotSize = 8f;   // Normal küçük nokta boyutu
    [SerializeField] private float hoverDotSize = 12f;   // Odaklanınca büyüme boyutu
    [SerializeField] private float holdingDotSize = 4f;  // Taşırken küçülme boyutu

    [Header("Nokta Renk Ayarları")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private Color hoverColor = new Color(0.1f, 0.9f, 0.5f, 0.95f); // Neon yeşil
    [SerializeField] private Color holdingColor = new Color(1f, 1f, 1f, 0.2f);      // Çok soluk beyaz

    [Header("Animasyon Hızı")]
    [SerializeField] private float transitionSpeed = 15f;

    private Canvas crosshairCanvas;
    private GameObject crosshairRoot;
    private PlayerInteraction playerInteraction;

    private Image dotImage;
    private RectTransform dotRect;

    // Dinamik Değerler (Lerp için)
    private Color currentColor;
    private float currentDotSize;

    void Start()
    {
        playerInteraction = GetComponent<PlayerInteraction>();
        
        currentColor = normalColor;
        currentDotSize = normalDotSize;

        CreateCrosshair();
    }

    void Update()
    {
        // GameManager kontrolü ile sadece oynanırken göster
        if (crosshairRoot != null && GameManager.Instance != null)
        {
            bool show = GameManager.Instance.CurrentState == GameManager.GameState.Playing;
            if (crosshairRoot.activeSelf != show)
            {
                crosshairRoot.SetActive(show);
            }
        }

        if (crosshairRoot != null && crosshairRoot.activeSelf)
        {
            UpdateCrosshairDynamics();
        }
    }

    private void UpdateCrosshairDynamics()
    {
        bool isHovering = playerInteraction != null && playerInteraction.IsHoveringInteractable;
        bool isHolding = playerInteraction != null && playerInteraction.IsHoldingObject;

        Color targetColor = normalColor;
        float targetDotSize = normalDotSize;

        if (isHolding)
        {
            // Kutu taşırken çok küçük ve soluk nokta
            targetColor = holdingColor;
            targetDotSize = holdingDotSize;
        }
        else if (isHovering)
        {
            // Kutuya bakarken yeşile dönsün ve hafifçe büyüsün
            targetColor = hoverColor;
            targetDotSize = hoverDotSize;
        }

        // Pürüzsüz geçiş (Lerp)
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * transitionSpeed);
        currentDotSize = Mathf.Lerp(currentDotSize, targetDotSize, Time.deltaTime * transitionSpeed);

        // Değerleri Uygula
        if (dotImage != null) dotImage.color = currentColor;
        if (dotRect != null) dotRect.sizeDelta = new Vector2(currentDotSize, currentDotSize);
    }

    private void CreateCrosshair()
    {
        // Yeni bir Canvas oluştur (Overlay, her şeyin en üstünde)
        GameObject canvasObj = new GameObject("CrosshairCanvas");
        canvasObj.transform.SetParent(transform);
        crosshairCanvas = canvasObj.AddComponent<Canvas>();
        crosshairCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        crosshairCanvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Root merkez obje
        crosshairRoot = new GameObject("CrosshairRoot");
        crosshairRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = crosshairRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        // Sadece tek bir nokta oluştur
        GameObject dotObj = new GameObject("Dot", typeof(RectTransform));
        dotObj.transform.SetParent(rootRect, false);

        dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = new Vector2(normalDotSize, normalDotSize);

        dotImage = dotObj.AddComponent<Image>();
        dotImage.color = normalColor;
        dotImage.raycastTarget = false;

        // Okunabilirlik için siyah gölge ekle
        Shadow shadow = dotObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }
}
