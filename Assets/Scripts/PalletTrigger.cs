using UnityEngine;

public class PalletTrigger : MonoBehaviour
{
    public enum PalletType { Kabul, Ret }

    [Header("Palet Ayarları")]
    [SerializeField] private PalletType palletType = PalletType.Kabul;

    public PalletType GetPalletType()
    {
        return palletType;
    }

    private void Awake()
    {
        // Oyuncunun palete tam aşağı bakmadan da "E" etkileşimini görebilmesi için
        // paletin algılama alanını (Trigger Collider) yukarı doğru 3 metre uzatıyoruz.
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null && col.isTrigger)
        {
            Vector3 newSize = col.size;
            newSize.y += 3.0f; // Yukarı doğru 3 birim uzat
            col.size = newSize;

            Vector3 newCenter = col.center;
            newCenter.y += 1.5f; // Merkeze göre uzadığı için yarısı kadar da yukarı kaydır
            col.center = newCenter;
        }
    }

    public void PlaceBox(BoxController box)
    {
        if (box != null && !box.IsEvaluated)
        {
            EvaluateBox(box);
        }
    }

    private void EvaluateBox(BoxController box)
    {
        box.MarkAsEvaluated();

        bool isCorrect = false;

        if (box.isMysteryBox)
        {
            // Sürpriz kutu: %50 şansla doğru veya yanlış
            isCorrect = (Random.value > 0.5f);
            Debug.Log($"SÜRPRİZ KUTU DEĞERLENDİRİLDİ: Sonuç şans eseri {(isCorrect ? "DOĞRU" : "HATALI")} çıktı.");
        }
        else
        {
            // Aktif fiziksel kusurları kontrol et (Renk ve Boyut hataları her zaman Ret paletine gitmelidir)
            bool hasActiveDefect = false;
            if (DayManager.Instance != null && box.CurrentDefect != BoxController.DefectType.None)
            {
                DayConfig config = DayManager.Instance.GetCurrentDayConfig();
                if (config.allowBarcodeDefect && box.CurrentDefect == BoxController.DefectType.BarcodeAnomaly) hasActiveDefect = true;
                if (config.allowWrongColorDefect && box.CurrentDefect == BoxController.DefectType.WrongColor) hasActiveDefect = true;
                if (config.allowSizeAnomalyDefect && box.CurrentDefect == BoxController.DefectType.SizeAnomaly) hasActiveDefect = true;
            }

            if (hasActiveDefect)
            {
                // Fiziksel kusuru olan kutular her zaman RET paletine yerleştirilmelidir
                isCorrect = (palletType == PalletType.Ret);
                if (isCorrect)
                    Debug.Log($"DOĞRU KARAR: Kusurlu kutu ({box.CurrentDefect}) ret paletine bırakıldı.");
                else
                    Debug.Log($"HATALI KARAR: Kusurlu kutu ({box.CurrentDefect}) kabul paletine bırakıldı!");
            }
            // Ağırlık kontrolü: 6. gün ve sonrasında 10 kg üstü kutular RET'e gitmeli
            else if (DayManager.Instance != null)
            {
                DayConfig config = DayManager.Instance.GetCurrentDayConfig();
                if (config.allowWeightDefect && box.Weight >= 10.0f)
                {
                    isCorrect = (palletType == PalletType.Ret);
                    if (isCorrect)
                        Debug.Log($"DOĞRU KARAR: Ağır kutu ({box.Weight:F1} kg) ret paletine bırakıldı.");
                    else
                        Debug.Log($"HATALI KARAR: Ağır kutu ({box.Weight:F1} kg) kabul paletine bırakıldı!");
                }
                else
                {
                    // Normal kutular için gün kurallarını kontrol et
                    if (GameManager.Instance != null && GameManager.Instance.DailyRules != null && 
                        GameManager.Instance.DailyRules.TryGetValue(box.Shape, out var targetPallet))
                    {
                        isCorrect = (palletType == targetPallet);
                        if (isCorrect)
                            Debug.Log($"DOĞRU KARAR: {box.Shape} kutu doğru palete ({palletType}) bırakıldı.");
                        else
                            Debug.Log($"HATALI KARAR: {box.Shape} kutu yanlış palete ({palletType}) bırakıldı! Hedef: {targetPallet}");
                    }
                    else
                    {
                        // Varsayılan kontrol
                        bool isDefective = box.IsDefective;
                        if (palletType == PalletType.Kabul)
                        {
                            isCorrect = !isDefective;
                        }
                        else
                        {
                            isCorrect = isDefective;
                        }
                    }
                }
            }
        }

        // Skoru veya hataları GameManager üzerinden güncelle
        if (GameManager.Instance != null)
        {
            if (isCorrect)
            {
                GameManager.Instance.AddCorrectChoice();
            }
            else
            {
                GameManager.Instance.AddIncorrectChoice();
            }
        }

        // Paletin üstünü boşaltmak ve performansı korumak için kutuyu yok et
        Destroy(box.gameObject, 0.3f);
    }
}
