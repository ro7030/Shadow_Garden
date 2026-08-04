#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShadowGarden.Infrastructure.EditorTools
{
    /// <summary>
    /// Keeps the project Temp folder present so Unity's FSTimeGet probe does not assert
    /// when the folder was wiped mid-reload / mid-test cleanup.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectTempFolderGuard
    {
        static ProjectTempFolderGuard()
        {
            EnsureTempFolder();
            EditorApplication.playModeStateChanged += _ => EnsureTempFolder();
            EditorApplication.projectChanged += EnsureTempFolder;
        }

        [MenuItem("ShadowGarden/Tools/Ensure Temp Folder")]
        private static void EnsureTempFolderMenu()
        {
            EnsureTempFolder();
            Debug.Log("[ShadowGarden] Temp folder ensured at " + GetTempPath());
        }

        internal static void EnsureTempFolder()
        {
            var temp = GetTempPath();
            if (!Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);
            }

            // Touch a stable probe file so FS clocks can resolve against an existing Temp entry.
            var probe = Path.Combine(temp, ".shadowgarden_temp_ok");
            if (!File.Exists(probe))
            {
                File.WriteAllText(probe, "ok");
            }
        }

        private static string GetTempPath() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
    }
}
#endif
