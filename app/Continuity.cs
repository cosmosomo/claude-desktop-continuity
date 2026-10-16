using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaudeSessionImporter
{
    public sealed class Account
    {
        public string name { get; set; }
        public string email { get; set; }
        public string health { get; set; }
        public bool isCurrent { get; set; }
        public string BaseDisplay
        {
            get { return (String.IsNullOrWhiteSpace(email) ? name + "（邮箱未记录）" : email) + (health == "usable" ? "" : "（需重新登录：" + health + "）"); }
        }
        public string Display
        {
            get { return BaseDisplay + (isCurrent ? " · [当前在线]" : ""); }
        }
    }
    public sealed class AccountState
    {
        public string current { get; set; }
        public List<Account> accounts { get; set; }
    }
    public sealed class Continuity
    {
        private readonly Settings settings;
        private readonly EventLog log;
        private readonly Catalog catalog;
        public Continuity(Settings settings, EventLog log, Catalog catalog)
        { this.settings = settings; this.log = log; this.catalog = catalog; }

        public async Task<AccountState> ReadAccounts()
        {
            var state = Json.Serializer().Deserialize<AccountState>(await Run(new[] { "continuity-accounts" }, null));
            if (state == null || state.accounts == null || state.accounts.Any(account => String.IsNullOrWhiteSpace(account.name) || String.IsNullOrWhiteSpace(account.health)))
                throw new IOException("账号服务返回的数据不完整。");
            foreach (var account in state.accounts)
                account.isCurrent = !String.IsNullOrWhiteSpace(state.current) && String.Equals(account.name, state.current, StringComparison.Ordinal);
            return state;
        }
        public void OpenAccountManager()
        {
            log.Write("account_manager_started", "launching bundled upstream tray");
            Process.Start(new ProcessStartInfo { FileName = settings.switcherGuiPath, WorkingDirectory = Path.GetDirectoryName(settings.switcherGuiPath), UseShellExecute = false });
            log.Write("account_manager_launched", "use tray Add account or Backup");
        }
        public void ValidateAccountTarget(Account target)
        {
            if (catalog.CurrentProvider() != "claude-desktop-official") throw new IOException("先在 CC Switch 切回官方 Desktop 并重启；账号切换不会修改 Gateway 配置。");
            if (target == null || String.IsNullOrWhiteSpace(target.name)) throw new ArgumentException("请选择目标账号；没有账号时点击添加账号 / 备份。");
            if (target.health != "usable") throw new IOException("目标账号登录状态不可用，请先在账号管理器中重新登录。");
        }
        public void ValidatePlan(List<Session> selected, Account target)
        {
            catalog.ValidateSelection(selected);
            ValidateAccountTarget(target);
        }
        public async Task SwitchOnly(Account target, IProgress<string> progress)
        {
            ValidateAccountTarget(target);
            var before = await ReadAccounts();
            if (!before.accounts.Any(account => account.name == target.name && account.health == "usable"))
                throw new IOException("目标账号状态已变化，请刷新列表。");
            log.Write("switch_only_started", "target=" + target.name);
            await Run(new[] { "use", "--", target.name }, progress);
            var after = await ReadAccounts();
            if (after.current != target.name)
                throw new IOException("Desktop 恢复后的账号不等于所选账号。");
            log.Write("switch_only_finished", "target=" + target.name);
        }
        public async Task SwitchAndOpen(List<Session> selected, Account target, IProgress<string> progress)
        {
            ValidatePlan(selected, target);
            var before = await ReadAccounts();
            if (!before.accounts.Any(account => account.name == target.name && account.health == "usable")) throw new IOException("目标账号状态已变化，请刷新列表。");
            log.Write("switch_continue_started", "sessions=" + selected.Count);
            await Run(new[] { "use", "--", target.name }, progress);
            var timer = Stopwatch.StartNew(); DesktopIdentity identity = null; string pendingReason = null;
            while (timer.Elapsed.TotalSeconds < Math.Min(3, settings.importTimeoutSeconds))
            {
                if (catalog.CurrentProvider() != "claude-desktop-official") throw new IOException("切换期间 Desktop 模式变化，未发送会话导入链接。");
                var state = await ReadAccounts();
                if (state.current != target.name) throw new IOException("Desktop 恢复后的账号不等于所选账号，未发送会话导入链接。");
                identity = catalog.TryResolveIdentity("claude-desktop-official", out pendingReason);
                if (identity != null) break;
                log.Write("desktop_initialization_pending", pendingReason ?? "pending");
                progress.Report("账号已恢复，正在准备唤醒 Code 会话（" + (int)timer.Elapsed.TotalSeconds + " 秒）…");
                await Task.Delay(settings.pollIntervalMs);
            }
            await catalog.OpenSelected(selected, "claude-desktop-official", identity, progress);
            var finalIdentity = catalog.OfficialIdentity ?? identity;
            log.Write("switch_continue_finished", "sessions=" + selected.Count + ";account=" + (finalIdentity == null ? target.name : finalIdentity.AccountId));
        }
        public async Task<string> Run(string[] arguments, IProgress<string> progress)
        {
            log.Write("switcher_call_started", "command=" + arguments[0]);
            var start = new ProcessStartInfo { FileName = settings.switcherCliPath,
                Arguments = String.Join(" ", arguments.Select(QuoteArgument)), WorkingDirectory = Path.GetDirectoryName(settings.switcherCliPath),
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = new UTF8Encoding(false), StandardErrorEncoding = new UTF8Encoding(false) };
            using (var process = new Process { StartInfo = start })
            {
                if (!process.Start()) throw new IOException("无法启动账号服务。");
                var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); var timer = Stopwatch.StartNew();
                while (!process.HasExited)
                {
                    if (timer.Elapsed.TotalSeconds > settings.switchTimeoutSeconds)
                    {
                        process.Kill();
                        if (!process.WaitForExit(5000)) throw new TimeoutException("账号服务超时且未能停止，请检查 Desktop 和账号管理器状态。");
                        log.Write("switcher_call_timeout", "command=" + arguments[0]);
                        throw new TimeoutException("账号服务超时，操作已中止。请检查 Desktop 当前账号后刷新；不要重复点击。");
                    }
                    if (progress != null) progress.Report("正在恢复账号并重启 Desktop（" + (int)timer.Elapsed.TotalSeconds + " 秒）…");
                    await Task.Delay(settings.pollIntervalMs);
                }
                string stdout = await output, stderr = await error;
                if (process.ExitCode != 0)
                {
                    log.Write("switcher_call_failed", "command=" + arguments[0] + ";exit=" + process.ExitCode + ";" + stderr);
                    throw new IOException("账号服务失败：" + stderr.Trim());
                }
                log.Write("switcher_call_finished", "command=" + arguments[0] + ";exit=0"); return stdout;
            }
        }
        public static string QuoteArgument(string value)
        {
            if (value == null) throw new ArgumentNullException("value");
            var quoted = new StringBuilder("\""); int slashes = 0;
            foreach (char character in value)
            {
                if (character == '\\') { slashes++; continue; }
                if (character == '"') { quoted.Append('\\', slashes * 2 + 1); quoted.Append('"'); }
                else { quoted.Append('\\', slashes); quoted.Append(character); }
                slashes = 0;
            }
            quoted.Append('\\', slashes * 2); quoted.Append('"'); return quoted.ToString();
        }
    }
}
