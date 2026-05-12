using UnityEngine;
using UnityEditor;

public class ForkliftSignageManager : MonoBehaviour
{
    [Header("Settings")]
    public Font signFont;
    public Shader depthShader;
    public float fontSize = 0.11f;
    public Vector3 localOffset = new Vector3(0, 3.2f, 0.5f);

    [ContextMenu("Recreate Signs")]
    public void RecreateSigns()
    {
        // 1. Cleanup existing
        GameObject oldKabul = GameObject.Find("KABUL_Sign");
        GameObject oldRet = GameObject.Find("RET_Sign");
        if (oldKabul != null) DestroyImmediate(oldKabul);
        if (oldRet != null) DestroyImmediate(oldRet);

        // 2. Find forklifts
        GameObject forklift1 = GameObject.Find("Forklift_Instance埋/strong/Forklift");
        GameObject forklift2 = GameObject.Find("Forklift_Instance埋/strong (1)/Forklift");

        if (forklift1 == null || forklift2 == null)
        {
            Debug.LogError("Forklifts not found in scene! Please check hierarchy names.");
            return;
        }

        // 3. Load assets if not assigned
        if (signFont == null) signFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Roboto-Black.ttf");
        if (depthShader == null) depthShader = Shader.Find("Custom/3DTextDepth");
        if (depthShader == null) depthShader = Shader.Find("GUI/3D Text Shader");

        // 4. Create KABUL
        CreateSign("KABUL_Sign", "✔ KABUL", Color.green, forklift1.transform);
        
        // 5. Create RET
        CreateSign("RET_Sign", "✘ RET", Color.red, forklift2.transform);

        Debug.Log("Forklift signs recreated successfully.");
    }

    private void CreateSign(string name, string text, Color color, Transform parent)
    {
        GameObject signGO = new GameObject(name);
        signGO.transform.SetParent(parent);
        signGO.transform.localPosition = localOffset;
        signGO.transform.localRotation = Quaternion.Euler(0, 180, 0);

        TextMesh tm = signGO.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = signFont;
        tm.fontSize = 60;
        tm.characterSize = fontSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        MeshRenderer renderer = signGO.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = signFont.material;

        if (depthShader != null)
        {
            Material signMat = new Material(depthShader);
            signMat.mainTexture = signFont.material.mainTexture;
            signMat.SetColor("_Color", color);
            renderer.sharedMaterial = signMat;
        }
    }
}
