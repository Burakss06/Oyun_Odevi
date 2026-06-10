using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Spawn Ayarları")]
    [Tooltip("Güçlendiricilerin belireceği hangar alanının sınırları (Örn: Boş bir BoxCollider)")]
    public Collider spawnArea;
    public float minSpawnInterval = 20f;
    public float maxSpawnInterval = 30f;
    public int maxActivePowerUps = 3;

    [Header("Prefablar")]
    [Tooltip("İçinde ikonlar ve soru işareti olan ana PowerUp prefabı")]
    public GameObject powerUpPrefab;

    // Obje Havuzu (Object Pooling)
    private List<PowerUpController> pool = new List<PowerUpController>();
    private int currentActiveCount = 0;
    
    private Coroutine spawnCoroutine;

    [Header("Ses Ayarları")]
    private AudioSource audioSource;
    private AudioClip popUpSound;
    private AudioClip rewardSound;
    private AudioClip penaltySound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            audioSource = gameObject.AddComponent<AudioSource>();
            popUpSound = Resources.Load<AudioClip>("Audio/pop_up");
            rewardSound = Resources.Load<AudioClip>("Audio/reward");
            penaltySound = Resources.Load<AudioClip>("Audio/wrong_buzzer");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 5 adet güçlendiriciyi baştan havuza doldur
        if (powerUpPrefab != null)
        {
            for (int i = 0; i < 5; i++)
            {
                GameObject obj = Instantiate(powerUpPrefab, transform);
                obj.SetActive(false);
                PowerUpController pu = obj.GetComponent<PowerUpController>();
                if (pu != null) pool.Add(pu);
            }
        }
        else
        {
            Debug.LogError("[PowerUpManager] PowerUp Prefab atanmamış!");
        }

        StartSpawning();
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
            spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            float timer = 0f;
            
            while (timer < waitTime)
            {
                // Sadece oyun oynanırken zamanı say (Briefing veya menülerde durur)
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
                {
                    timer += Time.deltaTime;
                }
                yield return null;
            }
            
            UpdateActiveCount();
            if (currentActiveCount < maxActivePowerUps)
            {
                SpawnPowerUp();
            }
        }
    }

    private void UpdateActiveCount()
    {
        currentActiveCount = 0;
        foreach (var pu in pool)
        {
            if (pu.gameObject.activeInHierarchy)
                currentActiveCount++;
        }
    }

    private void SpawnPowerUp()
    {
        PowerUpController pu = GetPooledPowerUp();
        if (pu == null) return;

        // Rastgele tip seçimi (Keskin Göz ihtimali %5 gibi düşük)
        PowerUpController.PowerUpType selectedType = GetRandomPowerUpType();

        // Pozisyon belirleme
        Vector3 spawnPos = GetRandomPositionInArea();

        pu.transform.position = spawnPos;
        pu.Initialize(selectedType, false); // isMystery artık kullanılmıyor, şimdilik false geçiyoruz.
        pu.gameObject.SetActive(true);

        ShowNotification("Hangarda bir güçlendirici belirdi!");
        if (audioSource != null && popUpSound != null)
        {
            audioSource.PlayOneShot(popUpSound);
        }
    }

    private PowerUpController.PowerUpType GetRandomPowerUpType()
    {
        float rand = Random.value; // 0.0 ile 1.0 arası
        
        // İhtimaller:
        // Keskin Göz: %10
        // Çamurlu Çizmeler (Debuff): %10
        // Ters Kontroller (Debuff): %15
        // Müfettişin İzni: %13
        // Sakin Vardiya: %13
        // Kargo Mıknatısı: %13
        // Bant Dondurucu: %13
        // Turbo Ayakkabı: %13
        if (rand <= 0.10f) return PowerUpController.PowerUpType.SharpEye;
        else if (rand <= 0.20f) return PowerUpController.PowerUpType.MuddyBoots;
        else if (rand <= 0.35f) return PowerUpController.PowerUpType.Confusion;
        else if (rand <= 0.48f) return PowerUpController.PowerUpType.InspectorPermission;
        else if (rand <= 0.61f) return PowerUpController.PowerUpType.QuietShift;
        else if (rand <= 0.74f) return PowerUpController.PowerUpType.CargoMagnet;
        else if (rand <= 0.87f) return PowerUpController.PowerUpType.BeltFreeze;
        else return PowerUpController.PowerUpType.TurboShoes;
    }

    private Vector3 GetRandomPositionInArea()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("[PowerUpManager] Spawn alanı (BoxCollider) atanmamış! Y ekseninde 1.5 birim yukarıda belirecek.");
            return transform.position + Vector3.up * 1.5f;
        }

        Bounds bounds = spawnArea.bounds;
        
        // Raf veya kutuların içine/üstüne veya banta denk gelmemesi için 50 defaya kadar boş yer ara
        for (int i = 0; i < 50; i++)
        {
            float rx = Random.Range(bounds.min.x, bounds.max.x);
            float rz = Random.Range(bounds.min.z, bounds.max.z);
            
            // Seçilen noktanın 10 metre yukarısından aşağıya doğru bir ışın gönder
            Vector3 rayStart = new Vector3(rx, bounds.max.y + 10f, rz);
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 30f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                // Çarptığı obje zemin olmalı (yüksekliği düşük). Bant veya kutu olmamalı.
                string hitName = hit.collider.gameObject.name.ToLower();
                bool isBeltOrBox = hitName.Contains("belt") || hitName.Contains("conveyor") || hitName.Contains("box") || hitName.Contains("kutu");

                if (hit.point.y < 0.5f && !isBeltOrBox)
                {
                    Vector3 potentialPos = new Vector3(rx, 0.85f, rz);

                    // Ekstra güvenlik: Etrafında yarım metre çapında kutu veya bant var mı diye kontrol et
                    if (!Physics.CheckSphere(potentialPos, 0.4f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    {
                        return potentialPos; // Güvenli alan!
                    }
                }
            }
        }

        // Eğer 50 denemede bulamazsa, sahnenin tamamen dışına / çok güvenli köşelere atmaktansa
        // belirli ve genelde boş olan sabit bir koordinata gönderelim (Örn: Hangarin sol köşesi)
        return new Vector3(-3f, 0.85f, -3f);
    }

    private PowerUpController GetPooledPowerUp()
    {
        foreach (var pu in pool)
        {
            if (!pu.gameObject.activeInHierarchy)
                return pu;
        }
        return null;
    }

    public void CollectPowerUp(PowerUpController pu)
    {
        if (audioSource != null)
        {
            if (pu.Type == PowerUpController.PowerUpType.MuddyBoots || pu.Type == PowerUpController.PowerUpType.Confusion)
            {
                if (penaltySound != null) audioSource.PlayOneShot(penaltySound);
            }
            else
            {
                if (rewardSound != null) audioSource.PlayOneShot(rewardSound);
            }
        }

        Vector3 pos = pu.transform.position + Vector3.up * 1f; // Güçlendiricinin biraz üstünden başlar
        
        switch (pu.Type)
        {
            case PowerUpController.PowerUpType.Confusion:
                StartCoroutine(ConfusionRoutine());
                SpawnFloatingText("Yönler Ters!", pos, new Color(0.8f, 0.2f, 0.8f));
                ShowNotification("Ters Kontroller: Hareket Yönlerin 8 Saniyeliğine Tersine Döndü!", new Color(0.8f, 0.2f, 0.8f));
                break;
            case PowerUpController.PowerUpType.InspectorPermission:
                if (GameManager.Instance != null) GameManager.Instance.DecreaseErrorCount();
                SpawnFloatingText("+1 Hata Hakkı!", pos, Color.green);
                ShowNotification("Müfettiş İzni: +1 Hata Hakkı Kazandın!", Color.green);
                break;
            case PowerUpController.PowerUpType.QuietShift:
                StartCoroutine(QuietShiftRoutine());
                SpawnFloatingText("Bantlar Yavaşladı!", pos, new Color(1f, 0.5f, 0f)); // Turuncu
                ShowNotification("Sakin Vardiya: Bantlar 15 Saniyeliğine Yarı Hızına Düştü!", new Color(1f, 0.5f, 0f));
                break;
            case PowerUpController.PowerUpType.SharpEye:
                StartCoroutine(SharpEyeRoutine());
                SpawnFloatingText("Keskin Göz!", pos, Color.yellow);
                ShowNotification("Keskin Göz: Hatalı Kutular 10 Saniyeliğine Parlayacak!", Color.yellow);
                break;
            case PowerUpController.PowerUpType.CargoMagnet:
                StartCoroutine(CargoMagnetRoutine());
                SpawnFloatingText("Mıknatıs Aktif!", pos, Color.magenta);
                ShowNotification("Kargo Mıknatısı: Kutuları Uzaktan Çekme Gücü (3x Menzil) 15 Saniyeliğine Aktif!", Color.magenta);
                break;
            case PowerUpController.PowerUpType.BeltFreeze:
                StartCoroutine(BeltFreezeRoutine());
                SpawnFloatingText("Bantlar Durdu!", pos, new Color(0.5f, 0.8f, 1f));
                ShowNotification("Bant Dondurucu: Bantlar 6 Saniyeliğine Tamamen Durduruldu!", new Color(0.5f, 0.8f, 1f));
                break;
            case PowerUpController.PowerUpType.TurboShoes:
                StartCoroutine(TurboShoesRoutine());
                SpawnFloatingText("Turbo Hız!", pos, new Color(1f, 0.2f, 0.2f));
                ShowNotification("Turbo Ayakkabı: Koşma ve Yürüme Hızı 12 Saniyeliğine %50 Artırıldı!", new Color(1f, 0.2f, 0.2f));
                break;
            case PowerUpController.PowerUpType.MuddyBoots:
                StartCoroutine(MuddyBootsRoutine());
                SpawnFloatingText("Yavaşladın!", pos, new Color(0.5f, 0.25f, 0f));
                ShowNotification("Çamurlu Çizmeler: Hareket Hızın 20 Saniyeliğine %40 Azaldı!", new Color(0.5f, 0.25f, 0f));
                break;
        }
    }

    private void SpawnFloatingText(string message, Vector3 position, Color color)
    {
        GameObject textObj = new GameObject("FloatingText");
        textObj.transform.position = position;
        
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = message;
        tm.color = color;
        tm.fontSize = 80; // Büyütüldü
        tm.characterSize = 0.1f; // Büyütüldü
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = defaultFont;
        if (defaultFont != null) tm.GetComponent<Renderer>().sharedMaterial = defaultFont.material;

        textObj.AddComponent<FloatingTextAnimator>();
    }

    private Coroutine notificationCoroutine;

    private void ShowNotification(string message, Color? customColor = null)
    {
        GameObject textObj = GameObject.Find("PowerUpNotificationText");
        if (textObj != null)
        {
            TMPro.TextMeshProUGUI tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
                Color targetColor = customColor ?? new Color(1f, 0.5f, 0f); // Varsayılan turuncu
                notificationCoroutine = StartCoroutine(NotificationRoutine(tmp, message, targetColor));
            }
        }
    }

    private IEnumerator NotificationRoutine(TMPro.TextMeshProUGUI tmp, string message, Color targetColor)
    {
        tmp.text = message;
        // Fade in
        tmp.color = new Color(targetColor.r, targetColor.g, targetColor.b, 1f);
        
        yield return new WaitForSeconds(5f); // Ekranda kalma süresi 3'ten 5 saniyeye uzatıldı
        
        // Fade out
        float fadeOutTime = 1f;
        float t = 0;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            tmp.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        tmp.text = "";
    }

    private IEnumerator QuietShiftRoutine()
    {
        Debug.Log("[PowerUp] Sakin Vardiya aktif! Bantlar 15 saniyeliğine yavaşlıyor.");
        ConveyorBeltPush.GlobalSpeedMultiplier = 0.5f; // Yarı hız
        yield return new WaitForSeconds(15f);
        ConveyorBeltPush.GlobalSpeedMultiplier = 1.0f; // Normal hız
        Debug.Log("[PowerUp] Sakin Vardiya bitti.");
    }

    private IEnumerator SharpEyeRoutine()
    {
        Debug.Log("[PowerUp] Keskin Göz aktif! Hatalı kutular 10 saniyeliğine parlayacak.");
        BoxController.IsSharpEyeActive = true;
        yield return new WaitForSeconds(10f);
        BoxController.IsSharpEyeActive = false;
        Debug.Log("[PowerUp] Keskin Göz bitti.");
    }

    private IEnumerator CargoMagnetRoutine()
    {
        Debug.Log("[PowerUp] Kargo Mıknatısı aktif! Menzil 3 katına çıkıyor.");
        PlayerInteraction.InteractionDistanceMultiplier = 3.0f;
        yield return new WaitForSeconds(15f);
        PlayerInteraction.InteractionDistanceMultiplier = 1.0f;
        Debug.Log("[PowerUp] Kargo Mıknatısı bitti.");
    }

    private IEnumerator BeltFreezeRoutine()
    {
        Debug.Log("[PowerUp] Bant Dondurucu aktif! Bantlar 6 saniyeliğine duruyor.");
        ConveyorBeltPush.GlobalSpeedMultiplier = 0.0f;
        yield return new WaitForSeconds(6f);
        ConveyorBeltPush.GlobalSpeedMultiplier = 1.0f;
        Debug.Log("[PowerUp] Bant Dondurucu bitti.");
    }

    private IEnumerator TurboShoesRoutine()
    {
        Debug.Log("[PowerUp] Turbo Ayakkabı aktif! Oyuncu hızı %50 artıyor.");
        PlayerController.SpeedMultiplier = 1.5f;
        yield return new WaitForSeconds(12f);
        PlayerController.SpeedMultiplier = 1.0f;
        Debug.Log("[PowerUp] Turbo Ayakkabı bitti.");
    }

    private IEnumerator MuddyBootsRoutine()
    {
        Debug.Log("[PowerUp] Çamurlu Çizmeler aktif! Oyuncu hızı %40 azalıyor.");
        PlayerController.SpeedMultiplier = 0.6f;
        yield return new WaitForSeconds(20f);
        PlayerController.SpeedMultiplier = 1.0f;
        Debug.Log("[PowerUp] Çamurlu Çizmeler bitti.");
    }

    private IEnumerator ConfusionRoutine()
    {
        Debug.Log("[PowerUp] Ters Kontroller aktif! Yönler 8 saniyeliğine tersine dönüyor.");
        PlayerController.InvertControls = true;
        yield return new WaitForSeconds(8f);
        PlayerController.InvertControls = false;
        Debug.Log("[PowerUp] Ters Kontroller bitti.");
    }
}
