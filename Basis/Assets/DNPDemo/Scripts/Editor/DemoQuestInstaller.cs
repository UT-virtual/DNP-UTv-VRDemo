using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace DNPDemo.Editor
{
    /// <summary>
    /// USB でつないだ Quest へ APK をインストールする。処理は tools/install-quest.ps1 に任せ、
    /// 手で実行したときと同じ動作にする。スクリプトはバックグラウンドで実行し、
    /// 出力を Console に流して、終わったら結果をダイアログで知らせる。
    /// </summary>
    public static class DemoQuestInstaller
    {
        const string DialogTitle = "Quest へのインストール";
        const string LogPrefix = "[DNPDemo] Install: ";

        // install-quest.ps1 の終了コード。
        const int ExitSucceeded = 0;
        const int ExitNoDevice = 2;

        static Process running;
        static string runningApkPath;
        static readonly ConcurrentQueue<string> pendingLines = new ConcurrentQueue<string>();

        public static void InstallAsync(string repoRoot, string apkPath)
        {
            if (running != null)
            {
                Debug.LogWarning(LogPrefix + "前のインストールがまだ終わっていません。");
                return;
            }

            string script = Path.Combine(repoRoot, "tools", "install-quest.ps1");
            string command = "[Console]::OutputEncoding = [Text.Encoding]::UTF8; "
                + $"& {Quote(script)} {Quote(apkPath)}; exit $LASTEXITCODE";
            var startInfo = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command}\"")
            {
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            try
            {
                running = new Process { StartInfo = startInfo };
                running.OutputDataReceived += (_, e) => Enqueue(e.Data);
                running.ErrorDataReceived += (_, e) => Enqueue(e.Data);
                running.Start();
                running.BeginOutputReadLine();
                running.BeginErrorReadLine();
                runningApkPath = apkPath;
            }
            catch (Exception exception)
            {
                running = null;
                Debug.LogError(LogPrefix + $"インストールスクリプトを起動できませんでした（{exception.Message}）。");
                return;
            }

            Debug.Log(LogPrefix + $"つないでいる Quest にインストールしています: {apkPath}");
            EditorApplication.update += Poll;
        }

        static string Quote(string path) => "'" + path.Replace("'", "''") + "'";

        static void Enqueue(string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                pendingLines.Enqueue(line);
            }
        }

        static void Poll()
        {
            FlushLines();
            if (!running.HasExited)
            {
                return;
            }

            // 終了後に届く残りの出力を読み切る。
            running.WaitForExit();
            FlushLines();
            int exitCode = running.ExitCode;
            running.Dispose();
            running = null;
            EditorApplication.update -= Poll;
            ReportResult(exitCode, runningApkPath);
        }

        static void FlushLines()
        {
            while (pendingLines.TryDequeue(out string line))
            {
                Debug.Log(LogPrefix + line);
            }
        }

        static void ReportResult(int exitCode, string apkPath)
        {
            switch (exitCode)
            {
                case ExitSucceeded:
                    Debug.Log(LogPrefix + "完了しました。");
                    EditorUtility.DisplayDialog(DialogTitle, "つないでいるすべての Quest にインストールしました。", "OK");
                    break;
                case ExitNoDevice:
                    Debug.LogWarning(LogPrefix + "Quest がつながっていないため、インストールしませんでした。");
                    EditorUtility.DisplayDialog(DialogTitle,
                        "Quest がつながっていないため、インストールしませんでした。\n"
                        + $"Quest をつないでから、次の APK を tools/install-quest.ps1 でインストールしてください。\n\n{apkPath}", "OK");
                    break;
                default:
                    Debug.LogError(LogPrefix + $"インストールに失敗しました（終了コード {exitCode}）。");
                    EditorUtility.DisplayDialog(DialogTitle, "インストールに失敗しました。Console のログを確認してください。", "OK");
                    break;
            }
        }
    }
}
