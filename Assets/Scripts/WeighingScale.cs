using UnityEngine;
using TMPro;

// Tartı sistemi. Kutuları tartıp ağırlığı ekranda gösterir.
public class WeighingScale : MonoBehaviour
{
    [Header("Ekran Referansı")]
    [SerializeField] private TextMeshPro displayText;

    [Header("Kutu Yerleştirme Noktası")]
    [SerializeField] private Transform snapPoint;

    [Header("Ses Efekti")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip weighSound;

    [Header("Görsel Ayarlar")]
    [SerializeField] private Color normalWeightColor = new Color(0.2f, 1f, 0.4f);
    [SerializeField] private Color overweightColor = new Color(1f, 0.15f, 0.15f);
    [SerializeField] private Color idleColor = new Color(0.3f, 0.8f, 0.3f, 0.6f);
    [SerializeField] private float overweightThreshold = 10.0f;

    private BoxController currentBox = null;
    private Rigidbody currentBoxRb = null;
    private float displayedWeight = 0f;
    private float targetWeight = 0f;

    public bool HasBox => currentBox != null;
    public BoxController CurrentBox => currentBox;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (weighSound == null)
        {
            weighSound = Resources.Load<AudioClip>("Audio/weigh_beep");
        }

        if (snapPoint == null)
        {
            GameObject sp = new GameObject("SnapPoint");
            sp.transform.SetParent(transform, false);
            sp.transform.localPosition = new Vector3(0f, 0.395f, 0f);
            snapPoint = sp.transform;
        }
        else
        {
            snapPoint.localPosition = new Vector3(0f, 0.395f, 0f);
        }

        if (displayText != null)
        {
            displayText.alignment = TextAlignmentOptions.Center;
            displayText.fontSize = 1.4f;
            displayText.fontStyle = FontStyles.Bold;
            displayText.margin = new Vector4(0f, 0f, 0f, 0f);
            displayText.enableWordWrapping = false;
        }

        UpdateDisplay(0f, true);
    }

    void Update()
    {
        // Ağırlık geçiş animasyonu (lerp)
        if (!Mathf.Approximately(displayedWeight, targetWeight))
        {
            displayedWeight = Mathf.Lerp(displayedWeight, targetWeight, Time.deltaTime * 5f);

            if (Mathf.Abs(displayedWeight - targetWeight) < 0.1f)
            {
                displayedWeight = targetWeight;
            }

            // Sayı artarken küçük dalgalanma efekti
            float animatedWeight = displayedWeight;
            if (displayedWeight != targetWeight && targetWeight > 0.01f)
            {
                animatedWeight += Random.Range(-0.15f, 0.15f);
                if (animatedWeight < 0f) animatedWeight = 0f;
            }

            UpdateDisplay(animatedWeight, false);
        }

        // Kutu pozisyonunu sabitle
        if (currentBox != null && snapPoint != null)
        {
            currentBox.transform.position = snapPoint.position;
            currentBox.transform.rotation = Quaternion.Euler(0f, currentBox.transform.eulerAngles.y, 0f);
        }

        // Sürpriz kutu için gökkuşağı rengi dalgalanması
        if (currentBox != null && currentBox.isMysteryBox && displayText != null)
        {
            float speed = 0.5f;
            float hue = Mathf.Repeat(Time.time * speed, 1f);
            displayText.color = Color.HSVToRGB(hue, 1f, 1f);
        }
    }

    // Kutuyu tartıya koy
    public void PlaceBox(BoxController box)
    {
        if (box == null || currentBox != null) return;

        currentBox = box;
        currentBoxRb = box.GetComponent<Rigidbody>();

        // Fiziği durdur
        if (currentBoxRb != null)
        {
            currentBoxRb.isKinematic = true;
            currentBoxRb.useGravity = false;
            currentBoxRb.linearVelocity = Vector3.zero;
            currentBoxRb.angularVelocity = Vector3.zero;
        }

        box.transform.position = snapPoint.position;
        box.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        targetWeight = box.Weight;

        if (audioSource != null && weighSound != null)
        {
            audioSource.PlayOneShot(weighSound);
        }
    }

    // Kutuyu tartıdan geri al
    public BoxController RetrieveBox()
    {
        if (currentBox == null) return null;

        BoxController box = currentBox;

        if (currentBoxRb != null)
        {
            currentBoxRb.isKinematic = true; // Elde tutulacağı için kinematic kalır
            currentBoxRb.useGravity = false;
        }

        currentBox = null;
        currentBoxRb = null;
        targetWeight = 0f;

        return box;
    }

    private void UpdateDisplay(float weight, bool isIdle)
    {
        if (displayText == null) return;

        if (currentBox != null && currentBox.isMysteryBox)
        {
            displayText.text = "??? kg";
            displayText.color = Color.magenta;
            return;
        }

        if (isIdle && weight <= 0.01f)
        {
            displayText.text = "0.0 kg";
            displayText.color = Color.white;
        }
        else
        {
            displayText.text = $"{weight:F1} kg";
            displayText.color = Color.white;
        }
    }
}
