using UnityEngine;

public class PalletTrigger : MonoBehaviour
{
    public enum PalletType { Kabul, Ret }

    [Header("Palet Ayarları")]
    [SerializeField] private PalletType palletType = PalletType.Kabul;

    private void OnTriggerStay(Collider other)
    {
        // Çarpışan objenin kutu olup olmadığını kontrol et
        BoxController box = other.GetComponent<BoxController>();
        if (box == null)
        {
            // Eğer parent'ında varsa oradan al (rigged prefabların yapısına uyum sağlamak için)
            box = other.GetComponentInParent<BoxController>();
        }

        // Eğer kutu bulunduysa ve henüz değerlendirilmediyse
        if (box != null && !box.IsEvaluated)
        {
            Rigidbody rb = box.GetComponent<Rigidbody>();
            if (rb == null) return;

            // Oyuncu kutuyu elinde tutarken (isKinematic = true) değerlendirme YAPMA.
            // Oyuncu kutuyu palete bıraktığı an (isKinematic = false) değerlendirme başlasın.
            if (!rb.isKinematic)
            {
                EvaluateBox(box);
            }
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
