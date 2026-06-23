using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// 커맨드라인(batchmode) 빌드용 진입점.
    /// 예: Unity -quit -batchmode -projectPath . -executeMethod Echoes.EditorTools.CommandLineBuild.BuildWindows
    /// </summary>
    public static class CommandLineBuild
    {
        private static string[] EnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        private static void Build(BuildTarget target, string outPath)
        {
            var opts = new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = outPath,
                target = target,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary s = report.summary;
            Debug.Log($"[CommandLineBuild] target={target} result={s.result} sizeMB={s.totalSize / (1024f * 1024f):F1} errors={s.totalErrors} warnings={s.totalWarnings} out={outPath}");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Build/Windows/ECHOES.exe");
        }

        public static void BuildMac()
        {
            Build(BuildTarget.StandaloneOSX, "Build/ECHOES.app");
        }
    }
}
