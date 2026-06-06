using UnityEngine;

public class FloatingTextAnimator : MonoBehaviour
{
    public float floatSpeed = 1.5f;
    public float fadeSpeed = 0.25f;
    private TextMesh textMesh;
    private Color textColor;

    private void Start()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh != null) textColor = textMesh.color;
        Destroy(gameObject, 4f); // 4 saniye sonra otomatik yok olur
    }

    private void Update()
    {
        // Yukarı doğru süzülme
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        
        // Hep kameraya doğru baksın (Billboard)
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        // Yavaşça şeffaflaşma (Fade out)
        if (textMesh != null)
        {
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
        }
    }
}
