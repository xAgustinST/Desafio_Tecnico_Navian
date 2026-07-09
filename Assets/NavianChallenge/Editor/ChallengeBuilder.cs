using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NavianChallenge.EditorTools
{
    /// <summary>
    /// Produces a standalone Windows .exe of the challenge scene so it can be launched
    /// without opening the Unity Editor. Menu item for interactive use; BuildWindows() is
    /// also callable from the command line via `-executeMethod` for CI/batch builds.
    /// </summary>
    public static class ChallengeBuilder
    {
        const string ScenePath = "Assets/NavianChallenge/Scenes/NavianChallenge_Main.unity";
        const string OutputPath = "Builds/Windows/NavianChallenge.exe";

        [MenuItem("Navian/Build Windows Executable")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log($"[ChallengeBuilder] result={summary.result} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings} time={summary.totalTime}");

            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
