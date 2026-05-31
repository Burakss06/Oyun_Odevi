using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, DayBriefing, Playing, DayReport, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("UI Panelleri")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject briefingPanel;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("HUD Elemanları")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Briefing (Bilgilendirme) Elemanları")]
    [SerializeField] private TextMeshProUGUI briefingTitleText;
    [SerializeField] private TextMeshProUGUI briefingContentText;
    [SerializeField] private Button startDayButton;

    [Header("Report (Gün Sonu Raporu) Elemanları")]
    [SerializeField] private TextMeshProUGUI reportTitleText;
    [SerializeField] private TextMeshProUGUI reportStatsText;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button retryDayButton;

    [Header("Oyun Sonu Elemanları")]
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button restartGameButton;

    [Header("İstatistikler")]
    public int Score { get; private set; }
    public int Errors { get; private set; }
    public int TotalSpawnedBoxes { get; private set; }
    public int TotalProcessedBoxes { get; private set; }

    public System.Collections.Generic.Dictionary<BoxController.BoxShape, PalletTrigger.PalletType> DailyRules { get; private set; }

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

    public void RollDailyRules()
    {
        DailyRules = new System.Collections.Generic.Dictionary<BoxController.BoxShape, PalletTrigger.PalletType>();
        
        if (DayManager.Instance == null) return;

        int currentDay = DayManager.Instance.CurrentDay;
        
        // 1. Gün kuralları sabit: Kapalı -> Kabul, Açık -> Ret
        if (currentDay == 1)
        {
            DailyRules[BoxController.BoxShape.Closed] = PalletTrigger.PalletType.Kabul;
            DailyRules[BoxController.BoxShape.Opened] = PalletTrigger.PalletType.Ret;
            return;
        }

        // 2. Gün ve sonrasında kutu kuralları rastgele belirlenecek (zar atma)
        // Kural: En az 1 tanesi Kabul, en az 1 tanesi Ret olacak.
        bool validRoll = false;
        while (!validRoll)
        {
            int kabulCount = 0;
            int retCount = 0;

            // Closed
            var closedPallet = (Random.value > 0.5f) ? PalletTrigger.PalletType.Kabul : PalletTrigger.PalletType.Ret;
            if (closedPallet == PalletTrigger.PalletType.Kabul) kabulCount++; else retCount++;

            // Opened
            var openedPallet = (Random.value > 0.5f) ? PalletTrigger.PalletType.Kabul : PalletTrigger.PalletType.Ret;
            if (openedPallet == PalletTrigger.PalletType.Kabul) kabulCount++; else retCount++;

            // Unfolded
            var unfoldedPallet = (Random.value > 0.5f) ? PalletTrigger.PalletType.Kabul : PalletTrigger.PalletType.Ret;
            if (unfoldedPallet == PalletTrigger.PalletType.Kabul) kabulCount++; else retCount++;

            if (kabulCount > 0 && retCount > 0)
            {
                DailyRules[BoxController.BoxShape.Closed] = closedPallet;
                DailyRules[BoxController.BoxShape.Opened] = openedPallet;
                DailyRules[BoxController.BoxShape.Unfolded] = unfoldedPallet;
                validRoll = true;
            }
        }
    }

    private void Start()
    {
        // Buton dinleyicilerini tanımla
        if (startDayButton != null) startDayButton.onClick.AddListener(StartActiveDay);
        if (nextDayButton != null) nextDayButton.onClick.AddListener(ProceedToNextDay);
        if (retryDayButton != null) retryDayButton.onClick.AddListener(RestartCurrentDay);
        if (restartGameButton != null) restartGameButton.onClick.AddListener(ResetWholeGame);

        // Oyunu başlat
        InitializeGame();
    }

    private void InitializeGame()
    {
        Score = 0;
        Errors = 0;
        TotalSpawnedBoxes = 0;
        TotalProcessedBoxes = 0;

        // DayManager'ı sıfırla
        if (DayManager.Instance != null)
        {
            DayManager.Instance.ResetProgress();
        }

        ShowBriefing();
    }

    public void ShowBriefing()
    {
        CurrentState = GameState.DayBriefing;
        Time.timeScale = 0f; // Zamanı durdur

        hudPanel.SetActive(false);
        briefingPanel.SetActive(true);
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // Cursor kilidini aç
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (DayManager.Instance != null)
        {
            // O günün kurallarını zar atarak belirle!
            RollDailyRules();

            DayConfig config = DayManager.Instance.GetCurrentDayConfig();
            briefingTitleText.text = $"{config.dayNumber}. GÜN";
            
            string rulesText = "Bugünkü Kurallar:\n";
            if (config.dayNumber == 1)
            {
                // Sabit kurallar
                rulesText += "- Kapalı kutuları KABUL paletine yerleştir.\n";
                rulesText += "- Açık (kanatlı) kutuları RET paletine yerleştir.\n";
            }
            else
            {
                // Rastgele seçilen kurallara göre madde madde yazdır
                foreach (var rule in DailyRules)
                {
                    string shapeName = "";
                    if (rule.Key == BoxController.BoxShape.Closed) shapeName = "Kapalı";
                    else if (rule.Key == BoxController.BoxShape.Opened) shapeName = "Açık (kanatlı)";
                    else if (rule.Key == BoxController.BoxShape.Unfolded) shapeName = "Düz (unfolded) karton";

                    string palletName = (rule.Value == PalletTrigger.PalletType.Kabul) ? "KABUL" : "RET";
                    rulesText += $"- {shapeName} kutuları {palletName} paletine yerleştir.\n";
                }
            }

            // Ekstra fiziksel hataları ve sürpriz kutuyu ekle
            if (config.allowWrongColorDefect)
            {
                rulesText += "- Kırmızı boyalı hatalı kutuları RET paletine yerleştir.\n";
            }
            if (config.allowSizeAnomalyDefect)
            {
                rulesText += "- Boyut hatası (çok küçük/büyük) olan kutuları RET paletine yerleştir.\n";
            }
            if (config.dayNumber == 5)
            {
                rulesText += "- SÜRPRİZ KUTU UYARISI: Mor renkli Sürpriz Kutular gelebilir! Nereye koyarsan koy %50 şansla doğru veya yanlış sayılacaktır.\n";
            }

            briefingContentText.text = $"Hedef: Toplam {config.totalBoxesToSpawn} kutunun kontrolünü yap.\n" +
                                       $"Hata Limiti: Maksimum {config.allowedErrors} hata yapma hakkın var.\n" +
                                       $"Süre: {config.dayDuration} saniye.\n\n" +
                                       rulesText;
        }
    }

    private void StartActiveDay()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f; // Zamanı başlat

        hudPanel.SetActive(true);
        briefingPanel.SetActive(false);
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // FPS kontrolü için imleci gizle ve kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Sahnedeki tüm eski kutuları (başlangıçtaki veya önceki günden kalanlar) temizle
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allGameObjects)
        {
            if (obj != null && obj.name.StartsWith("Cardboard Box"))
            {
                Destroy(obj);
            }
        }

        // Oyuncuyu başlangıç konumuna sıfırla
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.ResetToStartPosition();
        }

        // İstatistikleri temizle
        Score = 0;
        Errors = 0;
        TotalSpawnedBoxes = 0;
        TotalProcessedBoxes = 0;

        UpdateHUD();

        if (DayManager.Instance != null)
        {
            DayManager.Instance.StartDayTimer();
        }

        if (BoxSpawner.Instance != null)
        {
            BoxSpawner.Instance.StartSpawning();
        }
    }

    public void RegisterBoxSpawn()
    {
        TotalSpawnedBoxes++;
    }

    public void AddCorrectChoice()
    {
        Score++;
        TotalProcessedBoxes++;
        UpdateHUD();
        CheckDayCompletion();
    }

    public void AddIncorrectChoice()
    {
        Errors++;
        TotalProcessedBoxes++;
        UpdateHUD();

        DayConfig config = DayManager.Instance.GetCurrentDayConfig();
        if (Errors > config.allowedErrors)
        {
            TriggerGameOver("Hata limitini aştın!");
        }
        else
        {
            CheckDayCompletion();
        }
    }

    public void BoxMissed()
    {
        TotalProcessedBoxes++;
        CheckDayCompletion();
    }

    private void UpdateHUD()
    {
        if (DayManager.Instance != null)
        {
            dayText.text = $"Gün: {DayManager.Instance.CurrentDay}";
        }
        scoreText.text = $"Doğru: {Score}";
        
        if (DayManager.Instance != null)
        {
            DayConfig config = DayManager.Instance.GetCurrentDayConfig();
            errorText.text = $"Hata: {Errors}/{config.allowedErrors}";
        }
        else
        {
            errorText.text = $"Hata: {Errors}";
        }
    }

    public void UpdateTimerDisplay(float timeLeft)
    {
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);
        timerText.text = string.Format("Süre: {0:00}:{1:00}", minutes, seconds);
    }

    public void CheckDayCompletion()
    {
        if (DayManager.Instance == null) return;

        DayConfig config = DayManager.Instance.GetCurrentDayConfig();
        // Eğer gün içindeki tüm kutular üretildi ve hepsi işlendiyse gün biter
        if (TotalProcessedBoxes >= config.totalBoxesToSpawn)
        {
            EndDay(false);
        }
    }

    public void EndDay(bool wasTimeUp)
    {
        CurrentState = GameState.DayReport;
        Time.timeScale = 0f; // Oyunu durdur

        hudPanel.SetActive(false);
        briefingPanel.SetActive(false);
        reportPanel.SetActive(true);
        gameOverPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (BoxSpawner.Instance != null)
        {
            BoxSpawner.Instance.StopSpawning();
        }

        DayConfig config = DayManager.Instance.GetCurrentDayConfig();
        
        // Başarı koşulu: Süre bitmemiş olmalı (wasTimeUp == false) VE hatalar izin verilen sınırda olmalı
        bool isSuccess = !wasTimeUp && (Errors <= config.allowedErrors);
        
        if (isSuccess && config.dayNumber == 5)
        {
            TriggerGameWin();
            return;
        }
        
        reportTitleText.text = isSuccess ? "GÜN TAMAMLANDI" : "GÜN BAŞARISIZ";
        reportTitleText.color = isSuccess ? Color.green : Color.red;

        reportStatsText.text = $"Toplam Üretilen Kutu: {TotalSpawnedBoxes}\n" +
                               $"Kontrol Edilen: {TotalProcessedBoxes}\n" +
                               $"Doğru Ayrıştırma: {Score}\n" +
                               $"Yapılan Hata: {Errors}/{config.allowedErrors}\n\n" +
                               (isSuccess ? "Tebrikler, sonraki güne geçmeye hak kazandın!" : (wasTimeUp ? "Zaman sınırına ulaştın ve günü yetiştiremedin." : "Hata sınırını aşmıştın veya hedeflere ulaşamadın."));

        nextDayButton.gameObject.SetActive(isSuccess);
        retryDayButton.gameObject.SetActive(!isSuccess);
    }

    public void TriggerGameWin()
    {
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;

        hudPanel.SetActive(false);
        briefingPanel.SetActive(false);
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (BoxSpawner.Instance != null)
        {
            BoxSpawner.Instance.StopSpawning();
        }

        // Metin rengini YEŞİL yap
        gameOverText.color = Color.green;
        gameOverText.text = $"TEBRİKLER! OYUNU KAZANDINIZ\n\n5 günlük fabrika kalite kontrol vardiyasını başarıyla tamamladın ve usta bir fabrika işçisi olduğunu kanıtladın!\n\nToplam Doğru: {Score}\nYaptığın Toplam Hata: {Errors}\n\nYeniden başlamak için aşağıdaki butonu kullanabilirsin.";
    }

    public void TriggerGameOver(string reason)
    {
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;

        hudPanel.SetActive(false);
        briefingPanel.SetActive(false);
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (BoxSpawner.Instance != null)
        {
            BoxSpawner.Instance.StopSpawning();
        }

        // Metin rengini KIRMIZI yap
        gameOverText.color = Color.red;
        gameOverText.text = $"OYUN BİTTİ\n\nSebep: {reason}\n\nToplam Başarı: {DayManager.Instance.CurrentDay}. güne kadar gelebildin.";
    }

    private void ProceedToNextDay()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.IncrementDay();
            ShowBriefing();
        }
    }

    private void RestartCurrentDay()
    {
        ShowBriefing();
    }

    private void ResetWholeGame()
    {
        InitializeGame();
    }
}
