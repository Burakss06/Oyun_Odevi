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

            // Bir sonraki kutu üretimi için bekle
            yield return new WaitForSeconds(spawnInterval);
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

        // Rastgele bir prefab seç
        int randomIndex = Random.Range(0, boxPrefabs.Count);
        GameObject selectedPrefab = boxPrefabs[randomIndex];

        // Kutuyu oluştur
        GameObject spawnedBox = Instantiate(selectedPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);
        
        // Objeye etkileşim sağlanabilmesi için isminin içinde "Cardboard Box" geçmesi gerekiyor. 
        // PlayerInteraction raycast'i ismi kontrol ettiği için adını uygun formata çeviriyoruz.
        spawnedBox.name = "Cardboard Box_" + System.Guid.NewGuid().ToString().Substring(0, 5);

        // BoxController bileşenini al veya ekle
        BoxController boxController = spawnedBox.GetComponent<BoxController>();
        if (boxController == null)
        {
            boxController = spawnedBox.AddComponent<BoxController>();
        }

        // Kutuyu o günün kurallarına göre kur
        boxController.InitializeBox(config);

        // GameManager'a yeni kutunun üretildiğini bildir
        GameManager.Instance.RegisterBoxSpawn();
    }
}
