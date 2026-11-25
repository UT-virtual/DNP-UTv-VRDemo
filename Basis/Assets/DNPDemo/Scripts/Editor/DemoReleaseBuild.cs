using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DNPDemo.Editor
{
    /// <summary>
    /// 展示で使うリリース APK をビルドする。
    /// プロジェクトのセットアップを適用し、Development Build を外して、リポジトリ直下の Builds/Release に出力する。
    /// APK のファイル名と versionName には、ビルドした日時とコミットを入れる。
    /// メニューから実行したときは、続けてつないでいる Quest にインストールする（<see cref="DemoQuestInstaller"/>）。
    /// バッチモードではビルドだけ行う:
    /// Unity.exe -batchmode -quit -projectPath Basis -buildTarget Android -executeMethod DNPDemo.Editor.DemoReleaseBuild.BuildFromCommandLine -logFile Logs/release-build.log
    /// </summary>
    public static class DemoReleaseBuild
    {
        const string DialogTitle = "Release APK";

        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [MenuItem("DNP Demo/Build/Release APK")]
        public static void BuildFromMenu()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog(DialogTitle, "Build Profiles でプラットフォームを Android に切り替えてから実行してください。", "OK");
                return;
            }

            GitState git = GitState.Read(RepoRoot);
            if (git.ChangedFileCount > 0)
            {
                string message = $"コミットしていない変更が {git.ChangedFileCount} 件あります。"
                    + $"APK にはこれらの変更も含まれますが、名前にはコミット {git.ShortCommit} が入ります。\n\nビルドを続けますか？";
                if (!EditorUtility.DisplayDialog(DialogTitle, message, "ビルドする", "やめる"))
                {
                    return;
                }
            }

            if (Build(git, out string apkPath))
            {
                DemoQuestInstaller.InstallAsync(RepoRoot, apkPath);
            }
            else
            {
                EditorUtility.DisplayDialog(DialogTitle, "ビルドに失敗しました。Console のエラーを確認してください。", "OK");
            }
        }

        public static void BuildFromCommandLine()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("[DNPDemo] Release APK: -buildTarget Android を付けて実行してください。");
                EditorApplication.Exit(1);
                return;
            }

            GitState git = GitState.Read(RepoRoot);
            if (git.ChangedFileCount > 0)
            {
                Debug.LogWarning($"[DNPDemo] Release APK: コミットしていない変更が {git.ChangedFileCount} 件あります。APK にはこれらの変更も含まれます。");
            }

            EditorApplication.Exit(Build(git, out _) ? 0 : 1);
        }

        static bool Build(GitState git, out string apkPath)
        {
            DNPDemoProjectSetup.RunAll();
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            string label = $"{DateTime.Now:yyyyMMdd-HHmm}-{git.ShortCommit}";
            apkPath = Path.Combine(RepoRoot, "Builds", "Release", $"DNPDemo_{label}.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(apkPath));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report;
            DemoBuildVersionPolicy.ReleaseVersionName = label;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                // versionName を ProjectSettings.asset に残さないよう、固定値に戻す。
                DemoBuildVersionPolicy.ReleaseVersionName = null;
                DemoBuildVersionPolicy.Apply();
                AssetDatabase.SaveAssets();
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[DNPDemo] Release APK: ビルドに失敗しました（{report.summary.result}、エラー {report.summary.totalErrors} 件）。");
                return false;
            }

            Debug.Log($"[DNPDemo] Release APK: {apkPath}（versionName {label}）");
            return true;
        }

        readonly struct GitState
        {
            public readonly string ShortCommit;
            public readonly int ChangedFileCount;

            GitState(string shortCommit, int changedFileCount)
            {
                ShortCommit = shortCommit;
                ChangedFileCount = changedFileCount;
            }

            public static GitState Read(string repoRoot)
            {
                string commit = RunGit(repoRoot, "rev-parse --short=7 HEAD")?.Trim();
                string status = RunGit(repoRoot, "status --porcelain --untracked-files=no");
                int changed = string.IsNullOrEmpty(status)
                    ? 0
                    : status.Split('\n').Count(line => line.Trim().Length > 0);
                return new GitState(string.IsNullOrEmpty(commit) ? "nogit" : commit, changed);
            }

            static string RunGit(string workingDirectory, string arguments)
            {
                try
                {
                    var startInfo = new ProcessStartInfo("git", arguments)
                    {
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    using Process process = Process.Start(startInfo);
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode == 0 ? output : null;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[DNPDemo] Release APK: git を実行できませんでした（{exception.Message}）。");
                    return null;
                }
            }
        }
    }
}
