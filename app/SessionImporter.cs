using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ClaudeSessionImporter
{
    public sealed class Settings
    {
        public string projectsRoot { get; set; }
        public string officialDesktopRoot { get; set; }
        public string gatewayDesktopRoot { get; set; }
        public string officialLogPath { get; set; }
        public string gatewayLogPath { get; set; }
        public string ccSwitchSettingsPath { get; set; }
        public string initialProjectPath { get; set; }
        public int headBytes { get; set; }
        public int tailBytes { get; set; }
        public int previewBytes { get; set; }
        public int previewMessageCount { get; set; }
        public int previewCharsPerMessage { get; set; }
        public int maxSessions { get; set; }
        public int activeSessionHours { get; set; }
        public string timeZoneId { get; set; }
        public int importTimeoutSeconds { get; set; }
        public int pollIntervalMs { get; set; }
        public string logPath { get; set; }
        public string compilerPath { get; set; }
        public string shortcutPath { get; set; }
        public string desktopMode { get; set; }
        public string switcherCliPath { get; set; }
        public string switcherGuiPath { get; set; }
        public int switchTimeoutSeconds { get; set; }

        public static Settings Load(string path)
        {
            var settings = Json.Read<Settings>(path);
            settings.Validate();
            return settings;
        }

        public void Validate()
        {
            if (desktopMode != "official" && desktopMode != "ccswitch") throw new ArgumentException("desktopMode 必须为 official 或 ccswitch。");
            foreach (var path in new[] { projectsRoot, officialDesktopRoot })
                if (String.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !Directory.Exists(path))
                    throw new IOException("配置目录不存在或不是绝对路径：" + path);
            foreach (var path in new[] { compilerPath, officialLogPath, switcherCliPath, switcherGuiPath,
                Path.Combine(officialDesktopRoot, "config.json") })
                if (String.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !File.Exists(path))
                    throw new IOException("配置文件不存在或不是绝对路径：" + path);
            if (desktopMode == "ccswitch" && (String.IsNullOrWhiteSpace(ccSwitchSettingsPath) || !File.Exists(ccSwitchSettingsPath)))
                throw new IOException("ccswitch 模式需要有效的 ccSwitchSettingsPath。");
            if (!String.IsNullOrWhiteSpace(gatewayDesktopRoot) && (!Directory.Exists(gatewayDesktopRoot) || !File.Exists(gatewayLogPath)))
                throw new IOException("已启用的 Gateway 路径不可用。");
            foreach (var path in new[] { logPath, shortcutPath })
                if (String.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !Directory.Exists(Path.GetDirectoryName(path)))
                    throw new IOException("输出文件的父目录不存在：" + path);
            if (headBytes < 1024 || tailBytes < 1024 || headBytes > 16777216 || tailBytes > 16777216 ||
                maxSessions < 1 || maxSessions > 2000 || activeSessionHours < 1 || activeSessionHours > 168 || importTimeoutSeconds < 1 || importTimeoutSeconds > 120 ||
                pollIntervalMs < 100 || pollIntervalMs > 5000 || switchTimeoutSeconds < 20 || switchTimeoutSeconds > 180 || previewBytes < 1024 || previewBytes > 16777216 ||
                previewMessageCount < 1 || previewMessageCount > 100 || previewCharsPerMessage < 100 || previewCharsPerMessage > 10000)
                throw new ArgumentException("扫描大小、会话数量或导入超时配置超出允许范围。");
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            foreach (var root in new[] { officialDesktopRoot })
                if (!Directory.Exists(Path.Combine(root, "claude-code-sessions")))
                    throw new IOException("Desktop 会话存储根目录不存在：" + root);
        }
    }

    public static class Json
    {
        public static JavaScriptSerializer Serializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = 33554432, RecursionLimit = 256 };
        }

        public static T Read<T>(string path)
        {
            try { return Serializer().Deserialize<T>(File.ReadAllText(path, new UTF8Encoding(false, true))); }
            catch (Exception ex) { throw new IOException("无法读取 JSON：" + path + "；" + ex.Message, ex); }
        }
    }

    public sealed class DesktopIndex
    {
        public string sessionId { get; set; }
        public string cliSessionId { get; set; }
        public string title { get; set; }
        public string cwd { get; set; }
        public bool isArchived { get; set; }
    }

    public sealed class DesktopIdentity
    {
        public string AccountId;
        public string OrgId;
        public string IndexRoot;

        public bool SameAs(DesktopIdentity other)
        {
            return other != null && String.Equals(AccountId, other.AccountId, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(OrgId, other.OrgId, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(IndexRoot, other.IndexRoot, StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class Session
    {
        public string Id;
        public string Path;
        public string ProjectFolder;
        public string Cwd;
        public string Title;
        public DateTime ModifiedUtc;
        public DateTimeOffset? ConversationAt;
        public bool OfficialRegistered;
        public bool GatewayRegistered;
        public DateTimeOffset? CompactAt;
        public long? CompactBefore;
        public long? CompactAfter;
        public DateTimeOffset? UsageAt;
        public long? InputTokens;
    }

    public sealed class EventLog
    {
        private readonly string path;
        private readonly object gate = new object();
        public EventLog(string path) { this.path = path; }
        public void Write(string action, string detail)
        {
            var record = new { at = DateTimeOffset.UtcNow.ToString("o"), action = action, detail = detail };
            lock (gate) File.AppendAllText(path, Json.Serializer().Serialize(record) + "\n", new UTF8Encoding(false));
        }
    }

    public sealed class ConversationMessage
    {
        public string Role;
        public DateTimeOffset Timestamp;
        public string Text;
        public bool Truncated;
    }

    public sealed class Catalog
    {
        private readonly Settings settings;
        private readonly EventLog log;
        public DesktopIdentity OfficialIdentity { get; private set; }
        public DesktopIdentity GatewayIdentity { get; private set; }
        public Catalog(Settings settings, EventLog log) { this.settings = settings; this.log = log; }

        public DesktopIdentity ResolveIdentity(string provider)
        {
            bool official = provider == "claude-desktop-official";
            if (!official && String.IsNullOrWhiteSpace(settings.gatewayDesktopRoot)) throw new IOException("当前为 Gateway 模式，但未配置 Gateway 数据路径。");
            string root = official ? settings.officialDesktopRoot : settings.gatewayDesktopRoot;
            string logPath = official ? settings.officialLogPath : settings.gatewayLogPath;
            var config = Json.Read<Dictionary<string, object>>(Path.Combine(root, "config.json"));
            object account;
            if (!config.TryGetValue("lastKnownAccountUuid", out account) || !(account is string) || !IsSessionId((string)account))
                throw new IOException("Desktop 尚未记录有效登录账号：" + root + "。请先登录并打开 Code 页面。");

            // 组织由 Desktop 自身的初始化日志确认，避免误选其他账号或旧组织目录。
            Match initialized = null;
            var pattern = new Regex(@"\[LocalSessionManager\] Initialization succeeded.*accountId=([0-9a-fA-F-]{36}), orgId=([0-9a-fA-F-]{36})(?:,|\s|$)");
            using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = pattern.Match(line);
                    if (match.Success) initialized = match;
                }
            }
            if (initialized == null || !IsSessionId(initialized.Groups[2].Value))
                throw new IOException("Desktop 日志中没有 Code 会话初始化记录：" + logPath + "。请先打开 Code 页面后刷新列表。");
            string activeAccount = initialized.Groups[1].Value;
            if (!String.Equals((string)account, activeAccount, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Desktop 登录账号与 Code 初始化账号尚未一致。请完成账号切换、打开 Code 页面后刷新列表。");
            return new DesktopIdentity { AccountId = activeAccount, OrgId = initialized.Groups[2].Value,
                IndexRoot = Path.Combine(root, "claude-code-sessions", activeAccount, initialized.Groups[2].Value) };
        }

        public static void EnsureIdentity(DesktopIdentity current, DesktopIdentity expected)
        {
            if (current == null || !current.SameAs(expected))
                throw new IOException("Desktop 登录账号或组织已变化，请刷新列表后重新选择会话。");
        }

        public Dictionary<string, DesktopIndex> ReadIndexes(string directory)
        {
            var indexes = new Dictionary<string, DesktopIndex>(StringComparer.OrdinalIgnoreCase);
            // 新账号已初始化但尚无会话时，Desktop 会在首次原生导入后创建这一派生目录。
            if (!Directory.Exists(directory))
            {
                if (!Directory.Exists(Path.GetDirectoryName(Path.GetDirectoryName(directory))))
                    throw new IOException("Desktop 会话存储根目录不存在：" + directory);
                log.Write("index_namespace_pending_creation", directory);
                return indexes;
            }
            foreach (var file in Directory.EnumerateFiles(directory, "local_*.json", SearchOption.TopDirectoryOnly))
            {
                var record = Json.Read<DesktopIndex>(file);
                if (record == null || String.IsNullOrWhiteSpace(record.cliSessionId))
                    throw new IOException("Desktop 索引缺少 cliSessionId：" + file);
                indexes[record.cliSessionId] = record;
            }
            return indexes;
        }

        public List<Session> Scan()
        {
            log.Write("scan_started", settings.projectsRoot);
            string provider = CurrentProvider();
            var active = ResolveIdentity(provider);
            OfficialIdentity = provider == "claude-desktop-official" ? active : null;
            GatewayIdentity = provider == "claude-desktop-official" ? null : active;
            log.Write("desktop_identity_resolved", "provider=" + provider + ";account=" + active.AccountId + ";org=" + active.OrgId);
            var indexes = ReadIndexes(active.IndexRoot);
            // 只枚举项目目录的顶层 JSONL，避免把子代理记录当作可独立导入的会话。
            var files = Directory.EnumerateDirectories(settings.projectsRoot)
                .SelectMany(folder => Directory.EnumerateFiles(folder, "*.jsonl", SearchOption.TopDirectoryOnly))
                .Where(path => IsSessionId(System.IO.Path.GetFileNameWithoutExtension(path)))
                .Select(path => new FileInfo(path)).OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(settings.maxSessions).ToList();
            var sessions = new List<Session>();
            foreach (var file in files)
            {
                var session = new Session { Id = System.IO.Path.GetFileNameWithoutExtension(file.Name), Path = file.FullName,
                    ProjectFolder = file.Directory.Name, ModifiedUtc = file.LastWriteTimeUtc };
                DesktopIndex index;
                if (indexes.TryGetValue(session.Id, out index)) { session.Title = index.title; session.Cwd = index.cwd;
                    session.OfficialRegistered = provider == "claude-desktop-official"; session.GatewayRegistered = !session.OfficialRegistered; }
                if (!String.IsNullOrWhiteSpace(session.Cwd)) session.Cwd = NormalizeCwd(session.Cwd);
                ReadMetadata(session);
                var titlePath = System.IO.Path.Combine(file.DirectoryName, session.Id, "custom-title.json");
                if (File.Exists(titlePath))
                {
                    var title = Json.Read<Dictionary<string, object>>(titlePath);
                    object custom;
                    if (!title.TryGetValue("customTitle", out custom) || !(custom is string))
                        throw new IOException("标题文件缺少 customTitle：" + titlePath);
                    session.Title = (string)custom;
                }
                sessions.Add(session);
            }
            log.Write("scan_finished", "sessions=" + sessions.Count);
            return sessions;
        }

        public static bool IsSessionId(string id)
        {
            Guid parsed;
            return Guid.TryParseExact(id, "D", out parsed);
        }

        public static string ResumeUri(string id)
        {
            if (!IsSessionId(id)) throw new ArgumentException("无效的会话 ID：" + id);
            return "claude://resume?session=" + id;
        }

        // 读取头尾的完整行；截断边缘不解析，完整行出现坏 JSON 则明确报错。
        public static List<string> ReadWindow(string path, int maxBytes, bool tail)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                long start = tail ? Math.Max(0, stream.Length - maxBytes) : 0;
                stream.Position = start;
                var bytes = new byte[(int)Math.Min(maxBytes, stream.Length - start)];
                int length = 0, read;
                while (length < bytes.Length && (read = stream.Read(bytes, length, bytes.Length - length)) > 0) length += read;
                int begin = 0;
                if (start > 0) { while (begin < length && bytes[begin] != 10) begin++; begin++; }
                int end = length - 1;
                while (end >= begin && bytes[end] != 10) end--;
                if (end < begin) return new List<string>();
                string text = new UTF8Encoding(false, true).GetString(bytes, begin, end - begin + 1);
                return text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.TrimEnd('\r')).ToList();
            }
        }

        private void ReadMetadata(Session session)
        {
            var lines = ReadWindow(session.Path, settings.headBytes, false);
            lines.AddRange(ReadWindow(session.Path, settings.tailBytes, true));
            foreach (var line in lines)
            {
                var row = ParseRow(line, session.Path);
                ReadContextMetadata(session, row);
                object value;
                // 已登记会话以 Desktop 绑定的目录为准；历史行可能含临时切换的 cwd。
                if (String.IsNullOrWhiteSpace(session.Cwd) && row.TryGetValue("cwd", out value) && value is string && !String.IsNullOrWhiteSpace((string)value))
                    session.Cwd = NormalizeCwd((string)value);
                if (String.IsNullOrWhiteSpace(session.Title) && row.TryGetValue("customTitle", out value) && value is string) session.Title = (string)value;
                var message = ReadConversationMessage(row, session.Path);
                if (message == null) continue;
                if (String.IsNullOrWhiteSpace(session.Title) && message.Role == "user")
                    session.Title = message.Text.Length > 100 ? message.Text.Substring(0, 100) : message.Text;
                if (!session.ConversationAt.HasValue || message.Timestamp > session.ConversationAt.Value) session.ConversationAt = message.Timestamp;
            }
        }

        private static Dictionary<string, object> ParseRow(string line, string path)
        {
            try
            {
                var row = Json.Serializer().Deserialize<Dictionary<string, object>>(line);
                if (row == null) throw new IOException("JSONL 行不是消息对象。");
                return row;
            }
            catch (Exception ex) { throw new IOException("会话包含无效的完整 JSONL 行：" + path + "；" + ex.Message, ex); }
        }

        private static void ReadContextMetadata(Session session, Dictionary<string, object> row)
        {
            object stamp, subtype, metadata;
            DateTimeOffset at;
            if (!row.TryGetValue("timestamp", out stamp) || !(stamp is string) ||
                !DateTimeOffset.TryParse((string)stamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out at)) return;
            if (row.TryGetValue("subtype", out subtype) && Equals(subtype, "compact_boundary") &&
                row.TryGetValue("compactMetadata", out metadata) && metadata is Dictionary<string, object> &&
                (!session.CompactAt.HasValue || at > session.CompactAt.Value))
            {
                var compact = (Dictionary<string, object>)metadata;
                session.CompactAt = at; session.CompactBefore = Number(compact, "preTokens"); session.CompactAfter = Number(compact, "postTokens");
            }
            object messageValue, usageValue, model;
            if (!row.TryGetValue("message", out messageValue) || !(messageValue is Dictionary<string, object>)) return;
            var message = (Dictionary<string, object>)messageValue;
            if (message.TryGetValue("model", out model) && Equals(model, "<synthetic>")) return;
            if (!message.TryGetValue("usage", out usageValue) || !(usageValue is Dictionary<string, object>) ||
                session.UsageAt.HasValue && at <= session.UsageAt.Value) return;
            var usage = (Dictionary<string, object>)usageValue;
            long? input = Number(usage, "input_tokens"), read = Number(usage, "cache_read_input_tokens"), create = Number(usage, "cache_creation_input_tokens");
            if (!input.HasValue || !read.HasValue || !create.HasValue) return;
            session.UsageAt = at; session.InputTokens = input.Value + read.Value + create.Value;
        }

        private static long? Number(Dictionary<string, object> row, string key)
        {
            object value;
            return row.TryGetValue(key, out value) && value != null ? (long?)Convert.ToInt64(value, CultureInfo.InvariantCulture) : null;
        }

        private static ConversationMessage ReadConversationMessage(Dictionary<string, object> row, string path)
        {
            object type, value, content;
            if (!row.TryGetValue("type", out type) || (!Equals(type, "user") && !Equals(type, "assistant"))) return null;
            if (row.TryGetValue("isMeta", out value) && Equals(value, true)) return null;
            var message = row.ContainsKey("message") ? row["message"] as Dictionary<string, object> : null;
            if (message == null || (message.TryGetValue("model", out value) && Equals(value, "<synthetic>"))) return null;
            if (!message.TryGetValue("content", out content)) return null;
            var text = TextContent(content).Trim();
            if (text.Length == 0 || text.StartsWith("<task-notification") || text.StartsWith("<command") || text.StartsWith("<local-command")) return null;
            if (!row.TryGetValue("timestamp", out value) || !(value is string)) throw new IOException("对话消息缺少 timestamp：" + path);
            DateTimeOffset stamp;
            if (!DateTimeOffset.TryParse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out stamp))
                throw new IOException("对话时间格式无效：" + path);
            return new ConversationMessage { Role = (string)type, Timestamp = stamp, Text = text };
        }

        public List<ConversationMessage> ReadPreview(Session session, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            ValidateTranscript(session);
            log.Write("preview_started", "id=" + session.Id);
            var messages = new Queue<ConversationMessage>();
            foreach (var line in ReadWindow(session.Path, settings.previewBytes, true))
            {
                cancellation.ThrowIfCancellationRequested();
                var message = ReadConversationMessage(ParseRow(line, session.Path), session.Path);
                if (message == null) continue;
                if (message.Text.Length > settings.previewCharsPerMessage)
                {
                    int length = settings.previewCharsPerMessage;
                    if (Char.IsHighSurrogate(message.Text[length - 1])) length--;
                    message.Text = message.Text.Substring(0, length); message.Truncated = true;
                }
                messages.Enqueue(message);
                if (messages.Count > settings.previewMessageCount) messages.Dequeue();
            }
            log.Write("preview_finished", "id=" + session.Id + ";messages=" + messages.Count);
            return messages.ToList();
        }

        private static string TextContent(object content)
        {
            var text = content as string;
            if (text != null) return text;
            var blocks = content as IEnumerable;
            if (blocks == null) return String.Empty;
            var parts = new List<string>();
            foreach (var block in blocks)
            {
                var fields = block as Dictionary<string, object>;
                object kind, value;
                if (fields != null && fields.TryGetValue("type", out kind) && Equals(kind, "text") && fields.TryGetValue("text", out value) && value is string)
                    parts.Add((string)value);
            }
            return String.Join("\n", parts);
        }

        private static string NormalizeCwd(string path)
        {
            if ((path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '/' || path[2] == '\\')) || path.StartsWith("\\\\"))
                return System.IO.Path.GetFullPath(path).TrimEnd('\\');
            return path;
        }

        public string CurrentProvider()
        {
            if (settings.desktopMode == "official") return "claude-desktop-official";
            var settingsRow = Json.Read<Dictionary<string, object>>(settings.ccSwitchSettingsPath);
            object provider;
            if (!settingsRow.TryGetValue("currentProviderClaudeDesktop", out provider) || !(provider is string) || String.IsNullOrWhiteSpace((string)provider))
                throw new IOException("CC Switch 未配置 Claude Desktop 的当前服务商。");
            return (string)provider;
        }

        public void ValidateSelection(List<Session> selected)
        {
            if (selected.Count == 0) throw new ArgumentException("请先勾选要导入的会话。");
            if (selected.Select(session => session.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != selected.Count)
                throw new ArgumentException("选中的会话 ID 重复，无法通过 Desktop 原生入口区分。");
            foreach (var session in selected)
            {
                ValidateTranscript(session);
                if (String.IsNullOrWhiteSpace(session.Cwd) || !System.IO.Path.IsPathRooted(session.Cwd) || !Directory.Exists(session.Cwd))
                    throw new IOException("会话的工作目录不存在：" + session.Id + "；" + session.Cwd);
            }
        }

        private void ValidateTranscript(Session session)
        {
            ResumeUri(session.Id);
            var root = System.IO.Path.GetFullPath(settings.projectsRoot).TrimEnd('\\') + "\\";
            if (String.IsNullOrWhiteSpace(session.Path) || !System.IO.Path.IsPathRooted(session.Path) ||
                !System.IO.Path.GetFullPath(session.Path).StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(session.Path) || System.IO.Path.GetFileNameWithoutExtension(session.Path) != session.Id)
                throw new IOException("选中的记录不再对应原始本地会话：" + session.Id);
        }

        public async Task OpenSelected(List<Session> selected, string expectedProvider, DesktopIdentity expectedIdentity, IProgress<string> progress)
        {
            ValidateSelection(selected);
            foreach (var session in selected)
            {
                if (CurrentProvider() != expectedProvider) throw new IOException("CC Switch 的 Desktop 模式已变化，请刷新列表后重新选择。");
                var identity = ResolveIdentity(expectedProvider);
                EnsureIdentity(identity, expectedIdentity);
                string indexRoot = identity.IndexRoot;
                progress.Report("正在导入/打开：" + session.Title + " · " + session.Id.Substring(0, 8));
                log.Write("desktop_open_started", "id=" + session.Id + ";provider=" + expectedProvider +
                    ";account=" + identity.AccountId + ";org=" + identity.OrgId + ";indexRoot=" + indexRoot);
                Process.Start(new ProcessStartInfo { FileName = ResumeUri(session.Id), UseShellExecute = true, WorkingDirectory = session.Cwd });
                // 只发送一次原生链接；等待实际索引登记，超时明确失败，不改写 Desktop 内部数据。
                var timer = Stopwatch.StartNew();
                while (!ReadIndexes(indexRoot).ContainsKey(session.Id))
                {
                    if (timer.Elapsed.TotalSeconds >= settings.importTimeoutSeconds)
                        throw new TimeoutException("Desktop 未在 " + settings.importTimeoutSeconds + " 秒内登记会话 " + session.Id +
                            "（账号 " + identity.AccountId.Substring(0, 8) + "）。请检查 Desktop 的错误提示。日志：" + settings.logPath);
                    if (CurrentProvider() != expectedProvider) throw new IOException("等待登记期间 Desktop 模式发生变化，请检查当前 Desktop 配置。");
                    EnsureIdentity(ResolveIdentity(expectedProvider), expectedIdentity);
                    progress.Report("等待 Desktop 登记：" + session.Id.Substring(0, 8) + "（" + (int)timer.Elapsed.TotalSeconds + " 秒）");
                    await Task.Delay(settings.pollIntervalMs);
                }
                EnsureIdentity(ResolveIdentity(expectedProvider), expectedIdentity);
                var registered = ReadIndexes(indexRoot)[session.Id];
                if (expectedProvider == "claude-desktop-official") session.OfficialRegistered = true;
                else session.GatewayRegistered = true;
                log.Write("desktop_open_finished", "id=" + session.Id + ";registered=true;desktopSession=" + registered.sessionId +
                    ";account=" + identity.AccountId + ";org=" + identity.OrgId);
                await Task.Delay(settings.pollIntervalMs);
            }
        }
    }

    public sealed partial class Picker : Form
    {
        private readonly Settings settings;
        private readonly Catalog catalog;
        private readonly EventLog log;
        private readonly TimeZoneInfo zone;
        private readonly bool interactiveErrors;
        private readonly DataGridView grid = new DataGridView();
        private readonly ComboBox project = new ComboBox();
        private readonly TextBox search = new TextBox();
        private readonly Button refresh = new Button();
        private readonly Button open = new Button();
        private readonly ComboBox accounts = new ComboBox();
        private readonly Button switchOpen = new Button();
        private readonly Button manage = new Button();
        private readonly Continuity continuity;
        private readonly Label status = new Label();
        private readonly Label mode = new Label();
        private readonly Label previewHeading = new Label();
        private readonly RichTextBox preview = new RichTextBox();
        private List<Session> sessions = new List<Session>();
        private readonly Dictionary<string, Session> checkedSessions = new Dictionary<string, Session>(StringComparer.OrdinalIgnoreCase);
        private string provider;
        private DesktopIdentity identity;
        private bool busy;
        private bool rendering;
        private int previewVersion;
        private CancellationTokenSource previewCancellation;
        private Task previewTask = Task.FromResult(0);

        public Picker(Settings settings, bool interactiveErrors = true, bool automaticLoad = true)
        {
            this.interactiveErrors = interactiveErrors;
            this.settings = settings; log = new EventLog(settings.logPath); catalog = new Catalog(settings, log);
            continuity = new Continuity(settings, log, catalog);
            zone = TimeZoneInfo.FindSystemTimeZoneById(settings.timeZoneId);
            BuildLayout();
            // SelectionChanged 发生时 CurrentCell 仍可能是上一行，等当前单元格更新后再读预览。
            grid.CurrentCellChanged += delegate { if (!rendering && !busy) StartPreview(); };
            grid.CurrentCellDirtyStateChanged += delegate { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            grid.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args)
            {
                if (args.RowIndex < 0 || args.ColumnIndex != grid.Columns["Choose"].Index) return;
                var row = grid.Rows[args.RowIndex]; var session = row.Tag as Session;
                if (session == null) return;
                if (Equals(row.Cells["Choose"].Value, true)) checkedSessions[session.Path] = session;
                else checkedSessions.Remove(session.Path);
                UpdateSelection();
            };
            refresh.Click += async delegate { await Reload(); };
            project.SelectedIndexChanged += delegate { if (!busy) Render(); };
            search.TextChanged += delegate { if (!busy) Render(); };
            open.Click += async delegate { await Import(); };
            switchOpen.Click += async delegate { await SwitchAndContinue(); };
            manage.Click += delegate { try { continuity.OpenAccountManager(); } catch (Exception ex) { Failure(ex); } };
            if (automaticLoad) Shown += async delegate { await Reload(); };
            FormClosing += delegate(object sender, FormClosingEventArgs args) { if (busy) { args.Cancel = true; status.Text = "操作正在进行，请等待完成或错误提示。"; } };
            FormClosed += delegate { previewVersion++; if (previewCancellation != null) { previewCancellation.Cancel(); previewCancellation.Dispose(); } };
        }

        private void AddColumn(string name, string title, int width)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = title, Width = width, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        }

        private void SetBusy(bool value)
        {
            busy = value; refresh.Enabled = !value; project.Enabled = !value; search.Enabled = !value; open.Enabled = !value; grid.Enabled = !value;
            accounts.Enabled = !value; switchOpen.Enabled = !value; manage.Enabled = !value;
        }

        private void Failure(Exception ex)
        {
            log.Write("error", ex.ToString()); status.Text = "操作失败：" + ex.Message;
            if (!interactiveErrors) throw new InvalidOperationException("窗口操作失败：" + ex.Message, ex);
            MessageBox.Show(this, ex.Message, "Claude 会话导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async Task Reload()
        {
            SetBusy(true); ClearPreview(); status.Text = "正在自动扫描本机会话…";
            try
            {
                var accountState = await continuity.ReadAccounts();
                accounts.Items.Clear();
                foreach (var account in accountState.accounts) accounts.Items.Add(account);
                accounts.DisplayMember = "Display";
                var currentAccount = accountState.accounts.FirstOrDefault(account => account.name == accountState.current);
                accounts.SelectedItem = currentAccount;
                provider = catalog.CurrentProvider();
                sessions = await Task.Run(() => catalog.Scan());
                identity = provider == "claude-desktop-official" ? catalog.OfficialIdentity : catalog.GatewayIdentity;
                if (catalog.CurrentProvider() != provider) throw new IOException("扫描期间 Desktop 模式已变化，请刷新列表。");
                Catalog.EnsureIdentity(catalog.ResolveIdentity(provider), identity);
                checkedSessions.Clear();
                var selectionTime = DateTimeOffset.UtcNow;
                foreach (var recent in sessions.Where(session => session.ConversationAt.HasValue &&
                    session.ConversationAt.Value >= selectionTime.AddHours(-settings.activeSessionHours) && session.ConversationAt.Value <= selectionTime))
                    checkedSessions.Add(recent.Path, recent);
                project.Items.Clear(); project.Items.Add("所有项目");
                foreach (var cwd in sessions.Select(item => item.Cwd).Where(cwd => !String.IsNullOrWhiteSpace(cwd)).Distinct().OrderBy(cwd => cwd)) project.Items.Add(cwd);
                int selected = String.IsNullOrWhiteSpace(settings.initialProjectPath) ? 0 : project.Items.IndexOf(settings.initialProjectPath);
                project.SelectedIndex = selected >= 0 ? selected : 0;
                mode.Text = (provider == "claude-desktop-official" ? "官方订阅" : "API / Gateway") + " · 当前 " +
                    (currentAccount == null ? "未匹配保存的账号" : currentAccount.Display) +
                    " · 默认勾选最近 " + settings.activeSessionHours + " 小时对话";
            }
            catch (Exception ex) { sessions.Clear(); checkedSessions.Clear(); grid.Rows.Clear(); Failure(ex); return; }
            finally { SetBusy(false); }
            Render();
        }

        private void Render()
        {
            rendering = true;
            grid.Rows.Clear(); string selectedProject = project.SelectedItem as string; string query = search.Text.Trim();
            foreach (var session in sessions)
            {
                if (project.SelectedIndex > 0 && session.Cwd != selectedProject) continue;
                string haystack = (session.Title ?? String.Empty) + " " + session.Id + " " + session.ProjectFolder;
                if (query.Length > 0 && haystack.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                bool registered = provider == "claude-desktop-official" ? session.OfficialRegistered : session.GatewayRegistered;
                string modified = TimeZoneInfo.ConvertTimeFromUtc(session.ModifiedUtc, zone).ToString("MM-dd HH:mm:ss");
                string last = session.ConversationAt.HasValue ? TimeZoneInfo.ConvertTime(session.ConversationAt.Value, zone).ToString("MM-dd HH:mm:ss") : "窗口内无对话";
                int number = grid.Rows.Add(checkedSessions.ContainsKey(session.Path), session.Title, modified, last, registered ? "已登记（可打开）" : "未登记", session.Id.Substring(0, 8),
                    session.InputTokens.HasValue ? session.InputTokens.Value.ToString("N0") : "片段内未知");
                grid.Rows[number].Tag = session; grid.Rows[number].Cells["Title"].ToolTipText = session.Path + "\n" + session.Cwd;
                grid.Rows[number].Cells["Id"].ToolTipText = session.Id;
            }
            if (grid.Rows.Count > 0) grid.CurrentCell = grid.Rows[0].Cells["Title"];
            rendering = false;
            UpdateSelection();
            StartPreview();
        }

        private void ClearPreview()
        {
            previewVersion++;
            if (previewCancellation != null) { previewCancellation.Cancel(); previewCancellation.Dispose(); previewCancellation = null; }
            preview.Clear(); previewHeading.Text = "点选会话查看最近对话；勾选用于导入。";
        }

        private void StartPreview()
        {
            ClearPreview();
            var session = grid.CurrentRow == null ? null : grid.CurrentRow.Tag as Session;
            if (session == null) { previewTask = Task.FromResult(0); return; }
            previewHeading.Text = "正在读取最近对话 · " + session.Title + " · " + session.Id.Substring(0, 8);
            previewCancellation = new CancellationTokenSource();
            previewTask = LoadPreview(session, previewVersion, previewCancellation.Token);
        }

        private async Task LoadPreview(Session session, int version, CancellationToken cancellation)
        {
            try
            {
                var messages = await Task.Run(() => catalog.ReadPreview(session, cancellation), cancellation);
                // 快速切换行时，只展示当前行；旧读取不能覆盖新会话的预览。
                if (IsDisposed || Disposing || version != previewVersion) return;
                previewHeading.Text = "最近对话 · " + session.Title + " · " + session.Id.Substring(0, 8) + " · " + messages.Count + " 条文字消息";
                previewHeading.Text += session.CompactAt.HasValue ? " · 已压缩 " + TimeZoneInfo.ConvertTime(session.CompactAt.Value, zone).ToString("MM-dd HH:mm") : " · 扫描片段未发现压缩";
                if (messages.Count == 0) { preview.Text = "最近片段内没有普通文字对话。"; return; }
                using (var headingFont = new Font(preview.Font, FontStyle.Bold))
                {
                    foreach (var message in messages)
                    {
                        preview.SelectionFont = headingFont;
                        preview.SelectionColor = message.Role == "user" ? Color.FromArgb(30, 90, 160) : Color.FromArgb(40, 110, 70);
                        preview.AppendText((message.Role == "user" ? "你" : "Claude") + "  " + TimeZoneInfo.ConvertTime(message.Timestamp, zone).ToString("MM-dd HH:mm:ss") + "\n");
                        preview.SelectionFont = preview.Font; preview.SelectionColor = Color.FromArgb(35, 35, 35);
                        preview.AppendText(message.Text + (message.Truncated ? "\n[此条内容已截短]" : String.Empty) + "\n\n");
                    }
                }
                preview.SelectionStart = preview.TextLength; preview.SelectionLength = 0; preview.ScrollToCaret();
            }
            catch (OperationCanceledException) { log.Write("preview_cancelled", "id=" + session.Id + ";selection_changed_or_window_closed=true"); }
            catch (Exception ex)
            {
                log.Write("preview_error", "id=" + session.Id + ";" + ex.ToString());
                if (IsDisposed || Disposing || version != previewVersion) return;
                previewHeading.Text = "预览读取失败 · " + session.Id.Substring(0, 8); preview.Text = ex.Message;
            }
        }

        private List<Session> Selected()
        {
            grid.EndEdit();
            return checkedSessions.Values.OrderByDescending(session => session.ModifiedUtc).ToList();
        }

        private void UpdateSelection()
        {
            if (!busy) status.Text = "扫描到 " + sessions.Count + " 个会话，当前显示 " + grid.Rows.Count + " 个；已勾选 " + checkedSessions.Count + " 个（切换项目或搜索时保留选择）。";
        }

        private async Task Import()
        {
            var selected = Selected(); SetBusy(true);
            try
            {
                catalog.ValidateSelection(selected);
                await catalog.OpenSelected(selected, provider, identity, new Progress<string>(detail => status.Text = detail));
                status.Text = "完成：已把 " + selected.Count + " 个选中会话交给 Desktop。";
                MessageBox.Show(this, status.Text, "Claude 会话导入", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Failure(ex); }
            finally { string outcome = status.Text; SetBusy(false); Render(); status.Text = outcome; }
        }

        private async Task SwitchAndContinue()
        {
            var selected = Selected(); var target = accounts.SelectedItem as Account;
            SetBusy(true);
            try
            {
                await continuity.SwitchAndOpen(selected, target, new Progress<string>(detail => status.Text = detail));
                SetBusy(false); await Reload(); status.Text = "完成：已在目标账号接续 " + selected.Count + " 个会话。";
            }
            catch (Exception ex) { Failure(ex); }
            finally { SetBusy(false); }
        }
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                if (args.Length != 1 || !Path.IsPathRooted(args[0])) throw new ArgumentException("必须提供一个绝对路径的配置文件。");
                var settings = Settings.Load(args[0]); Application.Run(new Picker(settings)); return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Claude 会话导入启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1;
            }
        }
    }
}
