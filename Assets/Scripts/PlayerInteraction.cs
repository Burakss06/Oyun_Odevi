using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Etkileşim Ayarları")]
    [SerializeField] private float interactionDistance = 4.0f;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float followSpeed = 25f;

    [Header("Görsel (Outline) Ayarları")]
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 0.015f; // Daha belirgin kalınlık
    
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private GameObject lastTarget;
    private LineRenderer outlineLine;

    void Start()
    {
        // Kusursuz kenar çizgileri için bir LineRenderer oluşturuyoruz
        GameObject lineObj = new GameObject("SelectionOutlineLine");
        outlineLine = lineObj.AddComponent<LineRenderer>();
        
        // Çizgi özellikleri
        outlineLine.startWidth = outlineWidth;
        outlineLine.endWidth = outlineWidth;
        outlineLine.positionCount = 16;
        outlineLine.useWorldSpace = true;
        
        // Materyal ayarı (Parlak ve net beyaz için Unlit/Color kullanıyoruz)
        Shader unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
        
        Material outlineMat = new Material(unlitShader);
        outlineMat.color = outlineColor;
        outlineLine.material = outlineMat;
        
        // Çizginin her zaman en üstte gözükmesi için
        outlineLine.sortingOrder = 100;
        outlineLine.enabled = false;
    }

    void Update()
    {
        HandleHighlight();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject == null) TryPickUp();
            else DropObject();
        }
    }

    void LateUpdate()
    {
        // Taşıma mantığı (Pürüzsüz takip)
        if (heldObject != null)
        {
            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, holdPoint.position, Time.deltaTime * followSpeed);
            heldObject.transform.rotation = Quaternion.Slerp(heldObject.transform.rotation, holdPoint.rotation, Time.deltaTime * followSpeed);
        }

        // Eğer bir kutuya bakıyorsak, etrafındaki çerçeveyi her karede güncelle
        if (lastTarget != null && heldObject == null)
        {
            UpdateOutlinePositions(lastTarget);
        }
    }

    private void HandleHighlight()
    {
        if (heldObject != null)
        {
            if (outlineLine.enabled) outlineLine.enabled = false;
            lastTarget = null;
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, interactionDistance))
        {
            if (hit.collider.name.Contains("Cardboard Box"))
            {
                GameObject currentTarget = hit.collider.gameObject;
                if (currentTarget != lastTarget)
                {
                    lastTarget = currentTarget;
                    outlineLine.enabled = true;
                }
                return;
            }
        }

        if (outlineLine.enabled) outlineLine.enabled = false;
        lastTarget = null;
    }

    private void UpdateOutlinePositions(GameObject target)
    {
        BoxCollider col = target.GetComponent<BoxCollider>();
        if (col == null) return;

        // Kutu eğildiğinde de çizgilerin doğru sarması için Local uzayda noktaları hesaplıyoruz
        Vector3 center = col.center;
        Vector3 extents = col.size * 0.5f;
        extents += Vector3.one * 0.015f; // Çizginin içeri girmemesi için boşluk

        // Yerel noktaları dünya koordinatlarına çeviriyoruz
        Vector3 p0 = target.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z));
        Vector3 p1 = target.transform.TransformPoint(center + new Vector3( extents.x, -extents.y, -extents.z));
        Vector3 p2 = target.transform.TransformPoint(center + new Vector3( extents.x, -extents.y,  extents.z));
        Vector3 p3 = target.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y,  extents.z));
        Vector3 p4 = target.transform.TransformPoint(center + new Vector3(-extents.x,  extents.y, -extents.z));
        Vector3 p5 = target.transform.TransformPoint(center + new Vector3( extents.x,  extents.y, -extents.z));
        Vector3 p6 = target.transform.TransformPoint(center + new Vector3( extents.x,  extents.y,  extents.z));
        Vector3 p7 = target.transform.TransformPoint(center + new Vector3(-extents.x,  extents.y,  extents.z));

        Vector3[] points = new Vector3[16];
        points[0] = p0; points[1] = p1; points[2] = p2; points[3] = p3; points[4] = p0; // Alt kare
        points[5] = p4; points[6] = p5; points[7] = p1; points[8] = p5; // Ön-Sağ
        points[9] = p6; points[10] = p2; points[11] = p6; // Sağ-Arka
        points[12] = p7; points[13] = p3; points[14] = p7; // Arka-Sol
        points[15] = p4; // Üst kare sonu

        outlineLine.SetPositions(points);
    }

    private void TryPickUp()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, interactionDistance))
        {
            if (hit.collider.name.Contains("Cardboard Box"))
            {
                // Objeyi doğru alabilmek için eğer rigidbody child veya parent'taysa attachedRigidbody kullanıyoruz
                heldRigidbody = hit.collider.attachedRigidbody;
                
                if (heldRigidbody != null)
                {
                    heldObject = heldRigidbody.gameObject;
                    heldRigidbody.isKinematic = true;
                    heldRigidbody.useGravity = false;
                }
                else
                {
                    heldObject = hit.collider.gameObject;
                }
                
                outlineLine.enabled = false;
                lastTarget = null;
            }
        }
    }

    private void DropObject()
    {
        if (heldObject != null)
        {
            if (heldRigidbody != null)
            {
                heldRigidbody.isKinematic = false;
                heldRigidbody.useGravity = true;
                
                // Kutunun yamukken havada asılı kalmaması ve fiziğe uygun devrilmesi için
                // tüm dönüş kısıtlamalarını (freeze rotation) sıfırlıyoruz.
                heldRigidbody.constraints = RigidbodyConstraints.None;
                heldRigidbody.WakeUp();
            }
            heldObject = null;
            heldRigidbody = null;
        }
    }

    public void ResetInteraction()
    {
        heldObject = null;
        heldRigidbody = null;
        if (outlineLine != null)
        {
            outlineLine.enabled = false;
        }
        lastTarget = null;
    }
}
