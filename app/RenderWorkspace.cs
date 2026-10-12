using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClaudeSessionImporter;

// 仅渲染本程序控件；不是抓取用户桌面。公开导出明确覆盖敏感区域，不生成虚拟会话。
public static class RenderWorkspace
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct Range { public int First, Last; }
    [StructLayout(LayoutKind.Sequential)] private struct Format { public IntPtr Hdc, Target; public Rect Area, Page; public Range Characters; }
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr parameter, IntPtr content);
    private static void DrawPreview(Bitmap image, Picker window, RichTextBox preview)
    {
        // RichEdit 不参与 DrawToBitmap；使用它自己的打印接口渲染实际文本，不能用静态样例替代。
        using (var graphics = Graphics.FromImage(image))
        {
            var point = window.PointToClient(preview.PointToScreen(Point.Empty));
            int frame = (window.Width - window.ClientSize.Width) / 2;
            point.Offset(frame, window.Height - window.ClientSize.Height - frame);
            float scale = 1440f / graphics.DpiX; IntPtr dc = graphics.GetHdc();
            IntPtr memory = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Format)));
            try
            {
                var format = new Format { Hdc = dc, Target = dc,
                    Area = new Rect { Left = (int)(point.X * scale), Top = (int)(point.Y * scale), Right = (int)((point.X + preview.Width) * scale), Bottom = (int)((point.Y + preview.Height) * scale) },
                    Page = new Rect { Right = (int)(image.Width * scale), Bottom = (int)(image.Height * scale) },
                    Characters = new Range { First = preview.GetCharIndexFromPosition(Point.Empty), Last = -1 } };
                Marshal.StructureToPtr(format, memory, false); SendMessage(preview.Handle, 0x0439, new IntPtr(1), memory);
            }
            finally { SendMessage(preview.Handle, 0x0439, IntPtr.Zero, IntPtr.Zero); Marshal.FreeCoTaskMem(memory); graphics.ReleaseHdc(dc); }
        }
    }
    private static T Field<T>(Picker window, string name)
    { return (T)typeof(Picker).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window); }
    private static void Wait(Task pending)
    {
        var timer = Stopwatch.StartNew();
        while (!pending.IsCompleted)
        {
            if (timer.Elapsed.TotalSeconds > 60) throw new TimeoutException("Workspace rendering exceeded 60 seconds.");
            Application.DoEvents(); Thread.Sleep(10);
        }
        pending.GetAwaiter().GetResult(); Application.DoEvents();
    }
    private static void Hide(Control control, string caption)
    {
        var combo = control as ComboBox;
        if (combo != null) { combo.Items.Clear(); combo.Items.Add(caption); combo.SelectedIndex = 0; }
        else control.Text = caption;
    }
    [STAThread]
    public static int Main(string[] args)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            if (args.Length != 2) throw new ArgumentException("Provide absolute local config path and output directory.");
            var settings = Settings.Load(args[0]); Directory.CreateDirectory(args[1]);
            using (var window = new Picker(settings, false, false))
            {
                window.Show();
                var grid = Field<DataGridView>(window, "grid");
                var preview = Field<RichTextBox>(window, "preview");
                Wait((Task)typeof(Picker).GetMethod("Reload", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, null));
                Wait(Field<Task>(window, "previewTask"));
                using (var image = new Bitmap(window.Width, window.Height))
                {
                    window.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    DrawPreview(image, window, preview);
                    image.Save(Path.Combine(args[1], "workspace-private.png"), ImageFormat.Png);
                }
                Hide(Field<ComboBox>(window, "accounts"), "已隐去账号邮箱");
                Hide(Field<Label>(window, "mode"), "官方订阅 · 账号信息已隐去\n默认勾选最近 " + settings.activeSessionHours + " 小时对话");
                Hide(Field<ComboBox>(window, "project"), "本地项目 · 路径已隐去");
                Hide(Field<Label>(window, "previewHeading"), "最近对话 · 内容已隐去");
                var previewParent = (TableLayoutPanel)preview.Parent;
                previewParent.Controls.Remove(preview);
                previewParent.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(111, 121, 115), Font = preview.Font,
                    Text = "公开截图已隐藏聊天内容。\n\n本机右侧显示所选会话的真实用户与 Claude 对话。\n\n此截图来自真实应用与会话列表，不包含示例会话。" }, 0, 2);
                grid.CellPainting += delegate(object sender, DataGridViewCellPaintingEventArgs e)
                {
                    if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Title") return;
                    e.PaintBackground(e.ClipBounds, true);
                    TextRenderer.DrawText(e.Graphics, "会话标题已隐去", e.CellStyle.Font, e.CellBounds, e.CellStyle.ForeColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left); e.Handled = true;
                };
                Application.DoEvents();
                using (var image = new Bitmap(window.Width, window.Height))
                {
                    window.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                    image.Save(Path.Combine(args[1], "workspace-public.png"), ImageFormat.Png);
                }
            }
            Console.WriteLine("Rendered actual application; private and redacted exports are separate."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
