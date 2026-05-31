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
        bool isDefective = box.IsDefective;

        if (palletType == PalletType.Kabul)
        {
            // Kabul Paleti: Sadece sağlam (defosuz) kutular buraya konulmalı
            if (!isDefective)
            {
                isCorrect = true;
                Debug.Log("DOĞRU KARAR: Sağlam kutu kabul paletine bırakıldı.");
            }
            else
            {
                isCorrect = false;
                Debug.Log($"HATALI KARAR: Kusurlu kutu ({box.CurrentDefect}) kabul paletine bırakıldı!");
            }
        }
        else if (palletType == PalletType.Ret)
        {
            // Ret Paleti: Sadece defolu (kusurlu) kutular buraya konulmalı
            if (isDefective)
            {
                isCorrect = true;
                Debug.Log($"DOĞRU KARAR: Kusurlu kutu ({box.CurrentDefect}) ret paletine bırakıldı.");
            }
            else
            {
                isCorrect = false;
                Debug.Log("HATALI KARAR: Sağlam kutu ret paletine bırakıldı!");
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
