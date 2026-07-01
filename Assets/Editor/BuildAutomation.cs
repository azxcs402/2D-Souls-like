using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildAutomation
{
    private const string BuildRoot = "Builds/Windows64";
    private const string BuildName = "2D-Souls-like.exe";

    [MenuItem("Tools/2D Souls-like/Build Windows x64")]
    public static void BuildWindows64()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new System.InvalidOperationException("No enabled scenes found in Build Settings.");
        }

        string buildDirectory = Path.GetFullPath(BuildRoot);
        if (Directory.Exists(buildDirectory))
        {
            Directory.Delete(buildDirectory, true);
        }

        Directory.CreateDirectory(buildDirectory);

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(buildDirectory, BuildName),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CleanBuildCache
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception($"Build failed: {report.summary.result}.");
        }

        Debug.Log($"Build succeeded: {buildOptions.locationPathName}");
    }

    public static void BuildWindows64CommandLine()
    {
        BuildWindows64();
    }
}
