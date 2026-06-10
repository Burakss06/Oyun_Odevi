using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    public enum PowerUpType
    {
        Confusion,           // Ters Kontroller (Yönleri ters çevirir - Debuff)
        InspectorPermission, // Müfettişin İzni (1 Ekstra Hata Hakkı)
        QuietShift,          // Sakin Vardiya (Bant Yavaşlatıcı)
        SharpEye,            // Keskin Göz (Hata Vurgulayıcı)
        CargoMagnet,         // Kargo Mıknatısı (Uzak Etkileşim)
        BeltFreeze,          // Bant Dondurucu (Bantları Durdurur)
        TurboShoes,          // Turbo Ayakkabı (Oyuncu Hızlandırıcı)
        MuddyBoots           // Çamurlu Çizmeler (Oyuncu Yavaşlatıcı)
    }

    [Header("Güçlendirici Ayarları")]
    public PowerUpType Type;

    [Header("Görsel Referanslar")]
    [Tooltip("Soru İşareti Metin objesi (TextMesh)")]
    public TextMesh questionMarkText;

    // Dalgalanma (Süzülme) Animasyonu İçin
    private Vector3 startPos;
    private float floatSpeed = 2f;
    private float floatHeight = 0.5f;

    public void Initialize(PowerUpType type, bool mysteryStatus)
    {
        Type = type;
        startPos = transform.position;
        // Renk atamasını Update'de gökkuşağı yapacağımız için buradan kaldırdık
    }

    private void Start()
    {
        if (questionMarkText == null)
        {
            questionMarkText = GetComponentInChildren<TextMesh>();
        }
    }

    private void Update()
    {
        // Yukarı-Aşağı süzülme animasyonu ve yavaşça dönme
        transform.position = startPos + new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0);
        transform.Rotate(Vector3.up, 45f * Time.deltaTime);

        // Soru İşareti Rengarenk (Gökkuşağı) Efekti
        if (questionMarkText != null)
        {
            // Zaman (Time.time) bazlı sürekli değişen bir renk (Hue kaydırması)
            float hue = Mathf.Repeat(Time.time * 0.5f, 1f); 
            questionMarkText.color = Color.HSVToRGB(hue, 1f, 1f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player etiketi veya PlayerController kontrolü
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            // Eğer isMystery ise, alındığında asıl özelliğini ortaya çıkarabiliriz veya direkt etkiyi gösterebiliriz.
            Debug.Log($"[PowerUp] Oyuncu bir güçlendirici aldı: {Type}");
            
            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.CollectPowerUp(this);
            }
            
            // Obje Havuzuna geri gönder (Deaktif et)
            gameObject.SetActive(false);
        }
    }
}
