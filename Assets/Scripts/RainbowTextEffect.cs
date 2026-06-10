using UnityEngine;

public class RainbowTextEffect : MonoBehaviour
{
    private TextMesh textMesh;
    public float speed = 0.5f;

    void Start()
    {
        textMesh = GetComponent<TextMesh>();
    }

    void Update()
    {
        if (textMesh != null)
        {
            float hue = Mathf.Repeat(Time.time * speed, 1f);
            textMesh.color = Color.HSVToRGB(hue, 1f, 1f);
        }
    }
}
