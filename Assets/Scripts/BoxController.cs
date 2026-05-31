using UnityEngine;
using System.Collections.Generic;

public class BoxController : MonoBehaviour
{
    public enum DefectType
    {
        None,          // Sağlam (Kusursuz)
        Damaged,       // Ezik / Kırık
        WrongColor,    // Yanlış Renk
        SizeAnomaly    // Boyut Hatası (Çok büyük veya çok küçük)
    }

    [Header("Kusur Bilgisi")]
    [SerializeField] private DefectType currentDefect = DefectType.None;
    public DefectType CurrentDefect => currentDefect;
    public bool IsDefective => currentDefect != DefectType.None;

    [Header("Görsel Efekt Ayarları")]
    [SerializeField] private Color wrongColorTint = new Color(0.9f, 0.1f, 0.1f); // Yanlış renk için kırmızı tonu

    private bool isEvaluated = false; // Palete konup değerlendirildi mi?
    public bool IsEvaluated => isEvaluated;
    private Vector3 originalScale;
    private Rigidbody rb;

    private void Awake()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
    }

    public void InitializeBox(DayConfig config)
    {
        // Kutunun defolu olup olmayacağına karar ver
        float randomVal = Random.value;
        if (randomVal <= config.defectSpawnChance)
        {
            // O gün aktif olan kusur türlerini listele
            List<DefectType> allowedDefects = new List<DefectType>();
            if (config.allowDamagedDefect) allowedDefects.Add(DefectType.Damaged);
            if (config.allowWrongColorDefect) allowedDefects.Add(DefectType.WrongColor);
            if (config.allowSizeAnomalyDefect) allowedDefects.Add(DefectType.SizeAnomaly);

            if (allowedDefects.Count > 0)
            {
                // Aktif kusurlardan birini rastgele seç
                currentDefect = allowedDefects[Random.Range(0, allowedDefects.Count)];
            }
            else
            {
                currentDefect = DefectType.None;
            }
        }
        else
        {
            currentDefect = DefectType.None;
        }

        // Kusurun görsel etkilerini uygula
        ApplyVisualDefect();
    }

    private void ApplyVisualDefect()
    {
        switch (currentDefect)
        {
            case DefectType.None:
                // Kusursuz kutu
                break;

            case DefectType.Damaged:
                // Ezik / Kırık Görünümü:
                // Kutuyu belirli bir eksende rastgele ez / yamult ve hafifçe döndür
                float squashX = Random.Range(0.82f, 0.90f);
                float squashY = Random.Range(0.68f, 0.78f);
                float squashZ = Random.Range(1.10f, 1.25f);
                
                // Eğer child mesh'ler varsa onları da hafifçe döndürerek kırılmış etkisi yaratabiliriz
                transform.localScale = new Vector3(originalScale.x * squashX, originalScale.y * squashY, originalScale.z * squashZ);
                
                // Rigidbody varsa ağırlık merkezini de bozarak fiziksel dengesizlik hissi verelim
                if (rb != null)
                {
                    rb.centerOfMass = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0f), Random.Range(-0.1f, 0.1f));
                }
                break;

            case DefectType.WrongColor:
                // Yanlış Renk Görünümü:
                // Kutunun tüm mesh renderer'larının materyal renklerini değiştirerek yanlış renge boya
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer ren in renderers)
                {
                    // Çizgi çizen LineRenderer'ları atla
                    if (ren is LineRenderer) continue;

                    // Materyali klonla ve rengi değiştir
                    ren.material.color = wrongColorTint;
                }
                break;

            case DefectType.SizeAnomaly:
                // Boyut Hatası Görünümü:
                // Kutu ya çok küçük ya da çok büyük olmalı (%50 ihtimal)
                float scaleMultiplier = (Random.value > 0.5f) ? 0.62f : 1.48f;
                transform.localScale = originalScale * scaleMultiplier;
                
                // Rigidbody kütlesini boyutuna göre güncelle
                if (rb != null)
                {
                    rb.mass *= scaleMultiplier;
                }
                break;
        }
    }

    public void MarkAsEvaluated()
    {
        isEvaluated = true;
    }

    private void Update()
    {
        // Eğer değerlendirilmediyse ve banttan aşağı veya sahne dışına düştüyse (Y pozisyonu çok düşükse)
        if (!isEvaluated && transform.position.y < -4.0f)
        {
            isEvaluated = true;
            
            if (GameManager.Instance != null)
            {
                if (IsDefective)
                {
                    // Oyuncu hatalı/defolu bir kutuyu kaçırdı! Ceza puanı/hata sayacı artar.
                    GameManager.Instance.AddIncorrectChoice();
                    Debug.Log($"Kusurlu kutu ({currentDefect}) banttan düştü! Hata sayacı arttı.");
                }
                else
                {
                    // Sağlam kutu bant sonuna kadar gitti, bu doğru bir süreç (kaçırma sayılmaz, normal akış)
                    GameManager.Instance.BoxMissed();
                    Debug.Log("Sağlam kutu başarıyla kontrolü geçti ve banttan düştü.");
                }
            }

            // Objeyi yok et
            Destroy(gameObject);
        }
    }
}
