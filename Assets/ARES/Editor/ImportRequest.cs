using System.IO;
using UnityEditor;

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
            if (File.Exists(Request))
            {
                File.Delete(Request);
                AresImporter.Import();
                ParityChecks.Run();
            }
            else if (File.Exists(ValidationRequest))
            {
                File.Delete(ValidationRequest);
                ParityChecks.Run();
            }
        }
    }
}
