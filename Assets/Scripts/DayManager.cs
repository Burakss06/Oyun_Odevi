using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct DayConfig
{
    public int dayNumber;
    public int totalBoxesToSpawn;
    public float defectSpawnChance;
    public int allowedErrors;
    public float dayDuration; // saniye cinsinden

    [Header("Kusur Kuralları")]
    public bool allowBarcodeDefect;
    public bool allowWrongColorDefect;
    public bool allowSizeAnomalyDefect;
    public bool allowWeightDefect;
}

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Gün Konfigürasyonları")]
    [SerializeField] private List<DayConfig> dayConfigs = new List<DayConfig>();

    public int CurrentDay { get; private set; } = 1;
    
    private float timer;
    private bool isTimerRunning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefaultConfigs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDefaultConfigs()
    {
        // Eğer inspector'dan atanmamışsa varsayılan günleri oluştur
        if (dayConfigs.Count == 0)
        {
            // 1. Gün: Sadece Kapalı ve Açık kutular. Hasarlı ve diğer kusurlar kapalı.
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 1,
                totalBoxesToSpawn = 12,
                defectSpawnChance = 0.35f,
                allowedErrors = 3,
                dayDuration = 90f,
                allowBarcodeDefect = false,
                allowWrongColorDefect = false,
                allowSizeAnomalyDefect = false
            });

            // 2. Gün: Kapalı, Açık ve Düz kutular. Hasarlı ve diğer kusurlar kapalı.
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 2,
                totalBoxesToSpawn = 15,
                defectSpawnChance = 0.4f,
                allowedErrors = 2,
                dayDuration = 120f,
                allowBarcodeDefect = false,
                allowWrongColorDefect = false,
                allowSizeAnomalyDefect = false
            });

            // 3. Gün: Tartı Günü! (Ağır kutular) Renk, boyut ve hasar henüz yok.
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 3,
                totalBoxesToSpawn = 18,
                defectSpawnChance = 0.45f,
                allowedErrors = 2,
                dayDuration = 140f,
                allowBarcodeDefect = false,
                allowWrongColorDefect = false,
                allowSizeAnomalyDefect = false,
                allowWeightDefect = true
            });

            // 4. Gün: Renk Günü! (Yanlış renkli kutular) Ağırlık da devam ediyor.
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 4,
                totalBoxesToSpawn = 22,
                defectSpawnChance = 0.5f,
                allowedErrors = 2,
                dayDuration = 160f,
                allowBarcodeDefect = false,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = false,
                allowWeightDefect = true
            });

            // 5. Gün: Barkod Kontrol Günü! (Yanlış barkodlar)
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 5,
                totalBoxesToSpawn = 25,
                defectSpawnChance = 0.5f,
                allowedErrors = 2,
                dayDuration = 180f,
                allowBarcodeDefect = true,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = false,
                allowWeightDefect = true
            });

            // 6. Gün: Boyut Hatası Günü! (Çok küçük/büyük kutular)
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 6,
                totalBoxesToSpawn = 25,
                defectSpawnChance = 0.5f,
                allowedErrors = 2,
                dayDuration = 200f,
                allowBarcodeDefect = true,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = true,
                allowWeightDefect = true
            });

            // 7. Gün: Sürpriz Kutu Günü! (Mor kutular) Tüm kurallar aktif.
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 7,
                totalBoxesToSpawn = 25,
                defectSpawnChance = 0.55f,
                allowedErrors = 2,
                dayDuration = 220f,
                allowBarcodeDefect = true,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = true,
                allowWeightDefect = true
            });
        }
    }

    public DayConfig GetCurrentDayConfig()
    {
        // Eğer tanımlı günlerden büyük bir güne gelinirse son gün konfigürasyonunu temel al ve zorlaştır
        if (CurrentDay > dayConfigs.Count)
        {
            DayConfig lastConfig = dayConfigs[dayConfigs.Count - 1];
            return new DayConfig
            {
                dayNumber = CurrentDay,
                totalBoxesToSpawn = lastConfig.totalBoxesToSpawn + (CurrentDay - lastConfig.dayNumber) * 3,
                defectSpawnChance = Mathf.Min(0.6f, lastConfig.defectSpawnChance + 0.02f * (CurrentDay - lastConfig.dayNumber)),
                allowedErrors = Mathf.Max(1, lastConfig.allowedErrors),
                dayDuration = lastConfig.dayDuration + 10f,
                allowBarcodeDefect = true,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = true,
                allowWeightDefect = true
            };
        }

        return dayConfigs[CurrentDay - 1];
    }

    public void StartDayTimer()
    {
        DayConfig config = GetCurrentDayConfig();
        timer = config.dayDuration;
        isTimerRunning = true;
    }

    public void StopDayTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>
    /// Güçlendirici ile süre eklemek için kullanılır (Zaman Bükücü).
    /// </summary>
    public void AddTime(float extraTime)
    {
        if (isTimerRunning)
        {
            timer += extraTime;
            Debug.Log($"[DayManager] {extraTime} saniye eklendi. Kalan süre: {timer}");
        }
    }

    public float RemainingTime => Mathf.Max(0f, timer);

    private void Update()
    {
        if (isTimerRunning && GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            timer -= Time.deltaTime;
            GameManager.Instance.UpdateTimerDisplay(Mathf.Max(0f, timer));

            if (timer <= 0)
            {
                timer = 0;
                isTimerRunning = false;
                // Süre bittiğinde gün sonu değerlendirmesini tetikle
                GameManager.Instance.EndDay(true);
            }
        }
    }

    public void IncrementDay()
    {
        CurrentDay++;
    }

    public void ResetProgress()
    {
        CurrentDay = 1;
        isTimerRunning = false;
        timer = 0f;
    }
}
