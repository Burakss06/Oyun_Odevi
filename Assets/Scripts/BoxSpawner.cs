using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoxSpawner : MonoBehaviour
{
    public static BoxSpawner Instance { get; private set; }

    [Header("Spawner Ayarları")]
    [SerializeField] private List<GameObject> boxPrefabs = new List<GameObject>();
    [SerializeField] private Transform spawnPoint; // Geriye uyumluluk için tekil spawn noktası
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>(); // Çoklu spawn noktaları listesi
    [SerializeField] private float spawnInterval = 5f;

    private Coroutine spawnCoroutine;
    private bool isSpawning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartSpawning()
    {
        if (isSpawning) return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // İlk kutuyu hemen üretmek yerine 1.5 saniye bekle (gün başlangıç akıcılığı için)
        yield return new WaitForSeconds(1.5f);

        while (isSpawning && GameManager.Instance != null && DayManager.Instance != null)
        {
            DayConfig config = DayManager.Instance.GetCurrentDayConfig();

            // Eğer o gün için hedeflenen toplam kutu sayısına ulaşıldıysa üretimi durdur
            if (GameManager.Instance.TotalSpawnedBoxes >= config.totalBoxesToSpawn)
            {
                isSpawning = false;
                yield break;
            }

            SpawnBox(config);

            // Bir sonraki kutu üretimi için bekle (Hızlandırılabilir Zamanlayıcı)
            float timer = 0f;
            while (timer < spawnInterval)
            {
                if (!isSpawning) yield break;
                
                // Bant hız çarpanını zamana ekle ki bant hızlandığında kutular da aynı oranda hızlı üretilsin
                timer += Time.deltaTime * ConveyorBeltPush.PlayerSpeedMultiplier;
                yield return null;
            }
        }
    }

    private void SpawnBox(DayConfig config)
    {
        if (boxPrefabs == null || boxPrefabs.Count == 0)
        {
            Debug.LogError("BoxSpawner: Prefab listesi boş! Lütfen kutu prefablarını atayın.");
            return;
        }

        // Çoklu spawn noktalarından veya tekil spawn noktasından birini seç
        Transform selectedSpawnPoint = spawnPoint;
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            // Rastgele bir spawn noktası seç
            selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        }
        
        if (selectedSpawnPoint == null)
        {
            Debug.LogWarning("BoxSpawner: Herhangi bir Spawn Noktası atanmamış, spawner objesinin kendi pozisyonu kullanılacak.");
            selectedSpawnPoint = transform;
        }

        // 1. Hedef paleti seç: %50 Kabul, %50 Ret (Dengeyi sağlamak için)
        PalletTrigger.PalletType targetPallet = (Random.value > 0.5f) ? PalletTrigger.PalletType.Kabul : PalletTrigger.PalletType.Ret;

        // 2. Hedef palete uygun şekillerin listesini çıkar
        List<BoxController.BoxShape> validShapes = new List<BoxController.BoxShape>();
        
        if (DayManager.Instance != null && DayManager.Instance.CurrentDay == 1)
        {
            // 1. Gün kuralları sabit: Closed -> Kabul, Opened -> Ret.
            if (targetPallet == PalletTrigger.PalletType.Kabul) validShapes.Add(BoxController.BoxShape.Closed);
            else validShapes.Add(BoxController.BoxShape.Opened);
        }
        else if (GameManager.Instance != null && GameManager.Instance.DailyRules != null)
        {
            if (targetPallet == PalletTrigger.PalletType.Kabul)
            {
                // Kabul kutusu için şeklin de Kabul olması gerekir
                foreach (var rule in GameManager.Instance.DailyRules)
                {
                    if (rule.Value == PalletTrigger.PalletType.Kabul) validShapes.Add(rule.Key);
                }
            }
            else
            {
                // Ret kutusu için şekil ya Kabul ya Ret olabilir.
                // Çeşitlilik için bütün şekilleri ekleyelim, InitializeBox içindeki kurallar onu Ret yapacaktır.
                foreach (var rule in GameManager.Instance.DailyRules)
                {
                    validShapes.Add(rule.Key);
                }
            }
        }

        if (validShapes.Count == 0)
        {
            validShapes.Add(BoxController.BoxShape.Closed);
        }

        BoxController.BoxShape selectedShape = validShapes[Random.Range(0, validShapes.Count)];

        // Şekle uygun prefabı bul
        GameObject selectedPrefab = boxPrefabs[0];
        foreach (GameObject prefab in boxPrefabs)
        {
            if (selectedShape == BoxController.BoxShape.Opened && prefab.name.Contains("Opened"))
            {
                selectedPrefab = prefab;
                break;
            }
            else if (selectedShape == BoxController.BoxShape.Unfolded && prefab.name.Contains("Unfolded"))
            {
                selectedPrefab = prefab;
                break;
            }
            else if (selectedShape == BoxController.BoxShape.Closed && !prefab.name.Contains("Opened") && !prefab.name.Contains("Unfolded"))
            {
                selectedPrefab = prefab;
                break;
            }
        }

        // Kutuyu oluştur
        GameObject spawnedBox = Instantiate(selectedPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);
        spawnedBox.name = "Cardboard Box_" + System.Guid.NewGuid().ToString().Substring(0, 5);

        BoxController boxController = spawnedBox.GetComponent<BoxController>();
        if (boxController == null)
        {
            boxController = spawnedBox.AddComponent<BoxController>();
        }

        boxController.Shape = selectedShape;

        // 7. Gün ve Kapalı kutu ise %25 şansla Sürpriz Kutu (Mystery Box) yap
        if (DayManager.Instance != null && DayManager.Instance.CurrentDay == 7 && selectedShape == BoxController.BoxShape.Closed)
        {
            if (Random.value <= 0.25f)
            {
                boxController.isMysteryBox = true;
                Renderer[] renderers = spawnedBox.GetComponentsInChildren<Renderer>();
                foreach (Renderer ren in renderers)
                {
                    if (ren is LineRenderer) continue;
                    ren.material.color = Color.magenta;
                }
                spawnedBox.name = "Cardboard Box (Mystery)_" + System.Guid.NewGuid().ToString().Substring(0, 5);
            }
        }

        // Kutuyu o günün kurallarına göre kur
        boxController.InitializeBox(config, targetPallet);

        // GameManager'a yeni kutunun üretildiğini bildir
        GameManager.Instance.RegisterBoxSpawn();
    }
}
