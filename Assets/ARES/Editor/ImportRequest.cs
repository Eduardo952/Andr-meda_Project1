using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    /// <summary>Local automation hook. Only the fixed, project-local sentinel triggers an import.</summary>
    [InitializeOnLoad]
    internal static class ImportRequest
    {
        const string Request = "Temp/ares-import.request";
        const string ValidationRequest = "Temp/ares-validate.request";
        static ImportRequest() { EditorApplication.update += Process; }
        static void Process()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string request = File.Exists(Request) ? Request : File.Exists(ValidationRequest) ? ValidationRequest : null;
            if (request == null) return;
            string token = request + File.GetLastWriteTimeUtc(request).Ticks;
            if (SessionState.GetString("ARES.RequestRefresh", "") != token)
            {
                SessionState.SetString("ARES.RequestRefresh", token);
                AssetDatabase.Refresh();
                return; // Let script compilation/reload finish before using the importer or checks.
            }
            File.Delete(request);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ares-request-status.txt", DateTime.UtcNow.ToString("O") + " RUNNING " + request);
            try
            {
                if (request == Request) AresImporter.Import();
                ParityChecks.Run();
                TaskInteractionChecks.Run();
                File.WriteAllText("Logs/ares-request-status.txt", DateTime.UtcNow.ToString("O") + " PASS " + request);
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/ares-parity.txt", "FAIL: " + error);
                File.WriteAllText("Logs/ares-request-status.txt", DateTime.UtcNow.ToString("O") + " FAIL " + error);
                Debug.LogException(error);
            }
        }
    }
}
