using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClaudeSessionImporter;

public static class LocalIntegrationTests
{
    private static int passed;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("FAIL: " + name); passed++; Console.WriteLine("PASS: " + name); }
    private static T Field<T>(Picker picker, string name)
    { return (T)typeof(Picker).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(picker); }
    private static void Wait(Task task)
    {
        var timer = Stopwatch.StartNew();
        while (!task.IsCompleted)
        {
            if (timer.Elapsed.TotalSeconds > 60) throw new TimeoutException("Component operation exceeded 60 seconds.");
            Application.DoEvents(); Thread.Sleep(10);
        }
        task.GetAwaiter().GetResult(); Application.DoEvents();
    }
    private static string Digest(string path)
    {
        using (var sha = SHA256.Create())
        using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            return BitConverter.ToString(sha.ComputeHash(input));
    }
    private static void WaitPreview(Picker picker)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed.TotalSeconds < 60)
        {
            var pending = Field<Task>(picker, "previewTask"); Wait(pending);
            var current = Field<Task>(picker, "previewTask");
            var heading = Field<Label>(picker, "previewHeading").Text;
            if (Object.ReferenceEquals(pending, current) && current.IsCompleted && heading.StartsWith("最近对话 ·")) return;
            if (heading.StartsWith("预览读取失败")) throw new IOException("Preview failed: " + heading);
            Application.DoEvents(); Thread.Sleep(10);
        }
        throw new TimeoutException("The selected preview did not settle within 60 seconds.");
    }
    [STAThread]
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            if (args.Length != 1) throw new ArgumentException("An absolute local config path is required.");
            var settings = Settings.Load(args[0]);
            var log = new ClaudeSessionImporter.EventLog(settings.logPath);
            var catalog = new Catalog(settings, log); var continuity = new Continuity(settings, log, catalog);
            var accountTask = continuity.ReadAccounts(); Wait(accountTask); var state = accountTask.Result;
            Check(state.accounts.Count > 0, "read real saved accounts through credential-free JSON protocol");
            Check(state.accounts.Select(account => account.name).Distinct().Count() == state.accounts.Count, "account identities have unique names");
            var sessions = catalog.Scan(); Check(sessions.Count > 1, "discover real local transcripts without a predefined session ID");
            Check(Catalog.EnumerateLogCandidates(settings.officialLogPath).Any(), "enumerate primary and rotated desktop log files");
            Check(catalog.OfficialIdentity != null && Catalog.IsSessionId(catalog.OfficialIdentity.OrgId), "resolve desktop identity across log rotation and disk session folders");
            Check(sessions.Zip(sessions.Skip(1), (a, b) => a.ModifiedUtc >= b.ModifiedUtc).All(value => value), "sort recent modifications descending");
            Check(sessions.All(session => !session.Path.Contains("\\subagents\\")), "exclude subagent JSONLs");
            var sessionWithText = sessions.Where(session => session.ModifiedUtc < DateTime.UtcNow.AddHours(-1) && Directory.Exists(session.Cwd))
                .Last(session => catalog.ReadPreview(session, CancellationToken.None).Count > 0);
            string before = Digest(sessionWithText.Path);
            var messages = catalog.ReadPreview(sessionWithText, CancellationToken.None);
            Check(messages.Count <= settings.previewMessageCount && messages.All(message => message.Text.Length <= settings.previewCharsPerMessage), "bound actual conversation previews");
            Check(messages.All(message => message.Role == "user" || message.Role == "assistant"), "preview ordinary dialogue only");
            var target = state.accounts.First(account => account.health == "usable");
            continuity.ValidatePlan(new List<Session> { sessionWithText }, target);
            Check(true, "preflight actual account and transcript before switching");
            bool rejected = false;
            try { continuity.ValidatePlan(new List<Session>(), target); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "empty selection fails before any account mutation");
            rejected = false;
            try { continuity.ValidatePlan(new List<Session> { sessionWithText }, null); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "missing target fails before switching");
            var dryRun = continuity.Run(new[] { "use", "--dry-run", "--", target.name }, null); Wait(dryRun);
            Check(dryRun.Result.Contains("profile: " + target.name), "real CLI preserves profile arguments including Unicode");
            Check(!dryRun.Result.Contains("oauth:") && !dryRun.Result.Contains("sessionKey"), "diagnostic output has no authentication values");
            using (var picker = new Picker(settings, false))
            {
                // 隐藏的测试窗口不会像真实 Shown 一样创建子控件句柄；读取 TextLength 时才创建会重新触发行变化并清空旧预览。
                IntPtr windowHandle = picker.Handle;
                IntPtr gridHandle = Field<DataGridView>(picker, "grid").Handle;
                IntPtr previewHandle = Field<RichTextBox>(picker, "preview").Handle;
                Check(picker.IsHandleCreated && Field<DataGridView>(picker, "grid").IsHandleCreated && Field<RichTextBox>(picker, "preview").IsHandleCreated,
                    "create real window and child handles before asynchronous UI events");
                Wait((Task)typeof(Picker).GetMethod("Reload", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, null));
                var grid = Field<DataGridView>(picker, "grid"); var search = Field<TextBox>(picker, "search");
                var accounts = Field<ComboBox>(picker, "accounts"); var selected = Field<Dictionary<string, Session>>(picker, "checkedSessions");
                Check(accounts.Items.Count == state.accounts.Count, "unified window loads saved account choices");
                var selectionTime = DateTimeOffset.UtcNow;
                Check(grid.Rows.Count > 1 && selected.Values.All(session => session.ConversationAt.HasValue &&
                    session.ConversationAt.Value >= selectionTime.AddHours(-settings.activeSessionHours).AddSeconds(-60) && session.ConversationAt.Value <= selectionTime),
                    "default selection uses actual recent dialogue timestamps");
                Check(grid.Rows.Cast<DataGridViewRow>().Select(row => (Session)row.Tag).Where(session => session.ConversationAt.HasValue &&
                    session.ConversationAt.Value >= selectionTime.AddHours(-settings.activeSessionHours).AddSeconds(60) && session.ConversationAt.Value <= selectionTime)
                    .All(session => selected.ContainsKey(session.Path)), "default selection includes real active sessions");
                foreach (DataGridViewRow row in grid.Rows) row.Cells["Choose"].Value = false;
                Check(selected.Count == 0, "user can clear all automatically selected sessions");
                search.Text = sessionWithText.Id;
                Check(grid.Rows.Count == 1, "search identifies a real transcript");
                WaitPreview(picker);
                Check(Field<Label>(picker, "previewHeading").Text.Contains(sessionWithText.Id.Substring(0, 8)), "preview belongs to highlighted transcript");
                var renderedPreview = Field<RichTextBox>(picker, "preview");
                var expectedMessage = messages.Last().Text.Replace("\r\n", "\n");
                var currentMessage = catalog.ReadPreview(sessionWithText, CancellationToken.None).Last().Text.Replace("\r\n", "\n");
                Check(currentMessage == expectedMessage, "preview assertion uses an unchanged real transcript snapshot");
                Check(renderedPreview.Text.Replace("\r\n", "\n").Contains(expectedMessage), "preview includes actual last message");
                grid.Rows[0].Cells["Choose"].Value = true; search.Text = "";
                Check(selected.Count == 1, "selection survives filtering");
                grid.CurrentCell = grid.Rows[1].Cells["Title"]; WaitPreview(picker);
                Check(Field<Label>(picker, "previewHeading").Text.Contains(((Session)grid.Rows[1].Tag).Id.Substring(0, 8)), "row change cannot display previous row's preview");
                Check(grid.Columns.Contains("Input") && Field<Button>(picker, "switchOpen").Enabled, "request size and switch-and-continue action are present");
                continuity.ValidateAccountTarget(target);
                Check(true, "pure account switch target validation succeeds without sessions");
                typeof(Picker).GetMethod("SelectOnlyPreviewed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, null);
                Check(selected.Count == 1 && selected.ContainsKey(((Session)grid.Rows[1].Tag).Path), "select-only-previewed isolates current row");
                typeof(Picker).GetMethod("SelectUnregisteredOnly", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, null);
                Check(selected.Values.All(s => s.CwdExists && !s.IsRegisteredFor(Field<string>(picker, "provider"))), "select-only-unregistered filters out already registered sessions");
                typeof(Picker).GetMethod("ClearAllSelection", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(picker, null);
                Check(selected.Count == 0 && Field<Button>(picker, "open").Text == "打开当前预览会话" && Field<Button>(picker, "switchOpen").Enabled, "zero selection adapts button labels for preview-open or pure account switch");
            }
            Check(Digest(sessionWithText.Path) == before, "read-only testing leaves original transcript byte-identical");
            Console.WriteLine("RESULT: " + passed + " checks passed; no live switch, import or model request performed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
