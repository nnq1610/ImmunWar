using System;
using System.IO;
using System.Xml.Linq;
using ImmunWar.Core.Config;
using ImmunWar.Editor.AssetProvenance;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ImmunWar.Editor
{
    public static class BuildCommands
    {
        private const string Output = "Builds/Windows/ImmuneWar.exe";

        [MenuItem("Immune War/Build/Windows Development")]
        public static void BuildWindowsDevelopment() => Build(BuildOptions.Development | BuildOptions.AllowDebugging);

        [MenuItem("Immune War/Build/Windows Release")]
        public static void BuildWindowsRelease() => Build(BuildOptions.CleanBuildCache);

        public static void BuildWindows() => BuildWindowsRelease();

        private static void Build(BuildOptions options)
        {
            ValidateReleaseGates();
            ConfigValidationMenu.ValidateSelectedCatalog();
            ThirdPartyNoticeGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(Output) ?? "Builds/Windows");
            var enabledScenes = Array.FindAll(EditorBuildSettings.scenes, x => x.enabled);
            var paths = Array.ConvertAll(enabledScenes, x => x.path);
            if (paths.Length == 0) throw new InvalidOperationException("No enabled scenes are configured.");
            var report = BuildPipeline.BuildPlayer(paths, Output, BuildTarget.StandaloneWindows64, options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
            Debug.Log("IMMUNEWAR_BUILD_OK: " + Path.GetFullPath(Output));
        }

        private static void ValidateReleaseGates()
        {
            foreach (var result in new[] { "TestResults/EditMode-results.xml", "TestResults/PlayMode-results.xml", "TestResults/Performance-results.xml" })
            {
                if (!File.Exists(result)) throw new InvalidOperationException("Required test result is missing: " + result);
                var run = XDocument.Load(result).Root; var failed = (int?)run?.Attribute("failed") ?? -1; var status = (string)run?.Attribute("result");
                var acceptableStatus = string.Equals(status, "Passed", StringComparison.Ordinal)
                    || string.Equals(status, "Skipped:Ignored", StringComparison.Ordinal);
                if (failed != 0 || !acceptableStatus) throw new InvalidOperationException($"Test gate is not green: {result} (result={status}, failed={failed})");
            }
            foreach (var scene in EditorBuildSettings.scenes) if (scene.enabled && !File.Exists(scene.path)) throw new InvalidOperationException("Enabled scene is missing: " + scene.path);
        }
    }
}
