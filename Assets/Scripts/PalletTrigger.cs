using UnityEngine;
using System.Collections.Generic;

public class PalletTrigger : MonoBehaviour
{
    public enum PalletType { Kabul, Ret }

    [Header("Palet Ayarları")]
    [SerializeField] private PalletType palletType = PalletType.Kabul;

    private List<GameObject> stackedBoxes = new List<GameObject>();

    public PalletType GetPalletType()
    {
        return palletType;
    }

    // Awake metodu paletin orjinal yapısını bozmamak için kaldırıldı.

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
            isCorrect = (Random.value > 0.5f);
        }
        else
        {
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
                isCorrect = (palletType == PalletType.Ret);
            }
            else if (DayManager.Instance != null)
            {
                DayConfig config = DayManager.Instance.GetCurrentDayConfig();
                if (config.allowWeightDefect && box.Weight >= 10.0f)
                {
                    isCorrect = (palletType == PalletType.Ret);
                }
                else
                {
                    if (GameManager.Instance != null && GameManager.Instance.DailyRules != null && 
                        GameManager.Instance.DailyRules.TryGetValue(box.Shape, out var targetPallet))
                    {
                        isCorrect = (palletType == targetPallet);
                    }
                    else
                    {
                        bool isDefective = box.IsDefective;
                        isCorrect = (palletType == PalletType.Kabul) ? !isDefective : isDefective;
                    }
                }
            }
        }

        if (GameManager.Instance != null)
        {
            if (isCorrect) GameManager.Instance.AddCorrectChoice();
            else GameManager.Instance.AddIncorrectChoice();
        }

        StackBox(box);
    }

    private void StackBox(BoxController box)
    {
        // 1. Kutunun fiziğini kapat
        Rigidbody rb = box.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider[] colliders = box.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        // 2. Etkileşimi kes
        box.enabled = false;
        box.gameObject.name = "Cardboard Box (Stacked)";

        // Kutu rotasyonunu her zaman düzeltiyoruz
        float boxRotationY = 90f; 
        box.transform.rotation = transform.rotation * Quaternion.Euler(0, boxRotationY, 0);

        // Paletin GÖRSEL (Mesh) veya FİZİKSEL (Collider) boyutlarını dinamik okuyarak
        // her türlü Scale ve Position değişikliğine anında uyum sağlamasını sağlıyoruz.
        float palletWidth = 1.2f; 
        float palletDepth = 1.2f;
        Vector3 palletCenterWorld = transform.position;
        float palletTopY = transform.position.y + 0.15f; // Varsayılan değer

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = GetComponentInChildren<MeshFilter>(); // Alt objede (child) olma ihtimaline karşı

        BoxCollider pCol = GetComponent<BoxCollider>();

        if (mf != null && mf.sharedMesh != null)
        {
            // Direkt olarak görsel 3D modelin gerçek limitlerini, kendi transform'unu baz alarak okuyoruz
            palletWidth = mf.sharedMesh.bounds.size.x * mf.transform.lossyScale.x;
            palletDepth = mf.sharedMesh.bounds.size.z * mf.transform.lossyScale.z;
            palletCenterWorld = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
            // Paletin tam üst yüzeyinin Dünya (World) koordinatlarındaki yüksekliği
            palletTopY = mf.transform.TransformPoint(new Vector3(mf.sharedMesh.bounds.center.x, mf.sharedMesh.bounds.max.y, mf.sharedMesh.bounds.center.z)).y;
        }
        else if (pCol != null)
        {
            // Eğer mesh yoksa collider referans alınır
            palletWidth = pCol.size.x * transform.lossyScale.x;
            palletDepth = pCol.size.z * transform.lossyScale.z;
            palletCenterWorld = transform.TransformPoint(pCol.center);
            palletTopY = transform.TransformPoint(new Vector3(pCol.center.x, pCol.center.y + pCol.size.y / 2f, pCol.center.z)).y;
        }

        float pivotOffsetToBottom = 0f;

        BoxCollider boxCol = box.GetComponent<BoxCollider>();
        if (box.Shape == BoxController.BoxShape.Unfolded)
        {
            // Düz (uzun) kutuları dikey yerleştiriyoruz.
            // Bu kutularda BoxCollider modelle tam uyuşmadığı (kısa kaldığı) için 
            // dikey durduklarında paletin altına göçmelerini engellemek adına görsel sınırları (Renderer) kullanıyoruz.
            Renderer[] renderers = box.GetComponentsInChildren<Renderer>();
            bool boundsInitialized = false;
            Bounds visualBounds = new Bounds();
            foreach (Renderer r in renderers)
            {
                if (r is LineRenderer || r.gameObject.name.Contains("Barcode")) continue;
                if (!boundsInitialized) { visualBounds = r.bounds; boundsInitialized = true; }
                else visualBounds.Encapsulate(r.bounds);
            }

            if (boundsInitialized)
            {
                pivotOffsetToBottom = box.transform.position.y - visualBounds.min.y;
            }
            
            // Taşırma ve göçme sorununu dengelemek için güvenlik payı
            pivotOffsetToBottom += 0.18f;

            // Sadece boyut anomalisine sahip düz(uzun) kutular için diğerlerini bozmadan ekstra düzeltme
            if (box.CurrentDefect == BoxController.DefectType.SizeAnomaly)
            {
                if (box.transform.localScale.y > 1.0f)
                {
                    pivotOffsetToBottom += 0.04f; // Büyümüşse daha yukarı çek (palete göçmesini engelle)
                }
                else
                {
                    pivotOffsetToBottom -= 0.08f; // Küçülmüşse havada kalmaması için aşağı indir
                }
            }
        }
        else
        {
            // Normal kutular için katı fiziksel çarpışma kutusundan (BoxCollider) hesaplama
            if (boxCol != null)
            {
                float bottomY = box.transform.TransformPoint(boxCol.center - new Vector3(0, boxCol.size.y / 2f, 0)).y;
                pivotOffsetToBottom = box.transform.position.y - bottomY;
            }
            else
            {
                Renderer r = box.GetComponentInChildren<Renderer>();
                if (r != null) pivotOffsetToBottom = box.transform.position.y - r.bounds.min.y;
            }
        }

        // Zemin dolarsa temizle
        int index = stackedBoxes.Count;
        int maxPerRow = 3;
        int maxPerCol = 3;
        int layerCapacity = maxPerRow * maxPerCol;
        
        if (index >= layerCapacity)
        {
            // Zemin tam dolduğunda eski kutuları yok edip baştan başlıyoruz ki kutular üst üste çıkıp bug oluşturmasın
            ClearStackedBoxes();
            index = 0;
        }

        int row = index / maxPerRow;
        int col = index % maxPerRow;

        // Kutunun yönüne göre genişliğini hesapla
        float boxHalfWidth = 0.2f;
        float boxHalfDepth = 0.2f;
        if (boxCol != null)
        {
            // Kutu 90 derece döndürüldüğü için X ve Z yer değiştirir (Genişlik ve Derinlik)
            boxHalfWidth = (boxCol.size.z * box.transform.lossyScale.z) / 2f;
            boxHalfDepth = (boxCol.size.x * box.transform.lossyScale.x) / 2f;
        }

        // KUTULARIN ARASINDAKİ MESAFELERİ OTOMATİK HESAPLA (Paletin güncel scale'ine göre)
        // Eğer ekstra içeri çekmek/dışarı itmek isterseniz padding değerini değiştirebilirsiniz
        float padding = 0.0f; 
        
        float offsetX = 0f;
        if (col == 0) offsetX = -(palletWidth / 2f) + boxHalfWidth + padding;
        else if (col == 1) offsetX = 0f;
        else if (col == 2) offsetX = (palletWidth / 2f) - boxHalfWidth - padding;

        float offsetZ = 0f;
        if (row == 0) offsetZ = -(palletDepth / 2f) + boxHalfDepth + padding;
        else if (row == 1) offsetZ = 0f;
        else if (row == 2) offsetZ = (palletDepth / 2f) - boxHalfDepth - padding;

        box.transform.SetParent(this.transform, true);

        // Dünya koordinatlarında tam rotasyona göre yerleşim hesaplama
        Vector3 localOffset = new Vector3(offsetX, 0, offsetZ);
        Vector3 worldOffset = transform.rotation * localOffset;

        // Kutu DAİMA paletin en üst yüzeyine yerleştirilir
        box.transform.position = new Vector3(
            palletCenterWorld.x + worldOffset.x,
            palletTopY + pivotOffsetToBottom,
            palletCenterWorld.z + worldOffset.z
        );

        stackedBoxes.Add(box.gameObject);
    }

    public void ClearStackedBoxes()
    {
        foreach (GameObject boxObj in stackedBoxes)
        {
            if (boxObj != null)
            {
                Destroy(boxObj);
            }
        }
        stackedBoxes.Clear();
    }
}
