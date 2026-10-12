using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

// 用户下载后直接双击根目录 EXE；环境与配置仍由唯一 Setup 流程生成。
public static class Launcher
{
    private static string diagnosticPath;
    [STAThread]
    public static int Main(string[] arguments)
    {
        Application.EnableVisualStyles();
        try
        {
            if (arguments.Length != 0) throw new ArgumentException("请直接打开 Continuity.exe，无需命令参数。");
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string application = Path.Combine(root, "dist", "ClaudeDesktopContinuity.exe");
            string configuration = Path.Combine(root, "config.local.json");
            string setup = Path.Combine(root, "scripts", "continuity", "Setup.ps1");
            if (!File.Exists(application) || !File.Exists(setup)) throw new IOException("软件目录不完整。请完整解压 ZIP 后再打开 Continuity.exe，不能只移动 EXE 文件。");
            if (!File.Exists(configuration))
            {
                string eventPath = Path.Combine(root, "launcher.events.jsonl"); diagnosticPath = eventPath;
                File.AppendAllText(eventPath, "{\"action\":\"setup_started\",\"at\":\"" + DateTimeOffset.UtcNow.ToString("o") + "\"}\n", new UTF8Encoding(false));
                var start = new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + setup + "\"", WorkingDirectory = root,
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using (var process = Process.Start(start))
                {
                    if (process == null) throw new IOException("无法启动首次配置程序。");
                    var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(90000)) { process.Kill(); throw new TimeoutException("首次配置超时，请检查 Desktop 是否已安装并登录。"); }
                    string stderr = errors.GetAwaiter().GetResult(); output.GetAwaiter().GetResult();
                    if (process.ExitCode != 0) throw new IOException("首次配置失败：" + stderr);
                }
                File.AppendAllText(eventPath, "{\"action\":\"setup_finished\",\"at\":\"" + DateTimeOffset.UtcNow.ToString("o") + "\"}\n", new UTF8Encoding(false));
            }
            if (!File.Exists(configuration)) throw new IOException("首次配置未生成 config.local.json。");
            Process.Start(new ProcessStartInfo { FileName = application, Arguments = "\"" + configuration + "\"", WorkingDirectory = Path.GetDirectoryName(application), UseShellExecute = false });
            return 0;
        }
        catch (Exception ex)
        {
            string report = ex.Message;
            if (diagnosticPath != null)
            {
                try { File.AppendAllText(diagnosticPath, "{\"action\":\"setup_failed\",\"at\":\"" + DateTimeOffset.UtcNow.ToString("o") + "\",\"message\":\"" + ex.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "\"}\n", new UTF8Encoding(false)); }
                catch (Exception loggingError) { report += "\n诊断日志也无法写入：" + loggingError.Message; Console.Error.WriteLine(loggingError); }
            }
            MessageBox.Show(report, "Continuity · 启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1;
        }
    }
}
