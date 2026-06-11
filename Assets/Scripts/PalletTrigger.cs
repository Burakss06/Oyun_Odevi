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
            PalletType targetPallet = box.GetTargetPallet();
            isCorrect = (palletType == targetPallet);
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
        // Kutunun fiziğini kapat
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

        // Etkileşimi kes
        box.enabled = false;
        box.gameObject.name = "Cardboard Box (Stacked)";

        // Kutu rotasyonunu hizala
        float boxRotationY = 90f; 
        box.transform.rotation = transform.rotation * Quaternion.Euler(0, boxRotationY, 0);

        // Palet sınırlarını dinamik olarak belirle
        float palletWidth = 1.2f; 
        float palletDepth = 1.2f;
        Vector3 palletCenterWorld = transform.position;
        float palletTopY = transform.position.y + 0.15f;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = GetComponentInChildren<MeshFilter>();

        BoxCollider pCol = GetComponent<BoxCollider>();

        if (mf != null && mf.sharedMesh != null)
        {
            palletWidth = mf.sharedMesh.bounds.size.x * mf.transform.lossyScale.x;
            palletDepth = mf.sharedMesh.bounds.size.z * mf.transform.lossyScale.z;
            palletCenterWorld = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
            palletTopY = mf.transform.TransformPoint(new Vector3(mf.sharedMesh.bounds.center.x, mf.sharedMesh.bounds.max.y, mf.sharedMesh.bounds.center.z)).y;
        }
        else if (pCol != null)
        {
            palletWidth = pCol.size.x * transform.lossyScale.x;
            palletDepth = pCol.size.z * transform.lossyScale.z;
            palletCenterWorld = transform.TransformPoint(pCol.center);
            palletTopY = transform.TransformPoint(new Vector3(pCol.center.x, pCol.center.y + pCol.size.y / 2f, pCol.center.z)).y;
        }

        float pivotOffsetToBottom = 0f;

        BoxCollider boxCol = box.GetComponent<BoxCollider>();
        if (box.Shape == BoxController.BoxShape.Unfolded)
        {
            // Uzun kutuları dikey yerleştirdiğimiz için renderer sınırlarına göre hesaplama yap
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
        }
        else
        {
            // Normal kutular için collider sınırlarını baz al
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

        // İstif limiti dolunca temizle
        int index = stackedBoxes.Count;
        int maxPerRow = 3;
        int maxPerCol = 3;
        int layerCapacity = maxPerRow * maxPerCol;
        
        if (index >= layerCapacity)
        {
            ClearStackedBoxes();
            index = 0;
        }

        int row = index / maxPerRow;
        int col = index % maxPerRow;

        // Dönüşten ötürü boyutları değiştir (90 derece)
        float boxHalfWidth = 0.2f;
        float boxHalfDepth = 0.2f;
        if (boxCol != null)
        {
            boxHalfWidth = (boxCol.size.z * box.transform.lossyScale.z) / 2f;
            boxHalfDepth = (boxCol.size.x * box.transform.lossyScale.x) / 2f;
        }

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

        Vector3 localOffset = new Vector3(offsetX, 0, offsetZ);
        Vector3 worldOffset = transform.rotation * localOffset;

        // Paletin üst yüzeyine yerleştir
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
