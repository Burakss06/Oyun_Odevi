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
    public bool allowDamagedDefect;
    public bool allowWrongColorDefect;
    public bool allowSizeAnomalyDefect;
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
            // 1. Gün: Sadece ezik/hasarlı kutuları ayıkla
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 1,
                totalBoxesToSpawn = 12,
                defectSpawnChance = 0.35f,
                allowedErrors = 3,
                dayDuration = 90f,
                allowDamagedDefect = true,
                allowWrongColorDefect = false,
                allowSizeAnomalyDefect = false
            });

            // 2. Gün: Hem ezik hem yanlış renkli kutuları ayıkla
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 2,
                totalBoxesToSpawn = 15,
                defectSpawnChance = 0.4f,
                allowedErrors = 2,
                dayDuration = 120f,
                allowDamagedDefect = true,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = false
            });

            // 3. Gün: Ezik, yanlış renk ve boyut hatası içeren kutuları ayıkla
            dayConfigs.Add(new DayConfig
            {
                dayNumber = 3,
                totalBoxesToSpawn = 20,
                defectSpawnChance = 0.45f,
                allowedErrors = 2,
                dayDuration = 150f,
                allowDamagedDefect = true,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = true
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
                allowDamagedDefect = true,
                allowWrongColorDefect = true,
                allowSizeAnomalyDefect = true
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
