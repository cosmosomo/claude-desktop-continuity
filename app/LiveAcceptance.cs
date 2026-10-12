using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClaudeSessionImporter;

// 会重启真实 Desktop，只能在用户明确允许后手动执行；不属于 CI 或普通 Build。
public static class LiveAcceptance
{
    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string message) { Console.WriteLine(message); }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new IOException(message); Console.WriteLine("PASS: " + message); }
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        try
        {
            if (args.Length != 1) throw new ArgumentException("An absolute local config path is required. This test restarts Desktop.");
            Run(Settings.Load(args[0])).GetAwaiter().GetResult(); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static async Task Run(Settings settings)
    {
        var log = new ClaudeSessionImporter.EventLog(settings.logPath);
        var catalog = new Catalog(settings, log); var continuity = new Continuity(settings, log, catalog);
        var state = await continuity.ReadAccounts();
        var original = state.accounts.Single(account => account.name == state.current && account.health == "usable");
        var alternate = state.accounts.First(account => account.name != original.name && account.health == "usable");
        Require(catalog.CurrentProvider() == "claude-desktop-official", "Official Desktop provider is active");
        var originalIdentity = catalog.ResolveIdentity("claude-desktop-official");
        var session = catalog.Scan().First(candidate => candidate.OfficialRegistered && Directory.Exists(candidate.Cwd) &&
            catalog.ReadPreview(candidate, CancellationToken.None).Count > 0);
        var selection = new List<Session> { session };
        continuity.ValidatePlan(selection, alternate);
        var before = catalog.ReadPreview(session, CancellationToken.None);
        Console.WriteLine("Live cycle uses the most recently modified registered transcript: " + session.Id.Substring(0, 8));
        log.Write("live_acceptance_started", "session=" + session.Id + ";return_profile=" + original.name);
        bool switched = false; Exception cycleError = null;
        try
        {
            // 标记在调用之前，部分成功或超时也必须尝试恢复原账号。
            switched = true;
            await continuity.SwitchAndOpen(selection, alternate, new ConsoleProgress());
            var alternateState = await continuity.ReadAccounts();
            Require(alternateState.current == alternate.name, "Alternate saved account restored");
            var alternateIdentity = catalog.ResolveIdentity("claude-desktop-official");
            Require(!alternateIdentity.SameAs(originalIdentity), "Desktop initialized a different account namespace");
            Require(catalog.Scan().Single(candidate => candidate.Id == session.Id).OfficialRegistered,
                "Selected transcript registered in alternate Desktop namespace");
            var after = catalog.ReadPreview(session, CancellationToken.None);
            Require(before.Count == after.Count && before.Zip(after, (a, b) => a.Role == b.Role && a.Timestamp == b.Timestamp && a.Text == b.Text).All(value => value),
                "Recent conversation content preserved across account adoption");
        }
        catch (Exception ex)
        {
            cycleError = ex; log.Write("live_acceptance_failed", ex.ToString());
            Console.Error.WriteLine("Cycle failed; restoring original account: " + ex.Message);
        }
        if (switched)
        {
            Console.WriteLine("Returning to original account and reopening the selected session...");
            await continuity.SwitchAndOpen(selection, original, new ConsoleProgress());
            Require((await continuity.ReadAccounts()).current == original.name, "Original saved account restored");
            Require(catalog.ResolveIdentity("claude-desktop-official").SameAs(originalIdentity), "Original Desktop identity restored");
            Require(catalog.Scan().Single(candidate => candidate.Id == session.Id).OfficialRegistered,
                "Selected transcript registered after return to original account");
        }
        if (cycleError != null) throw new IOException("Live cycle failed; original account has been restored.", cycleError);
        log.Write("live_acceptance_finished", "session=" + session.Id + ";cycle=A-B-A;inference=false");
        Console.WriteLine("RESULT: live A-to-B-to-A account restore and native registration passed; no model request sent. Visible history rendering and inference are separate checks.");
    }
}
