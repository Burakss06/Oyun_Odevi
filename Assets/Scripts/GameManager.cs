using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, DayBriefing, Playing, DayReport, GameOver, Paused }
    public GameState CurrentState { get; private set; }

    [Header("UI Panelleri")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject briefingPanel;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Pause Menu Elements")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseMuteButton;
    [SerializeField] private TextMeshProUGUI sensitivityValueText;

    [Header("HUD Elemanları")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI errorText;
    private TextMeshProUGUI remainingBoxesText;

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
    [SerializeField] private AudioClip correctChoiceSound;

    [Header("İstatistikler")]
    public int Score { get; private set; }
    public int Errors { get; private set; }
    public int TotalSpawnedBoxes { get; private set; }
    public int TotalProcessedBoxes { get; private set; }
    public int TotalSalary { get; private set; }

    public System.Collections.Generic.Dictionary<BoxController.BoxShape, PalletTrigger.PalletType> DailyRules { get; private set; }
    public string ValidBarcodeNumber { get; private set; } = "";
    public PalletTrigger.PalletType WeightDefectRule { get; private set; } = PalletTrigger.PalletType.Ret;
    public PalletTrigger.PalletType ColorDefectRule { get; private set; } = PalletTrigger.PalletType.Ret;

    private Button muteButton;
    private Button howToPlayButton;
    private Button backButton;
    private Button quitGameButton;
    private bool isMuted = false;
    private string originalStartButtonText = "Günü Başlat";
    private Color originalStartButtonColor = Color.white;
    private Vector2 originalStartButtonPosition;
    private Vector2 originalStartButtonSize;
    private Vector2 originalContentTextPosition;
    private bool openedHowToPlayFromPause = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeAudio();
            
            // Arayüz temasını yükle
            if (GetComponent<UIThemeEnhancer>() == null)
            {
                gameObject.AddComponent<UIThemeEnhancer>();
            }
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
            sfxSource.mute = false; // Efektler sessize alınmaz
        }

        StartCoroutine(LoadAudioClipsAsync());
    }

    private System.Collections.IEnumerator LoadAudioClipsAsync()
    {
        if (backgroundMusic == null)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>("Audio/background");
            yield return request;
            backgroundMusic = request.asset as AudioClip;
            if (CurrentState == GameState.Menu && musicSource != null && backgroundMusic != null)
            {
                PlayBackgroundMusic();
            }
        }
        if (winMusic == null)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>("Audio/win");
            yield return request;
            winMusic = request.asset as AudioClip;
        }
        if (loseMusic == null)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>("Audio/lose");
            yield return request;
            loseMusic = request.asset as AudioClip;
        }
        if (wrongBuzzerSound == null)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>("Audio/wrong_buzzer");
            yield return request;
            wrongBuzzerSound = request.asset as AudioClip;
        }
        if (correctChoiceSound == null)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>("Audio/correct_ding");
            yield return request;
            correctChoiceSound = request.asset as AudioClip;
        }
    }

    public void RollDailyRules()
    {
        DailyRules = new System.Collections.Generic.Dictionary<BoxController.BoxShape, PalletTrigger.PalletType>();
        
        if (DayManager.Instance == null) return;

        int currentDay = DayManager.Instance.CurrentDay;
        
        // 1. Gün kuralları sabit
        if (currentDay == 1)
        {
            DailyRules[BoxController.BoxShape.Closed] = PalletTrigger.PalletType.Kabul;
            DailyRules[BoxController.BoxShape.Opened] = PalletTrigger.PalletType.Ret;
            WeightDefectRule = PalletTrigger.PalletType.Ret;
            ColorDefectRule = PalletTrigger.PalletType.Ret;
            return;
        }

        // 2. Gün ve sonrasında kutu kuralları rastgele belirlenir (en az 1 kabul, 1 ret)
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

        // Renk ve Ağırlık kurallarını rastgele belirle (Kabul veya Ret)
        WeightDefectRule = (Random.value > 0.5f) ? PalletTrigger.PalletType.Kabul : PalletTrigger.PalletType.Ret;
        ColorDefectRule = (Random.value > 0.5f) ? PalletTrigger.PalletType.Kabul : PalletTrigger.PalletType.Ret;

        // Barkod günü aktifse yeni barkod numarası belirle
        DayConfig cfg = DayManager.Instance.GetCurrentDayConfig();
        if (cfg.allowBarcodeDefect)
        {
            ValidBarcodeNumber = Random.Range(1000000, 9999999).ToString();
        }
    }

    private void Start()
    {
        // Orijinal buton metnini, rengini, pozisyonunu ve boyutunu al
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
                originalStartButtonSize = startRect.sizeDelta;
            }

            if (briefingContentText != null)
            {
                RectTransform textRect = briefingContentText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    originalContentTextPosition = textRect.anchoredPosition;
                }
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

            // Nasıl Oynanır butonunu oluştur
            howToPlayButton = Instantiate(startDayButton, startDayButton.transform.parent);
            howToPlayButton.name = "HowToPlayButton";
            howToPlayButton.onClick.RemoveAllListeners();
            howToPlayButton.onClick.AddListener(OnMenuHowToPlayClicked);
            UpdateButtonText(howToPlayButton, "NASIL OYNANIR");

            // Geri butonunu oluştur
            backButton = Instantiate(startDayButton, startDayButton.transform.parent);
            backButton.name = "BackButton";
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
            UpdateButtonText(backButton, "GERİ");
            backButton.gameObject.SetActive(false);

            // Oyundan Çık butonunu oluştur
            quitGameButton = Instantiate(startDayButton, startDayButton.transform.parent);
            quitGameButton.name = "QuitGameButton";
            quitGameButton.onClick.RemoveAllListeners();
            quitGameButton.onClick.AddListener(QuitGame);
            UpdateButtonText(quitGameButton, "OYUNDAN ÇIK");
            quitGameButton.gameObject.SetActive(false);
        }

        // Pause Menüsünü kur
        if (pausePanel != null)
        {
            var pauseMenuUI = pausePanel.GetComponent<PauseMenuUI>();
            if (pauseMenuUI == null) pauseMenuUI = pausePanel.AddComponent<PauseMenuUI>();
            pauseMenuUI.BuildUI();
            
            pauseResumeButton = pauseMenuUI.resumeButton;
            pauseMuteButton = pauseMenuUI.muteButton;
            sensitivitySlider = pauseMenuUI.sensitivitySlider;
            sensitivityValueText = pauseMenuUI.sensitivityValueText;

            if (pauseMenuUI.quitButton != null)
            {
                pauseMenuUI.quitButton.onClick.AddListener(QuitGame);
            }
            if (pauseMenuUI.howToPlayButton != null)
            {
                pauseMenuUI.howToPlayButton.onClick.AddListener(OnPauseHowToPlayClicked);
            }
        }

        // Kalan kutu metnini oluştur
        if (scoreText != null)
        {
            GameObject remObj = Instantiate(scoreText.gameObject, scoreText.transform.parent);
            remObj.name = "RemainingBoxesText";
            remainingBoxesText = remObj.GetComponent<TextMeshProUGUI>();
            remainingBoxesText.color = Color.white; // Tam beyaz
            
            // ScoreText'in soluna konumlandır (X ekseninde -65 birim)
            RectTransform rect = remObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(scoreText.rectTransform.anchoredPosition.x - 65f, scoreText.rectTransform.anchoredPosition.y);
            remainingBoxesText.alignment = TextAlignmentOptions.Center;
            remainingBoxesText.text = "Kalan Kutu: 0 / 0";
        }



        if (nextDayButton != null) nextDayButton.onClick.AddListener(ProceedToNextDay);
        if (retryDayButton != null) retryDayButton.onClick.AddListener(RestartCurrentDay);
        if (restartGameButton != null) restartGameButton.onClick.AddListener(ResetWholeGame);
        if (pauseResumeButton != null) pauseResumeButton.onClick.AddListener(ResumeGame);
        if (pauseMuteButton != null) pauseMuteButton.onClick.AddListener(ToggleMute);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        // Oyunu başlat
        InitializeGame();
    }

    private void InitializeGame()
    {
        Score = 0;
        Errors = 0;
        TotalSpawnedBoxes = 0;
        TotalProcessedBoxes = 0;
        TotalSalary = 0;

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
        StartCoroutine(UIAnimator.FadeInAndScale(briefingPanel));
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Orijinal buton metnine, rengine ve pozisyonuna geri dön
        UpdateButtonText(startDayButton, originalStartButtonText);
        var startImg = startDayButton.GetComponent<Image>();
        if (startImg != null)
        {
            startImg.color = originalStartButtonColor;
        }

        RectTransform startRectTemp = startDayButton.GetComponent<RectTransform>();
        if (startRectTemp != null)
        {
            startRectTemp.sizeDelta = originalStartButtonSize;
            startRectTemp.anchoredPosition = originalStartButtonPosition;
        }

        RectTransform textRect = briefingContentText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.anchoredPosition = originalContentTextPosition;
        }

        // Diğer butonları brifingde gizle
        if (muteButton != null) muteButton.gameObject.SetActive(false);
        if (howToPlayButton != null) howToPlayButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (quitGameButton != null) quitGameButton.gameObject.SetActive(false);
        startDayButton.gameObject.SetActive(true);

        // Cursor kilidini aç
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (DayManager.Instance != null)
        {
            // O günün kurallarını zar atarak belirle!
            RollDailyRules();

            DayConfig config = DayManager.Instance.GetCurrentDayConfig();
            briefingTitleText.text = $"<color=#00BCD4>{config.dayNumber}. GÜN</color>";

            // 7. günde butonun yazılarla çakışmaması için Y pozisyonunu aşağı kaydırıyoruz
            if (startDayButton.transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
            {
                RectTransform startRect = startDayButton.GetComponent<RectTransform>();
                if (startRect != null)
                {
                    int activeRulesCount = 0;
                    if (config.allowBarcodeDefect) activeRulesCount++;
                    if (config.allowWrongColorDefect) activeRulesCount++;
                    if (config.allowSizeAnomalyDefect) activeRulesCount++;
                    if (config.allowWeightDefect) activeRulesCount++;
                    if (config.dayNumber == 7) activeRulesCount++;

                    if (activeRulesCount >= 3)
                    {
                        // Sadece Y ekseninde aşağı çek
                        startRect.anchoredPosition = new Vector2(originalStartButtonPosition.x, originalStartButtonPosition.y - 45f);
                    }
                    else
                    {
                        startRect.anchoredPosition = originalStartButtonPosition;
                    }
                }
            }
            
            string rulesText = "";
            if (config.dayNumber == 1)
            {
                rulesText += " <color=#555555>■</color> Kapalı (bantlı) kutular <color=#888888>→</color> <color=#4CAF50><b>KABUL</b></color>\n";
                rulesText += " <color=#555555>■</color> Açık (kanatlı) kutular <color=#888888>→</color> <color=#F44336><b>RET</b></color>\n";
            }
            else
            {
                // Rastgele seçilen kurallara göre madde madde yazdır
                foreach (var rule in DailyRules)
                {
                    string shapeName = "";
                    if (rule.Key == BoxController.BoxShape.Closed) shapeName = "Kapalı kutular";
                    else if (rule.Key == BoxController.BoxShape.Opened) shapeName = "Açık (kanatlı) kutular";
                    else if (rule.Key == BoxController.BoxShape.Unfolded) shapeName = "Uzun kutular";

                    string palletColor = (rule.Value == PalletTrigger.PalletType.Kabul) ? "#4CAF50" : "#F44336";
                    string palletName = (rule.Value == PalletTrigger.PalletType.Kabul) ? "KABUL" : "RET";
                    rulesText += $" <color=#555555>■</color> {shapeName} <color=#888888>→</color> <color={palletColor}><b>{palletName}</b></color>\n";
                }
            }

            // Ekstra fiziksel hataları ve sürpriz kutuyu ekle
            if (config.allowBarcodeDefect)
            {
                rulesText += $"<color=#FFC107><b>[BARKOD KONTROLÜ]</b></color>\n <color=#555555>■</color> Sadece <b>{ValidBarcodeNumber}</b> numaralılar <color=#888888>→</color> <color=#4CAF50><b>KABUL</b></color>\n";
            }
            if (config.allowWrongColorDefect)
            {
                string targetName = (ColorDefectRule == PalletTrigger.PalletType.Kabul) ? "KABUL" : "RET";
                string targetColor = (ColorDefectRule == PalletTrigger.PalletType.Kabul) ? "#4CAF50" : "#F44336";
                rulesText += $"<color=#4CAF50><b>[RENK KONTROLÜ]</b></color>\n <color=#555555>■</color> Yeşil boyalı hatalı kutular <color=#888888>→</color> <color={targetColor}><b>{targetName}</b></color>\n";
            }
            if (config.allowSizeAnomalyDefect)
            {
                rulesText += $"<color=#2196F3><b>[BOYUT KONTROLÜ]</b></color>\n <color=#555555>■</color> Küçük/büyük kutular <color=#888888>→</color> <color=#F44336><b>RET</b></color>\n";
            }
            if (config.allowWeightDefect)
            {
                string targetName = (WeightDefectRule == PalletTrigger.PalletType.Kabul) ? "KABUL" : "RET";
                string targetColor = (WeightDefectRule == PalletTrigger.PalletType.Kabul) ? "#4CAF50" : "#F44336";
                rulesText += $"<color=#9C27B0><b>[AĞIRLIK KONTROLÜ]</b></color>\n <color=#555555>■</color> 10.0 kg ve üzeri ağır kutular <color=#888888>→</color> <color={targetColor}><b>{targetName}</b></color>\n";
            }
            if (config.dayNumber == 7)
            {
                rulesText += $"<color=#E91E63><b>[SÜRPRİZ KUTU]</b></color>\n <color=#555555>■</color> Mor kutular eklendi. %50 şansla çalışır.\n";
            }

            briefingContentText.text = $"<color=#AAAAAA><size=80%>────────── GÖREV ÖZETİ ──────────</size></color>\n" +
                                       $" Hedef: Toplam <b>{config.totalBoxesToSpawn}</b> kutu kontrol edilecek. Hata Limiti: <b>{config.allowedErrors}</b> hata.\n" +
                                       $" Süre: <b>{config.dayDuration}</b> saniye.\n" +
                                       $"<color=#AAAAAA><size=80%>─────────────────────────────────</size></color>\n" +
                                       $"<size=100%><b>GÜNLÜK KURALLAR:</b></size>\n" +
                                       $"<line-height=85%><size=75%>{rulesText}</size></line-height>";
        }
    }

    private void StartActiveDay()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f; // Zamanı başlat

        PlayBackgroundMusic();

        // Oynanış esnasında mute ve quit butonunu gizle
        if (muteButton != null)
        {
            muteButton.gameObject.SetActive(false);
        }
        if (quitGameButton != null)
        {
            quitGameButton.gameObject.SetActive(false);
        }

        hudPanel.SetActive(true);
        briefingPanel.SetActive(false);
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // İmleci gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Sahnedeki eski kutuları temizle
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allGameObjects)
        {
            if (obj != null && obj.name.StartsWith("Cardboard Box"))
            {
                Destroy(obj);
            }
        }

        // Sahnedeki eski güçlendiricileri temizle
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.DeactivateAllPowerUps();
        }

        // Paletlerin üzerindeki kutuları temizle
        PalletTrigger[] pallets = FindObjectsOfType<PalletTrigger>();
        foreach (PalletTrigger pallet in pallets)
        {
            pallet.ClearStackedBoxes();
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
        TotalSalary += 50;
        UpdateHUD();
        PlayCorrectSound();
        CheckDayCompletion();
    }

    public void AddIncorrectChoice()
    {
        Errors++;
        TotalProcessedBoxes++;
        TotalSalary -= 20;
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

    /// <summary>
    /// Güçlendirici ile yapılan hataları silmek için kullanılır (Müfettişin İzni).
    /// </summary>
    public void DecreaseErrorCount()
    {
        if (Errors > 0)
        {
            Errors--;
            UpdateHUD();
            Debug.Log($"[GameManager] Hata sayısı 1 düşürüldü. Mevcut Hata: {Errors}");
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
            dayText.text = $"Gün: {DayManager.Instance.CurrentDay}   |   Maaş: {TotalSalary} TL";
        }
        scoreText.text = $"Doğru: {Score}";
        
        if (DayManager.Instance != null)
        {
            DayConfig config = DayManager.Instance.GetCurrentDayConfig();
            errorText.text = $"Hata: {Errors}/{config.allowedErrors}";
            
            if (remainingBoxesText != null)
            {
                int remaining = Mathf.Max(0, config.totalBoxesToSpawn - TotalProcessedBoxes);
                remainingBoxesText.text = $"Kalan Kutu: {remaining} / {config.totalBoxesToSpawn}";
            }
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
        StartCoroutine(UIAnimator.FadeInAndScale(reportPanel));
        gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

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
            // Erken bitirme bonusu: Kalan her saniye için +5 TL
            if (DayManager.Instance != null && DayManager.Instance.RemainingTime > 0)
            {
                TotalSalary += Mathf.FloorToInt(DayManager.Instance.RemainingTime) * 5;
            }

            if (config.dayNumber == 7)
            {
                TriggerGameWin();
                return;
            }
            else
            {
                PlayWinMusic();
            }
        }
        else
        {
            PlayLoseMusic();
        }
        
        reportTitleText.text = isSuccess ? "<color=#4CAF50>GÜN TAMAMLANDI</color>" : "<color=#F44336>GÜN BAŞARISIZ</color>";
        reportTitleText.color = Color.white; // Renkleri TextMeshPro tagı ile hallediyoruz

        reportStatsText.text = $"<color=#AAAAAA><size=90%>────────── GÜN RAPORU ──────────</size></color>\n" +
                               $" Toplam Üretilen Kutu: <color=#FFFFFF><b>{TotalSpawnedBoxes}</b></color>\n" +
                               $" Kontrol Edilen: <color=#FFFFFF><b>{TotalProcessedBoxes}</b></color>\n" +
                               $" Doğru Ayrıştırma: <color=#4CAF50><b>{Score}</b></color>\n" +
                               $" Yapılan Hata: <color=#F44336><b>{Errors}/{config.allowedErrors}</b></color>\n" +
                               $" Güncel Maaş: <color=#FFD700><b>{TotalSalary} TL</b></color>\n" +
                               $"<color=#AAAAAA><size=90%>────────────────────────────────</size></color>\n\n" +
                               (isSuccess ? "<color=#FFC107>Tebrikler, sonraki güne geçmeye hak kazandın!</color>" : (wasTimeUp ? "<color=#F44336>Zaman sınırına ulaştın ve günü yetiştiremedin.</color>" : "<color=#F44336>Hata sınırını aşmıştın veya hedeflere ulaşamadın.</color>"));

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
        StartCoroutine(UIAnimator.FadeInAndScale(gameOverPanel));
        if (pausePanel != null) pausePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Image bgImage = gameOverPanel.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = new Color(0.05f, 0.35f, 0.15f, 0.95f); // Koyu yeşil tonu
        }

        if (BoxSpawner.Instance != null)
        {
            BoxSpawner.Instance.StopSpawning();
        }

        // Metinleri yeni tasarıma uyarla
        gameOverText.color = Color.white;
        gameOverText.text = $"<align=center><color=#4CAF50><size=130%>TEBRİKLER! OYUNU KAZANDINIZ</size></color>\n\n" +
                            "<color=#CCCCCC>7 günlük fabrika kalite kontrol vardiyasını başarıyla tamamladın ve usta bir fabrika işçisi olduğunu kanıtladın!</color>\n\n" +
                            $"<color=#AAAAAA><size=90%>─── PERFORMANS ÖZETİ ───</size></color>\n" +
                            $"Toplam Doğru: <color=#4CAF50><b>{Score}</b></color>\n" +
                            $"Yaptığın Toplam Hata: <color=#F44336><b>{Errors}</b></color>\n" +
                            $"Kazanılan Maaş: <color=#FFD700><b>{TotalSalary} TL</b></color>\n" +
                            $"<color=#AAAAAA><size=90%>─────────────────────</size></color></align>";
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
        StartCoroutine(UIAnimator.FadeInAndScale(gameOverPanel));
        if (pausePanel != null) pausePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Image bgImage = gameOverPanel.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = new Color(0.35f, 0.05f, 0.05f, 0.95f); // Koyu kırmızı tonu
        }

        if (BoxSpawner.Instance != null)
        {
            BoxSpawner.Instance.StopSpawning();
        }

        // Metin rengini BEYAZ yapıp zengin metin (Rich Text) ile renklendir
        gameOverText.color = Color.white;
        
        string salaryText = TotalSalary >= 0 
            ? $"Kovuldun! Tazminat Olarak Yatan Para: <b>{TotalSalary} TL</b>"
            : $"Kovuldun! Giderken biraz paran düştü <b>{TotalSalary} TL</b>";

        gameOverText.text = $"<align=center><color=#FF3B30><size=130%><b>[ DENETİM BAŞARISIZ ]</b></size></color>\n\n" +
                            $"<color=#AAAAAA><size=90%>─── İŞTEN ÇIKARILMA NEDENİ ───</size></color>\n" +
                            $"<color=#FF453A>■</color> Sebep: <b>{reason}</b>\n\n" +
                            $"<color=#AAAAAA><size=90%>───── VARDİYA ÖZETİ ─────</size></color>\n" +
                            $"<color=#FFD60A>■</color> Çalışılan Gün Sayısı: <b>{DayManager.Instance.CurrentDay} Gün</b>\n" +
                            $"<color=#FFD60A>■</color> {salaryText}</align>";

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

    private void PlayCorrectSound()
    {
        if (sfxSource != null && correctChoiceSound != null)
        {
            sfxSource.PlayOneShot(correctChoiceSound);
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
        StartCoroutine(UIAnimator.FadeInAndScale(briefingPanel)); // Menu ve Briefing aynı paneli kullanıyor
        reportPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // İmleci göster
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Müzik çal
        PlayBackgroundMusic();

        // Metin objesini yukarı çek (3 satırlı buton düzeni nedeniyle daha fazla yukarı çekiyoruz)
        RectTransform textRect = briefingContentText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.anchoredPosition = originalContentTextPosition + new Vector2(0f, 95f);
        }

        // Geri butonunu gizle
        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }

        // Oyuna Başla butonunu etkinleştir ve yeşil yap
        if (startDayButton != null)
        {
            startDayButton.gameObject.SetActive(true);
            var startImg = startDayButton.GetComponent<Image>();
            if (startImg != null)
            {
                startImg.color = new Color(0.18f, 0.77f, 0.31f); // Güzel bir zümrüt yeşili
            }
            UpdateButtonText(startDayButton, "OYUNA BAŞLA");
        }

        // Nasıl Oynanır butonunu etkinleştir ve mavi yap
        if (howToPlayButton != null)
        {
            howToPlayButton.gameObject.SetActive(true);
            var htpImg = howToPlayButton.GetComponent<Image>();
            if (htpImg != null)
            {
                htpImg.color = new Color(0.11f, 0.5f, 0.9f); // Şık bir mavi
            }
            UpdateButtonText(howToPlayButton, "NASIL OYNANIR");
        }

        // Mute butonunu aktif et ve gri yap
        if (muteButton != null)
        {
            muteButton.gameObject.SetActive(true);
            var muteImg = muteButton.GetComponent<Image>();
            if (muteImg != null)
            {
                muteImg.color = new Color(0.5f, 0.5f, 0.5f); // Şık bir gri
            }
            UpdateMuteButtonText();
        }

        // Oyundan Çık butonunu etkinleştir ve kırmızı yap
        if (quitGameButton != null)
        {
            quitGameButton.gameObject.SetActive(true);
            var quitImg = quitGameButton.GetComponent<Image>();
            if (quitImg != null)
            {
                quitImg.color = new Color(0.75f, 0.2f, 0.2f); // Kırmızı
            }
            UpdateButtonText(quitGameButton, "OYUNDAN ÇIK");
        }

        // Dört butonu konumlandır (Üç satırlı düzen: Üstte Oyuna Başla ve Müziği Kapa, ortada Nasıl Oynanır, altta Oyundan Çık)
        if (startDayButton != null && howToPlayButton != null && muteButton != null && quitGameButton != null)
        {
            if (startDayButton.transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
            {
                RectTransform startRect = startDayButton.GetComponent<RectTransform>();
                RectTransform htpRect = howToPlayButton.GetComponent<RectTransform>();
                RectTransform muteRect = muteButton.GetComponent<RectTransform>();
                RectTransform quitRect = quitGameButton.GetComponent<RectTransform>();

                if (startRect != null && htpRect != null && muteRect != null && quitRect != null)
                {
                    float w = originalStartButtonSize.x;
                    float h = originalStartButtonSize.y;
                    float horizontalSpacing = 20f;
                    float verticalSpacing = 15f;

                    // Row 1: Oyuna Başla (Sol) ve Müziği Kapa (Sağ)
                    startRect.sizeDelta = new Vector2(w, h);
                    muteRect.sizeDelta = new Vector2(w, h);

                    float row1Y = originalStartButtonPosition.y + h + verticalSpacing;
                    startRect.anchoredPosition = new Vector2(originalStartButtonPosition.x - (w + horizontalSpacing) / 2f, row1Y);
                    muteRect.anchoredPosition = new Vector2(originalStartButtonPosition.x + (w + horizontalSpacing) / 2f, row1Y);

                    // Row 2: Nasıl Oynanır (Geniş buton, ortada)
                    float combinedWidth = w * 2f + horizontalSpacing;
                    htpRect.sizeDelta = new Vector2(combinedWidth, h);
                    float row2Y = originalStartButtonPosition.y;
                    htpRect.anchoredPosition = new Vector2(originalStartButtonPosition.x, row2Y);

                    // Row 3: Oyundan Çık (Geniş buton, altta)
                    quitRect.sizeDelta = new Vector2(combinedWidth, h);
                    float row3Y = originalStartButtonPosition.y - h - verticalSpacing;
                    quitRect.anchoredPosition = new Vector2(originalStartButtonPosition.x, row3Y);
                }
            }
        }

        // Metinleri ata
        briefingTitleText.text = "";
        
        briefingContentText.text = "<align=center><line-height=150%><color=#FFAA00><size=120%>DENETİM VE KALİTE KONTROL SİMÜLASYONU'NA</size></color></line-height>\n" +
                                   "<line-height=115%><size=160%><b><color=#FFFFFF>HOŞ GELDİNİZ</color></b></size></align>\n\n" +
                                   "<color=#AAAAAA><size=90%>─────────────────────────────────────────</size></color>\n" +
                                   "<color=#CCCCCC><b>Göreviniz:</b> Banttan gelen kutuları günlük kurallara göre incelemek ve doğru paletlere (<color=#4CAF50><b>KABUL</b></color> veya <color=#F44336><b>RET</b></color>) yerleştirmektir.</color>\n\n" +
                                   "<color=#CCCCCC>Her gün değişen kurallara dikkat edin ve hata limitinizi aşmadan vardiyayı tamamlayın.</color>\n" +
                                   "<color=#AAAAAA><size=90%>─────────────────────────────────────────</size></color>\n" +
                                   "<align=center><color=#FFC107>Başlamak için aşağıdaki butona tıklayın!</color></align></line-height>";
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

    private void OnMenuHowToPlayClicked()
    {
        ShowHowToPlay(false);
    }

    private void OnPauseHowToPlayClicked()
    {
        ShowHowToPlay(true);
    }

    private void OnBackButtonClicked()
    {
        if (openedHowToPlayFromPause)
        {
            if (briefingPanel != null) briefingPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }
        else
        {
            ShowMenu();
        }
    }

    public void ShowHowToPlay(bool fromPause)
    {
        openedHowToPlayFromPause = fromPause;

        // Butonları gizle
        if (startDayButton != null) startDayButton.gameObject.SetActive(false);
        if (muteButton != null) muteButton.gameObject.SetActive(false);
        if (howToPlayButton != null) howToPlayButton.gameObject.SetActive(false);
        if (quitGameButton != null) quitGameButton.gameObject.SetActive(false);

        if (fromPause)
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (briefingPanel != null) briefingPanel.SetActive(true);
        }

        // Geri butonunu etkinleştir ve konumlandır
        if (backButton != null)
        {
            backButton.gameObject.SetActive(true);
            var backImg = backButton.GetComponent<Image>();
            if (backImg != null)
            {
                backImg.color = new Color(0.5f, 0.5f, 0.5f); // Gri renk
            }
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            if (backRect != null)
            {
                backRect.sizeDelta = originalStartButtonSize;
                backRect.anchoredPosition = originalStartButtonPosition;
            }
            UpdateButtonText(backButton, "GERİ");
        }

        // Nasıl Oynanır metnini yazdır ve yukarı kaydır
        RectTransform textRect = briefingContentText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.anchoredPosition = originalContentTextPosition + new Vector2(0f, 25f);
        }

        // Nasıl Oynanır metnini yazdır
        if (briefingTitleText != null)
        {
            briefingTitleText.text = "<color=#2196F3><b>NASIL OYNANIR?</b></color>";
        }

        if (briefingContentText != null)
        {
            briefingContentText.text = 
                "<line-height=115%><size=73%>" +
                "<color=#AAAAAA>───────────────── OYUN KONTROLLERİ ─────────────────</color>\n" +
                "  <color=#FFC107><b>W, A, S, D</b></color> : Hareket Etme  |  <color=#FFC107><b>FARE</b></color> : Çevreye Bakış / Kamera\n" +
                "  <color=#FFC107><b>E TUŞU</b></color> : Etkileşim (Kutuyu Al, Bırak, Tartıya Koy, Palete Bırak)\n" +
                "  <color=#FFC107><b>TAB (Basılı Tut)</b></color> : Not Defteri (Günlük kuralları ekranda tutar)\n" +
                "  <color=#FFC107><b>SOL SHIFT</b></color> : Koşma  |  <color=#FFC107><b>SOL CTRL (Basılı Tut)</b></color> : Bant Hızlandırma\n" +
                "<color=#AAAAAA>────────────────── OYUN MANTIĞI ──────────────────</color>\n" +
                "  <color=#555555>■</color> <b>Ayrıştırma:</b> Kutuları kurallara göre <color=#4CAF50><b>KABUL</b></color> veya <color=#F44336><b>RET</b></color> paletine yerleştirin.\n" +
                "  <color=#555555>■</color> <b>Kural Sistemi:</b> En az 1 ret şartını karşılayan kutu direkt ret edilmelidir.\n" +
                "  <color=#555555>■</color> <b>Tartı:</b> Ağır kutuları tartıp kilosunu ekranından kontrol edin.\n" +
                "  <color=#555555>■</color> <b>Güçlendiriciler:</b> Hangardaki kutulardan faydalı veya zararlı etkiler alın.\n" +
                "  <color=#555555>■</color> <b>Başarı:</b> Hata limitini aşmadan vardiya sonuna ulaşın.</size></line-height>";
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

    public void QuitGame()
    {
        Debug.Log("[GameManager] Oyundan çıkılıyor...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void UpdateMuteButtonText()
    {
        if (muteButton != null)
        {
            UpdateButtonText(muteButton, isMuted ? "MÜZİK AÇ" : "MÜZİĞİ KAPAT");
        }
        if (pauseMuteButton != null)
        {
            UpdateButtonText(pauseMuteButton, isMuted ? "Müzik Aç" : "Müziği Kapat");
        }
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (CurrentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (CurrentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && sensitivitySlider != null)
        {
            sensitivitySlider.value = player.MouseSensitivity;
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = player.MouseSensitivity.ToString("F1");
            }
        }

        UpdateMuteButtonText();
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (briefingPanel != null) briefingPanel.SetActive(false); // Nasıl Oynanır ekranı açık kaldıysa kapat
        if (hudPanel != null) hudPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnSensitivityChanged(float value)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.MouseSensitivity = value;
        }

        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = value.ToString("F1");
        }
    }

    private void UpdateButtonText(Button button, string newText)
    {
        if (button == null) return;
        
        string boldText = $"<b>{newText}</b>";
        
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = boldText;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = 26f;
            tmp.enableWordWrapping = false;
            return;
        }

        var txt = button.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = boldText;
            txt.fontStyle = FontStyle.Bold;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 14;
            txt.resizeTextMaxSize = 26;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}
