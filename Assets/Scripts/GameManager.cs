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

    [Header("Ses Efektleri & Müzik")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip winMusic;
    [SerializeField] private AudioClip loseMusic;
    [SerializeField] private AudioClip wrongBuzzerSound;

    [Header("İstatistikler")]
    public int Score { get; private set; }
    public int Errors { get; private set; }
    public int TotalSpawnedBoxes { get; private set; }
    public int TotalProcessedBoxes { get; private set; }

    public System.Collections.Generic.Dictionary<BoxController.BoxShape, PalletTrigger.PalletType> DailyRules { get; private set; }

    private Button muteButton;
    private bool isMuted = false;
    private string originalStartButtonText = "Günü Başlat";
    private Color originalStartButtonColor = Color.white;
    private Vector2 originalStartButtonPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        if (musicSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length > 0)
            {
                musicSource = sources[0];
            }
            else
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (sfxSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                sfxSource = sources[1];
            }
            else
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }

        musicSource.mute = isMuted;
        if (sfxSource != null)
        {
            sfxSource.mute = false; // Efekt sesi her zaman açık kalır!
        }

        if (backgroundMusic == null)
        {
            backgroundMusic = Resources.Load<AudioClip>("Audio/background");
        }
        if (winMusic == null)
        {
            winMusic = Resources.Load<AudioClip>("Audio/win");
        }
        if (loseMusic == null)
        {
            loseMusic = Resources.Load<AudioClip>("Audio/lose");
        }
        if (wrongBuzzerSound == null)
        {
            wrongBuzzerSound = Resources.Load<AudioClip>("Audio/wrong_buzzer");
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
        // Orijinal buton metnini, rengini ve pozisyonunu al
        if (startDayButton != null)
        {
            var tmp = startDayButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) originalStartButtonText = tmp.text;
            else
            {
                var txt = startDayButton.GetComponentInChildren<Text>();
                if (txt != null) originalStartButtonText = txt.text;
            }

            var startImg = startDayButton.GetComponent<Image>();
            if (startImg != null)
            {
                originalStartButtonColor = startImg.color;
            }

            RectTransform startRect = startDayButton.GetComponent<RectTransform>();
            if (startRect != null)
            {
                originalStartButtonPosition = startRect.anchoredPosition;
            }

            // Start butonu tıklama dinleyicisini bağla
            startDayButton.onClick.AddListener(OnStartDayButtonClicked);

            // Mute butonunu oluştur (StartDayButton'ı kopyalayarak)
            muteButton = Instantiate(startDayButton, startDayButton.transform.parent);
            muteButton.name = "MuteMusicButton";
            
            // Klon butonun dinleyicilerini sıfırla ve yeni dinleyici ekle
            muteButton.onClick.RemoveAllListeners();
            muteButton.onClick.AddListener(ToggleMute);

            UpdateMuteButtonText();
        }

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

        ShowMenu();
    }

    public void ShowBriefing()
    {
        StopMusic();

        CurrentState = GameState.DayBriefing;
        Time.timeScale = 0f; // Zamanı durdur

        hudPanel.SetActive(false);
        briefingPanel.SetActive(true);
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // Orijinal buton metnine, rengine ve pozisyonuna geri dön
        UpdateButtonText(startDayButton, originalStartButtonText);
        var startImg = startDayButton.GetComponent<Image>();
        if (startImg != null)
        {
            startImg.color = originalStartButtonColor;
        }
        if (startDayButton.transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
        {
            RectTransform startRect = startDayButton.GetComponent<RectTransform>();
            if (startRect != null)
            {
                startRect.anchoredPosition = originalStartButtonPosition;
            }
        }

        // Mute butonunu diğer günlerin brifing ekranında gizleyelim
        if (muteButton != null)
        {
            muteButton.gameObject.SetActive(false);
        }

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

        PlayBackgroundMusic();

        // Oynanış esnasında mute butonunu gizle
        if (muteButton != null)
        {
            muteButton.gameObject.SetActive(false);
        }

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
            PlayBuzzerSound();
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
        StopMusic();

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
        
        if (isSuccess)
        {
            if (config.dayNumber == 5)
            {
                TriggerGameWin();
                return;
            }
            else
            {
                PlayWinMusic();
            }
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
        StopMusic();
        PlayWinMusic();

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
        StopMusic();
        PlayBuzzerSound();

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

        // Lose müziğini gecikmeli başlat
        StartCoroutine(PlayLoseMusicDelayed(1.2f));
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

    private void PlayBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void PlayWinMusic()
    {
        if (musicSource != null && winMusic != null)
        {
            musicSource.clip = winMusic;
            musicSource.loop = false;
            musicSource.Play();
        }
    }

    private void PlayLoseMusic()
    {
        if (musicSource != null && loseMusic != null)
        {
            musicSource.clip = loseMusic;
            musicSource.loop = false;
            musicSource.Play();
        }
    }

    private void PlayBuzzerSound()
    {
        if (sfxSource != null && wrongBuzzerSound != null)
        {
            sfxSource.PlayOneShot(wrongBuzzerSound);
        }
    }

    private System.Collections.IEnumerator PlayLoseMusicDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (CurrentState == GameState.GameOver)
        {
            PlayLoseMusic();
        }
    }

    private void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void ShowMenu()
    {
        CurrentState = GameState.Menu;
        Time.timeScale = 0f; // Zamanı durdur

        hudPanel.SetActive(false);
        briefingPanel.SetActive(true);
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // İmleci göster
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Müzik çal
        PlayBackgroundMusic();

        // Oyuna Başla butonunu yeşil yap
        var startImg = startDayButton.GetComponent<Image>();
        if (startImg != null)
        {
            startImg.color = new Color(0.18f, 0.77f, 0.31f); // Güzel bir zümrüt yeşili
        }

        // Mute butonunu aktif et ve butonları ortalayarak yan yana hizala
        if (muteButton != null)
        {
            muteButton.gameObject.SetActive(true);
            var muteImg = muteButton.GetComponent<Image>();
            if (muteImg != null)
            {
                muteImg.color = new Color(0.5f, 0.5f, 0.5f); // Şık bir gri
            }
            if (startDayButton.transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
            {
                RectTransform startRect = startDayButton.GetComponent<RectTransform>();
                RectTransform muteRect = muteButton.GetComponent<RectTransform>();
                if (startRect != null && muteRect != null)
                {
                    float startWidth = startRect.rect.width;
                    float muteWidth = muteRect.rect.width;
                    float spacing = 20f; // 20 piksel boşluk

                    // İki butonu da orijinal merkeze göre dengeli şekilde kaydır (Oyuna Başla solda, Mute sağda)
                    startRect.anchoredPosition = originalStartButtonPosition - new Vector2((muteWidth + spacing) / 2f, 0f);
                    muteRect.anchoredPosition = originalStartButtonPosition + new Vector2((startWidth + spacing) / 2f, 0f);
                }
            }
            UpdateMuteButtonText();
        }

        // Metinleri ata
        briefingTitleText.text = "<color=#FFaa00>Denetim ve Kalite Kontrol Simülasyonuna Hoş Geldiniz</color>";
        
        briefingContentText.text = "Fabrika kalite kontrol vardiyanıza başlamak üzeresiniz.\n\n" +
                                   "<b>Göreviniz:</b> Banttan gelen kutuları günlük kurallara göre KABUL veya RET paletlerine yerleştirmektir.\n" +
                                   "Hata limitinizi aşmadan günü tamamlamalısınız.\n\n" +
                                   "<b>Başlamak için aşağıdaki butona tıklayın!</b>";

        UpdateButtonText(startDayButton, "OYUNA BAŞLA");
    }

    private void OnStartDayButtonClicked()
    {
        if (CurrentState == GameState.Menu)
        {
            ShowBriefing();
        }
        else if (CurrentState == GameState.DayBriefing)
        {
            StartActiveDay();
        }
    }

    private void ToggleMute()
    {
        isMuted = !isMuted;
        
        if (musicSource != null)
        {
            musicSource.mute = isMuted;
        }

        UpdateMuteButtonText();
    }

    private void UpdateMuteButtonText()
    {
        if (muteButton != null)
        {
            UpdateButtonText(muteButton, isMuted ? "Müzik Aç" : "Müziği Kapa");
        }
    }

    private void UpdateButtonText(Button button, string newText)
    {
        if (button == null) return;
        
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = newText;
            return;
        }

        var txt = button.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = newText;
        }
    }
}
