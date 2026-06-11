using UnityEditor;
using System.IO;
using UnityEngine;

public class AutoBuilder
{
    [MenuItem("Tools/Auto Build (Windows 64-bit)")]
    public static void BuildWindows64()
    {
        // Proje ve şirket bilgileri
        PlayerSettings.productName = "Denetim ve Kalite Kontrol Simülasyonu";
        PlayerSettings.companyName = "BAU";
        PlayerSettings.bundleVersion = "1.0.0";

        // Masaüstünde build klasörü yoksa oluştur
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string buildPath = Path.Combine(desktopPath, "build");
        
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        string exePath = Path.Combine(buildPath, "Denetim ve Kalite Kontrol Simülasyonu.exe");

        // Şu an açık olan sahneyi başlangıç sahnesi (index 0) yap
        string activeScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        System.Collections.Generic.List<string> scenePathsList = new System.Collections.Generic.List<string>();
        
        if (!string.IsNullOrEmpty(activeScenePath) && File.Exists(activeScenePath))
        {
            scenePathsList.Add(activeScenePath);
        }

        // Build Settings'deki diğer etkin sahneleri ekle (silinenleri filtrele)
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        foreach (var scene in scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path) && File.Exists(scene.path) && !scenePathsList.Contains(scene.path))
            {
                scenePathsList.Add(scene.path);
            }
        }

        string[] scenePaths = scenePathsList.ToArray();

        if (scenePaths.Length == 0)
        {
            Debug.LogError("[AutoBuilder] Sahne bulunamadı!");
            EditorUtility.DisplayDialog("Hata", "Build settings'de aktif sahne yok.", "Tamam");
            return;
        }

        Debug.Log("[AutoBuilder] Derleme başlatılıyor: " + exePath);

        // Windows 64-bit build parametreleri
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenePaths;
        buildPlayerOptions.locationPathName = exePath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[AutoBuilder] Build başarılı.");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError("[AutoBuilder] Build başarısız: " + report.summary.result);
        }
    }
}
