using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Ares.Editor
{
    /// <summary>Opt-in local Windows build; never replaces an existing external installation.</summary>
    [InitializeOnLoad]
    public static class SceneryBuildRequest
    {
        const string Request="Temp/ares-scenery-build.request";
        static SceneryBuildRequest(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Temp/ares-import.request"))return;
            File.Delete(Request);Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ares-scenery-build.txt","RUNNING");
            try
            {
                ReferenceProvenance.Validate();WorldImportChecks.Run();
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                    scenes=new[]{"Assets/ARES/Scenes/BaseARES.unity"},
                    locationPathName="Builds/AresBase/AresBase.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                File.WriteAllText("Logs/ares-scenery-build.txt",$"{report.summary.result}: {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings, {report.summary.totalSize} bytes, {report.summary.totalTime}.\nBuilds/AresBase/AresBase.exe");
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Scenery player build failed");
            }
            catch(Exception error){File.AppendAllText("Logs/ares-scenery-build.txt","\nFAIL: "+error);UnityEngine.Debug.LogException(error);}
        }
    }
}
