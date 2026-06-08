using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Etkileşim Ayarları")]
    [SerializeField] private float interactionDistance = 2.2f;
    public static float InteractionDistanceMultiplier = 1.0f;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float followSpeed = 25f;

    [Header("Tartı Etkileşimi")]
    [SerializeField] private float scaleInteractionDistance = 3.5f;

    [Header("Görsel (Outline) Ayarları")]
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 0.015f; // Daha belirgin kalınlık
    
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private GameObject lastTarget;
    private LineRenderer outlineLine;
    private Collider playerCollider;
    private WeighingScale lookedAtScale = null;

    // Public getters for UI and crosshair feedback
    public bool IsHoldingObject => heldObject != null;
    public bool IsHoveringInteractable => lastTarget != null;
    public bool IsLookingAtScale => lookedAtScale != null;
    public WeighingScale LookedAtScale => lookedAtScale;

    private struct ColliderState
    {
        public Collider collider;
        public bool originalIsTrigger;
    }
    private List<ColliderState> heldColliders = new List<ColliderState>();

    [Header("Ses Efektleri")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip grabSound;
    [SerializeField] private AudioClip putSound;

    private void Awake()
    {
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (grabSound == null)
        {
            grabSound = Resources.Load<AudioClip>("Audio/grab_item");
        }
        if (putSound == null)
        {
            putSound = Resources.Load<AudioClip>("Audio/put_item");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void Start()
    {
        playerCollider = GetComponent<CharacterController>();

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
        DetectScale();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject == null)
            {
                // Elimiz boşken E'ye bastık
                if (lookedAtScale != null && lookedAtScale.HasBox)
                {
                    // Tartıya bakıyoruz ve tartıda kutu var -> Tartıdan al
                    RetrieveBoxFromScale();
                }
                else
                {
                    // Normal yerden kutu kaldır
                    TryPickUp();
                }
            }
            else
            {
                // Elimizde kutu varken E'ye bastık
                if (lookedAtScale != null && !lookedAtScale.HasBox)
                {
                    // Tartıya bakıyoruz ve tartı boş -> Tartıya koy
                    PlaceBoxOnScale();
                }
                else
                {
                    // Normal yere bırak
                    DropObject();
                }
            }
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

        // Kapsamlı tarama: Lazerin çarptığı her şeyi listele
        float dist = interactionDistance * InteractionDistanceMultiplier;
        RaycastHit[] hits = Physics.RaycastAll(playerCamera.position, playerCamera.forward, dist);
        
        // Mesafeye göre yakından uzağa sırala
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.name.Contains("Cardboard Box"))
            {
                GameObject currentTarget = hit.collider.gameObject;
                if (currentTarget != lastTarget)
                {
                    lastTarget = currentTarget;
                    outlineLine.enabled = true;
                }
                return; // Kutuyu bulduk, işlemi bitir
            }

            // Kutu değilse ve görünmez bir etkileşim alanı (trigger) da değilse, muhtemelen katı bir engeldir (cam, duvar vb.)
            // Ancak Forklift'in görünmez büyük bir katı çarpışma alanı olduğu için onu istisna tutuyoruz ki hata çözülmeye devam etsin.
            if (!hit.collider.isTrigger && !hit.collider.name.ToLower().Contains("forklift"))
            {
                // Cam veya başka bir katı duvara çarptık. Arkasındaki kutuyu almayı engellemek için taramayı durdur.
                break;
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
        float dist = interactionDistance * InteractionDistanceMultiplier;
        RaycastHit[] hits = Physics.RaycastAll(playerCamera.position, playerCamera.forward, dist);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.name.Contains("Cardboard Box"))
            {
                ExecutePickUp(hit.collider);
                return;
            }

            if (!hit.collider.isTrigger && !hit.collider.name.ToLower().Contains("forklift"))
            {
                // Cam veya duvar arkasından almayı engelle
                break;
            }
        }
    }

    private void ExecutePickUp(Collider hitCollider)
    {
        heldRigidbody = hitCollider.attachedRigidbody;
        
        if (heldRigidbody != null)
        {
            heldObject = heldRigidbody.gameObject;
            heldRigidbody.isKinematic = true;
            heldRigidbody.useGravity = false;
        }
        else
        {
            heldObject = hitCollider.gameObject;
        }

        // Eğer bu kutu tartı üzerindeyse, tartıdan düzgün bir şekilde çıkarıp sıfırlayalım
        var scale = Object.FindAnyObjectByType<WeighingScale>();
        if (scale != null && scale.HasBox && scale.CurrentBox != null && (scale.CurrentBox.gameObject == heldObject || scale.CurrentBox.transform == heldObject.transform))
        {
            scale.RetrieveBox();
        }

        SetHeldObjectCollision(heldObject, true);
        
        outlineLine.enabled = false;
        lastTarget = null;

        PlaySound(grabSound);
    }

    private void DropObject()
    {
        if (heldObject != null)
        {
            PlaySound(putSound);

            // Bırakmadan önce çarpışmayı geri aç
            SetHeldObjectCollision(heldObject, false);

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

    // ========== TARTI ETKİLEŞİMİ ==========

    /// <summary>
    /// Oyuncunun baktığı yönde tartı var mı kontrol eder.
    /// </summary>
    private void DetectScale()
    {
        lookedAtScale = null;
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, scaleInteractionDistance))
        {
            WeighingScale scale = hit.collider.GetComponent<WeighingScale>();
            if (scale == null) scale = hit.collider.GetComponentInParent<WeighingScale>();
            if (scale != null)
            {
                lookedAtScale = scale;
            }
        }
    }

    /// <summary>
    /// Kutuyu tartıya yerleştirir.
    /// </summary>
    private void PlaceBoxOnScale()
    {
        if (lookedAtScale == null || heldObject == null) return;

        BoxController box = heldObject.GetComponent<BoxController>();
        if (box == null) box = heldObject.GetComponentInParent<BoxController>();
        if (box != null && !lookedAtScale.HasBox)
        {
            // Önce collision'ı geri yükle
            SetHeldObjectCollision(heldObject, false);

            // Kutuyu tartıya yerleştir
            lookedAtScale.PlaceBox(box);

            // Elimizden bırak
            heldObject = null;
            heldRigidbody = null;

            PlaySound(putSound);
        }
    }

    /// <summary>
    /// Kutuyu tartıdan geri alır.
    /// </summary>
    private void RetrieveBoxFromScale()
    {
        if (lookedAtScale == null || !lookedAtScale.HasBox) return;

        BoxController box = lookedAtScale.RetrieveBox();
        if (box != null)
        {
            heldObject = box.gameObject;
            heldRigidbody = box.GetComponent<Rigidbody>();

            if (heldRigidbody != null)
            {
                heldRigidbody.isKinematic = true;
                heldRigidbody.useGravity = false;
            }

            SetHeldObjectCollision(heldObject, true);
            PlaySound(grabSound);
        }
    }

    /// <summary>
    /// Tutulan objenin collider'larını player ile ignore/restore eder ve trigger durumunu ayarlar.
    /// </summary>
    private void SetHeldObjectCollision(GameObject obj, bool ignore)
    {
        if (obj == null || playerCollider == null) return;

        if (ignore)
        {
            heldColliders.Clear();
            Collider[] cols = obj.GetComponentsInChildren<Collider>();
            foreach (Collider col in cols)
            {
                ColliderState state;
                state.collider = col;
                state.originalIsTrigger = col.isTrigger;
                heldColliders.Add(state);

                Physics.IgnoreCollision(col, playerCollider, true);
                col.isTrigger = true; // Fiziksel çakışmaları (uçma/titreme) önlemek için trigger yap
            }
        }
        else
        {
            foreach (ColliderState state in heldColliders)
            {
                if (state.collider != null)
                {
                    Physics.IgnoreCollision(state.collider, playerCollider, false);
                    state.collider.isTrigger = state.originalIsTrigger;
                }
            }
            heldColliders.Clear();
        }
    }

    public void ResetInteraction()
    {
        if (heldObject != null)
        {
            SetHeldObjectCollision(heldObject, false);
        }
        heldObject = null;
        heldRigidbody = null;
        if (outlineLine != null)
        {
            outlineLine.enabled = false;
        }
        lastTarget = null;
    }
}
