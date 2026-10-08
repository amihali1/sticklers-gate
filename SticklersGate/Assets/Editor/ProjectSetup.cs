using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time project setup tasks that can run headless:
/// Unity.exe -batchmode -projectPath SticklersGate -executeMethod ProjectSetup.ImportTmpEssentials
/// (no -quit: the import is asynchronous, so the method exits Unity itself when it completes)
/// </summary>
public static class ProjectSetup
{
    /// <summary>
    /// Imports TextMeshPro Essential Resources (default SDF font, shaders, TMP Settings)
    /// from the ugui package, same as Window > TextMeshPro > Import TMP Essential Resources.
    /// </summary>
    public static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            Debug.Log("ProjectSetup: TMP Essential Resources already imported.");
            ExitIfBatch(0);
            return;
        }

        var package = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui@*")
            .Select(dir => Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage"))
            .FirstOrDefault(File.Exists);

        if (package == null)
        {
            Debug.LogError("ProjectSetup: TMP Essential Resources.unitypackage not found in the ugui package.");
            ExitIfBatch(1);
            return;
        }

        AssetDatabase.importPackageCompleted += name =>
        {
            AssetDatabase.Refresh();
            Debug.Log($"ProjectSetup: imported {name}");
            ExitIfBatch(0);
        };
        AssetDatabase.importPackageFailed += (name, error) =>
        {
            Debug.LogError($"ProjectSetup: import of {name} failed: {error}");
            ExitIfBatch(1);
        };
        AssetDatabase.ImportPackage(package, false);
    }

    private static void ExitIfBatch(int code)
    {
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
}
