using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ClaudeSessionImporter
{
    public sealed partial class Picker
    {
        // ==================== Claude 高对比度、大字号、零浅灰小字视觉系统 ====================
        private static readonly Color SidebarBg = Color.FromArgb(244, 241, 234);       // #F4F1EA 温暖燕麦侧栏
        private static readonly Color SidebarBorder = Color.FromArgb(222, 218, 208);   // #DEDAD0 侧栏清晰分割线
        private static readonly Color PaperBg = Color.FromArgb(250, 249, 245);         // #FAF9F5 主工作区暖白画布
        private static readonly Color CardBg = Color.White;                            // #FFFFFF 纯白卡片
        private static readonly Color Ink = Color.FromArgb(18, 18, 17);                // #121211 浓郁纯正深炭黑（大标题/粗体主字）
        private static readonly Color InkNormal = Color.FromArgb(42, 40, 36);          // #2A2824 扎实深色常规字（绝不用看不清的浅灰！）
        private static readonly Color AccentPrimary = Color.FromArgb(217, 95, 52);     // #D95F34 Claude 陶土珊瑚橙
        private static readonly Color AccentHover = Color.FromArgb(194, 76, 36);       // #C24C24 悬停深陶土橙
        private static readonly Color AccentSoft = Color.FromArgb(253, 236, 228);      // #FDECE4 蜜桃粉徽章底色
        private static readonly Color BorderColor = Color.FromArgb(216, 212, 202);     // #D8D4CA 清晰卡片与输入框边框
        private static readonly Color RowSelectColor = Color.FromArgb(253, 240, 233);  // #FDF0E9 选中行暖色底
        private static readonly Color TableHeaderBg = Color.FromArgb(243, 241, 235);   // #F3F1EB 清晰表头底色
        private static readonly Color PillGreenBg = Color.FromArgb(224, 244, 232);     // #E0F4E8 已登记薄荷绿胶囊底色
        private static readonly Color PillGreenFg = Color.FromArgb(18, 112, 56);       // #127038 已登记深翠绿粗体字
        private static readonly Color PillAmberBg = Color.FromArgb(254, 236, 220);     // #FEECDC 未登记暖橙胶囊底色
        private static readonly Color PillAmberFg = Color.FromArgb(186, 74, 28);       // #BA4A1C 未登记深暖橙粗体字
        private static readonly Color PillGrayBg = Color.FromArgb(235, 233, 228);      // #EBE9E4 缺失目录底色
        private static readonly Color PillGrayFg = Color.FromArgb(75, 72, 66);        // #4B4842 缺失目录深色字

        private static GraphicsPath RoundRect(RectangleF bounds, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();
            if (radius <= 0.5f || bounds.Width < d || bounds.Height < d)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawSharpText(Graphics g, string text, Font font, Rectangle bounds, Color fg, Color bg, TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text ?? String.Empty, font, bounds, fg, bg, flags);
        }

        private static void DrawSharpTextAt(Graphics g, string text, Font font, Point pt, Color fg, Color bg)
        {
            TextRenderer.DrawText(g, text ?? String.Empty, font, pt, fg, bg);
        }

        private Label LabelAt(string text, float size, Color color, FontStyle weight)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", size, weight),
                ForeColor = color,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0)
            };
        }

        // GDI+ 大号圆角主/次按钮（11pt 粗体大字，一眼看清）
        private void StyleButton(Button button, string text, bool primary)
        {
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = SidebarBg;
            button.ForeColor = primary ? Color.White : Ink;
            button.Font = new Font("Microsoft YaHei UI", 10.8F, FontStyle.Bold);
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 4, 0, 4);
            button.Cursor = Cursors.Hand;
            bool hovered = false;
            button.MouseEnter += delegate { hovered = true; button.Invalidate(); };
            button.MouseLeave += delegate { hovered = false; button.Invalidate(); };
            button.Paint += delegate(object sender, PaintEventArgs e)
            {
                if (button.Width <= 4 || button.Height <= 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(SidebarBg);

                Color fill = primary
                    ? (hovered && button.Enabled ? AccentHover : AccentPrimary)
                    : (hovered && button.Enabled ? RowSelectColor : Color.White);
                if (!button.Enabled) fill = Color.FromArgb(230, 227, 220);

                var rect = new RectangleF(1f, 1f, button.Width - 2.5f, button.Height - 2.5f);
                using (var path = RoundRect(rect, 9f))
                {
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);

                    if (!primary)
                    {
                        Color border = hovered && button.Enabled ? AccentPrimary : BorderColor;
                        using (var pen = new Pen(border, 1.2f))
                            g.DrawPath(pen, path);
                    }
                }

                Color fg = !button.Enabled ? InkNormal : (primary ? Color.White : (hovered ? AccentHover : Ink));
                string raw = button.Text ?? String.Empty;
                bool hasArrow = raw.Contains("→");
                string cleanText = raw.Replace("  →", "").Replace("→", "").Trim();

                if (hasArrow)
                {
                    var textRect = new Rectangle(16, 0, Math.Max(10, button.Width - 44), button.Height);
                    DrawSharpText(g, cleanText, button.Font, textRect, fg, fill,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    var arrowRect = new Rectangle(Math.Max(0, button.Width - 34), 0, 22, button.Height);
                    using (var arrowFont = new Font("Microsoft YaHei UI", 11.5F, FontStyle.Bold))
                        DrawSharpText(g, "→", arrowFont, arrowRect, fg, fill,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }
                else
                {
                    var innerRect = new Rectangle(8, 0, Math.Max(10, button.Width - 16), button.Height);
                    DrawSharpText(g, cleanText, button.Font, innerRect, fg, fill,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
                }
            };
        }

        // GDI+ 工具栏按钮（10pt 深炭黑粗体/常规字，绝不发灰发浅）
        private void StyleSecondaryToolButton(Button button, string text, Padding margin, bool isPreviewBadge = false)
        {
            Color clearBg = isPreviewBadge ? CardBg : PaperBg;
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = clearBg;
            button.ForeColor = Ink;
            button.Font = new Font("Microsoft YaHei UI", 9.8F, FontStyle.Bold);
            button.Dock = DockStyle.Fill;
            button.Margin = margin;
            button.Cursor = Cursors.Hand;
            bool hovered = false;
            button.MouseEnter += delegate { hovered = true; button.Invalidate(); };
            button.MouseLeave += delegate { hovered = false; button.Invalidate(); };
            button.Paint += delegate(object sender, PaintEventArgs e)
            {
                if (button.Width <= 4 || button.Height <= 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(clearBg);

                bool isSelectedBadge = isPreviewBadge && (button.Text ?? "").Contains("✓");
                Color fill = isSelectedBadge
                    ? AccentSoft
                    : (hovered && button.Enabled ? RowSelectColor : Color.White);

                var rect = new RectangleF(1f, 1f, button.Width - 2.5f, button.Height - 2.5f);
                using (var path = RoundRect(rect, 7.5f))
                {
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);

                    Color border = isSelectedBadge
                        ? AccentPrimary
                        : (hovered && button.Enabled ? AccentPrimary : BorderColor);
                    using (var pen = new Pen(border, 1.2f))
                        g.DrawPath(pen, path);
                }

                Color fg = isSelectedBadge
                    ? AccentHover
                    : (hovered && button.Enabled ? AccentHover : Ink);
                string label = button.Text ?? String.Empty;
                if (label == "刷新") label = "↻ 刷新";
                var innerRect = new Rectangle(4, 0, Math.Max(8, button.Width - 8), button.Height);
                DrawSharpText(g, label, button.Font, innerRect, fg, fill,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            };
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private Panel CreateModernComboContainer(ComboBox combo, bool drawUserBadge, Color parentBg)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = parentBg,
                Padding = new Padding(drawUserBadge ? 36 : 12, 6, 28, 6),
                Margin = new Padding(0, 2, 0, 6),
                Cursor = Cursors.Hand
            };
            var innerClip = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0),
                Cursor = Cursors.Hand
            };
            combo.FlatStyle = FlatStyle.Flat;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = 26;
            combo.BackColor = Color.White;
            combo.ForeColor = Ink;
            combo.Dock = DockStyle.None;
            innerClip.Resize += delegate
            {
                combo.Location = new Point(-2, -2);
                combo.Width = Math.Max(40, innerClip.Width + 22);
            };
            combo.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                Color itemBg = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0
                    ? RowSelectColor : Color.White;
                using (var bg = new SolidBrush(itemBg))
                {
                    e.Graphics.FillRectangle(bg, e.Bounds);
                }
                if (e.Index >= 0 && e.Index < combo.Items.Count)
                {
                    string itemText = combo.GetItemText(combo.Items[e.Index]);
                    DrawSharpText(e.Graphics, itemText, combo.Font,
                        new Rectangle(e.Bounds.X + 2, e.Bounds.Y, e.Bounds.Width - 22, e.Bounds.Height),
                        Ink, itemBg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                }
            };
            EventHandler toggleDrop = delegate { combo.Focus(); combo.DroppedDown = !combo.DroppedDown; };
            host.Click += toggleDrop;
            innerClip.Click += toggleDrop;
            innerClip.Controls.Add(combo);
            host.Controls.Add(innerClip);
            host.Paint += delegate(object sender, PaintEventArgs e)
            {
                if (host.Width <= 4 || host.Height <= 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(parentBg);
                var rect = new RectangleF(0.5f, 0.5f, host.Width - 1.5f, host.Height - 1.5f);
                using (var path = RoundRect(rect, 8f))
                using (var fill = new SolidBrush(Color.White))
                using (var pen = new Pen(BorderColor, 1.2f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
                if (drawUserBadge)
                {
                    var badgeRect = new RectangleF(9f, (host.Height - 22f) / 2f, 22f, 22f);
                    using (var bBrush = new SolidBrush(AccentSoft))
                        g.FillEllipse(bBrush, badgeRect);
                    using (var pPen = new Pen(AccentPrimary, 1.6f))
                    {
                        g.DrawEllipse(pPen, badgeRect.X + 7f, badgeRect.Y + 4f, 8f, 8f);
                        g.DrawArc(pPen, badgeRect.X + 4.5f, badgeRect.Y + 12f, 13f, 8f, 180, 180);
                    }
                }
                float ax = host.Width - 16f, ay = host.Height / 2f - 1.5f;
                using (var arrowPen = new Pen(Ink, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    g.DrawLines(arrowPen, new[] {
                        new PointF(ax - 4.5f, ay - 1.5f),
                        new PointF(ax, ay + 3f),
                        new PointF(ax + 4.5f, ay - 1.5f)
                    });
                }
            };
            return host;
        }

        private Panel CreateModernSearchContainer(TextBox textBox, Color parentBg)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = parentBg,
                Padding = new Padding(34, 8, 10, 6),
                Margin = new Padding(0, 0, 12, 0),
                Cursor = Cursors.IBeam
            };
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = Color.White;
            textBox.ForeColor = Ink;
            textBox.Dock = DockStyle.Fill;
            textBox.HandleCreated += delegate
            {
                SendMessage(textBox.Handle, 0x1501, new IntPtr(1), "输入关键词搜索会话标题、ID 或项目...");
            };
            host.Controls.Add(textBox);

            bool focused = false;
            textBox.GotFocus += delegate { focused = true; host.Invalidate(); };
            textBox.LostFocus += delegate { focused = false; host.Invalidate(); };
            textBox.TextChanged += delegate { host.Invalidate(); };
            host.Click += delegate { textBox.Focus(); };

            host.Paint += delegate(object sender, PaintEventArgs e)
            {
                if (host.Width <= 4 || host.Height <= 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(parentBg);
                var rect = new RectangleF(0.5f, 0.5f, host.Width - 1.5f, host.Height - 1.5f);
                using (var path = RoundRect(rect, 8f))
                using (var fill = new SolidBrush(Color.White))
                using (var pen = new Pen(focused ? AccentPrimary : BorderColor, focused ? 1.5f : 1.2f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
                float cx = 16f, cy = host.Height / 2f - 1f;
                using (var iconPen = new Pen(InkNormal, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawEllipse(iconPen, cx - 5.5f, cy - 5.5f, 10f, 10f);
                    g.DrawLine(iconPen, cx + 2.5f, cy + 2.5f, cx + 6.2f, cy + 6.2f);
                }
            };
            return host;
        }

        private void BuildLayout()
        {
            SuspendLayout();
            Text = "Continuity · Claude 账号与会话接续";
            try
            {
                string iconFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cmd", "assets", "continuity.ico");
                if (System.IO.File.Exists(iconFile)) Icon = new Icon(iconFile);
                else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            Font = new Font("Microsoft YaHei UI", 10F);
            Size = new Size(1540, 920);
            MinimumSize = new Size(1240, 780);
            BackColor = PaperBg;
            ForeColor = Ink;
            StartPosition = FormStartPosition.CenterScreen;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = PaperBg
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 316F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            // ==================== 1. 侧边栏容器 ====================
            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarBg,
                Padding = new Padding(24, 26, 24, 22),
                Margin = new Padding(0)
            };
            sidebar.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (var pen = new Pen(SidebarBorder, 1f))
                    e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };
            root.Controls.Add(sidebar, 0, 0);

            var sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarBg,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0)
            };
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 266F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            sidebar.Controls.Add(sidebarLayout);

            // 1.1 品牌头（超大字标题 20pt Bold + 清晰深色副标题 10pt）
            var brandPanel = new Panel { Dock = DockStyle.Fill, BackColor = SidebarBg, Margin = new Padding(0) };
            brandPanel.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(SidebarBg);

                var logoRect = new RectangleF(0f, 6f, 46f, 46f);
                using (var path = RoundRect(logoRect, 11f))
                using (var brush = new SolidBrush(AccentPrimary))
                    g.FillPath(brush, path);

                float cx = 23f, cy = 29f;
                using (var sparkPen = new Pen(Color.White, 2.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    for (int i = 0; i < 6; i++)
                    {
                        double a = i * Math.PI / 3.0 + 0.22;
                        g.DrawLine(sparkPen, cx - (float)Math.Cos(a) * 11.5f, cy - (float)Math.Sin(a) * 11.5f,
                            cx + (float)Math.Cos(a) * 11.5f, cy + (float)Math.Sin(a) * 11.5f);
                    }
                }

                using (var titleFont = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold))
                using (var subFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular))
                {
                    DrawSharpTextAt(g, "Continuity", titleFont, new Point(56, 0), Ink, SidebarBg);
                    DrawSharpTextAt(g, "换个账号，继续手头工作", subFont, new Point(58, 38), InkNormal, SidebarBg);
                }
            };
            sidebarLayout.Controls.Add(brandPanel, 0, 0);

            // 1.2 目标账号与核心动作卡片（全部放大字号、深色高对比度）
            var actionCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarBg,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0, 6, 0, 6),
                Padding = new Padding(0)
            };
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            actionCard.Controls.Add(LabelAt("当前 / 目标账号", 11F, Ink, FontStyle.Bold), 0, 0);

            accounts.DropDownStyle = ComboBoxStyle.DropDownList;
            accounts.AccessibleName = "选择目标账号";
            accounts.Font = new Font("Microsoft YaHei UI", 10.2F, FontStyle.Bold);
            actionCard.Controls.Add(CreateModernComboContainer(accounts, true, SidebarBg), 0, 1);

            mode.ForeColor = InkNormal;
            mode.Dock = DockStyle.Fill;
            mode.AutoEllipsis = true;
            mode.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);
            mode.Padding = new Padding(2, 4, 0, 4);
            actionCard.Controls.Add(mode, 0, 2);

            StyleButton(switchOpen, "切换账号并接续  →", true);
            actionCard.Controls.Add(switchOpen, 0, 3);

            StyleButton(open, "在当前账号打开", false);
            open.AccessibleName = "导入或打开选中会话";
            actionCard.Controls.Add(open, 0, 4);

            sidebarLayout.Controls.Add(actionCard, 0, 1);

            // 1.3 快速上手（11pt 粗体标题 + 10pt 深色步骤文字）
            var guideBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarBg,
                Margin = new Padding(0, 8, 0, 4)
            };
            guideBox.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(SidebarBg);

                using (var linePen = new Pen(SidebarBorder, 1f))
                    g.DrawLine(linePen, 0, 2, guideBox.Width, 2);

                using (var headFont = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold))
                using (var numFont = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Bold))
                using (var stepFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular))
                {
                    DrawSharpTextAt(g, "快速上手", headFont, new Point(0, 14), Ink, SidebarBg);

                    string[] nums = { "1", "2", "3" };
                    string[] steps = {
                        "选择目标账号（或保持当前）",
                        "勾选新会话或单选预览行",
                        "一键唤醒或迁移至 Desktop"
                    };
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 48f + i * 36f;
                        var pill = new RectangleF(0f, y, 26f, 24f);
                        using (var path = RoundRect(pill, 8f))
                        using (var bBrush = new SolidBrush(AccentSoft))
                            g.FillPath(bBrush, path);

                        DrawSharpText(g, nums[i], numFont, Rectangle.Round(pill), AccentPrimary, AccentSoft,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        DrawSharpTextAt(g, steps[i], stepFont, new Point(34, (int)y + 1), InkNormal, SidebarBg);
                    }
                }
            };
            sidebarLayout.Controls.Add(guideBox, 0, 2);

            // 1.4 底部管理区（去掉密密麻麻的小字提示，只保留清晰的按钮与一行 9.5pt 深色说明）
            var lowerBox = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarBg,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            StyleButton(manage, "＋ 添加账号 / 备份…", false);
            lowerBox.Controls.Add(manage, 0, 0);

            lowerBox.Controls.Add(LabelAt("跨账号切换会自动重启 Desktop；同账号直接唤醒。", 9.2F, InkNormal, FontStyle.Regular), 0, 1);

            sidebarLayout.Controls.Add(lowerBox, 0, 4);

            // ==================== 2. 主工作区 ====================
            var workspace = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(32, 24, 32, 18),
                RowCount = 4,
                ColumnCount = 1,
                BackColor = PaperBg,
                Margin = new Padding(0)
            };
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.Controls.Add(workspace, 1, 0);

            // 2.1 顶部超大标题与大号深色副标题
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PaperBg,
                RowCount = 2,
                ColumnCount = 1,
                Margin = new Padding(0)
            };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            header.Controls.Add(LabelAt("从上次停下的地方继续", 20F, Ink, FontStyle.Bold), 0, 0);
            header.Controls.Add(LabelAt("最近 " + settings.activeSessionHours + " 小时内有对话的会话已自动勾选 · 点击任意行在右侧查看对话", 10.5F, InkNormal, FontStyle.Regular), 0, 1);
            workspace.Controls.Add(header, 0, 0);

            // 2.2 筛选栏与快捷勾选操作（10.5pt 粗体深色标签 + 10.2pt 输入框）
            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PaperBg,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(0, 4, 0, 8),
                Margin = new Padding(0)
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

            filters.Controls.Add(LabelAt("项目目录", 10.2F, Ink, FontStyle.Bold), 0, 0);
            filters.Controls.Add(LabelAt("搜索会话", 10.2F, Ink, FontStyle.Bold), 1, 0);
            var quickSelectLabel = LabelAt("快捷操作", 10.2F, Ink, FontStyle.Bold);
            filters.Controls.Add(quickSelectLabel, 2, 0);
            filters.SetColumnSpan(quickSelectLabel, 2);

            project.DropDownStyle = ComboBoxStyle.DropDownList;
            project.Font = new Font("Microsoft YaHei UI", 10.2F, FontStyle.Regular);
            var projectHost = CreateModernComboContainer(project, false, PaperBg);
            projectHost.Margin = new Padding(0, 0, 12, 0);
            filters.Controls.Add(projectHost, 0, 1);

            search.AccessibleName = "搜索会话";
            search.Font = new Font("Microsoft YaHei UI", 10.2F, FontStyle.Regular);
            filters.Controls.Add(CreateModernSearchContainer(search, PaperBg), 1, 1);

            StyleSecondaryToolButton(selectUnregistered, "仅勾未登记", new Padding(0, 0, 8, 0));
            selectUnregistered.AccessibleName = "仅勾选未登记会话";
            filters.Controls.Add(selectUnregistered, 2, 1);

            StyleSecondaryToolButton(clearSelection, "清空勾选", new Padding(0, 0, 8, 0));
            clearSelection.AccessibleName = "清空所有勾选";
            filters.Controls.Add(clearSelection, 3, 1);

            StyleSecondaryToolButton(refresh, "刷新", new Padding(0));
            refresh.AccessibleName = "刷新列表";
            filters.Controls.Add(refresh, 4, 1);

            workspace.Controls.Add(filters, 0, 1);

            // 2.3 内容分割区 (左列表 + 右预览)
            var content = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Size = new Size(1140, 620),
                SplitterDistance = 660,
                SplitterWidth = 16,
                Panel1MinSize = 420,
                Panel2MinSize = 300,
                BackColor = PaperBg,
                TabStop = false
            };
            workspace.Controls.Add(content, 0, 2);

            // 2.3.1 会话表格配置（大字号 10.5pt~11pt，深色表头，50px 宽敞行高）
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = CardBg;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(234, 231, 224);
            grid.RowTemplate.Height = 50;
            grid.ColumnHeadersHeight = 44;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = TableHeaderBg,
                ForeColor = Ink,
                Font = new Font("Microsoft YaHei UI", 10.2F, FontStyle.Bold),
                Padding = new Padding(8, 0, 8, 0)
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Ink,
                SelectionBackColor = RowSelectColor,
                SelectionForeColor = Ink,
                Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular),
                Padding = new Padding(8, 6, 8, 6)
            };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 251, 248);

            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Choose",
                HeaderText = "",
                Width = 46,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            AddColumn("Title", "会话标题", 240);
            AddColumn("Modified", "文件修改", 140);
            AddColumn("Conversation", "最后对话时间", 150);
            AddColumn("Registered", "Desktop 状态", 142);
            AddColumn("Id", "会话 ID", 105);
            AddColumn("Input", "请求输入", 110);

            grid.Columns["Title"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["Title"].MinimumWidth = 160;
            grid.Columns["Modified"].Visible = false;
            grid.Columns["Id"].Visible = false;
            grid.Columns["Input"].Visible = false;
            grid.AccessibleName = "本地会话列表";

            grid.CellPainting += delegate(object sender, DataGridViewCellPaintingEventArgs e)
            {
                if (e.ColumnIndex < 0) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                string colName = grid.Columns[e.ColumnIndex].Name;

                if (e.RowIndex == -1 && colName == "Choose")
                {
                    e.PaintBackground(e.ClipBounds, false);
                    var box = new RectangleF(e.CellBounds.X + (e.CellBounds.Width - 18f) / 2f,
                        e.CellBounds.Y + (e.CellBounds.Height - 18f) / 2f, 18f, 18f);
                    using (var path = RoundRect(box, 4.5f))
                    using (var pen = new Pen(InkNormal, 1.4f))
                        g.DrawPath(pen, path);
                    e.Handled = true;
                    return;
                }

                if (e.RowIndex < 0) return;

                bool isCheckedRow = Equals(grid.Rows[e.RowIndex].Cells["Choose"].Value, true);
                bool isSelectedRow = (e.State & DataGridViewElementStates.Selected) != 0;
                Color rowBg = (isSelectedRow || isCheckedRow)
                    ? RowSelectColor
                    : (e.RowIndex % 2 == 1 ? Color.FromArgb(252, 251, 248) : Color.White);

                using (var bgBrush = new SolidBrush(rowBg))
                    g.FillRectangle(bgBrush, e.CellBounds);
                using (var linePen = new Pen(grid.GridColor, 1f))
                    g.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                if (colName == "Choose")
                {
                    var box = new RectangleF(e.CellBounds.X + (e.CellBounds.Width - 19f) / 2f,
                        e.CellBounds.Y + (e.CellBounds.Height - 19f) / 2f, 19f, 19f);
                    using (var path = RoundRect(box, 5f))
                    {
                        if (isCheckedRow)
                        {
                            using (var fill = new SolidBrush(AccentPrimary))
                                g.FillPath(fill, path);
                            using (var checkPen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                            {
                                g.DrawLines(checkPen, new[] {
                                    new PointF(box.X + 4.2f, box.Y + 9.8f),
                                    new PointF(box.X + 8.0f, box.Y + 13.6f),
                                    new PointF(box.X + 14.5f, box.Y + 5.8f)
                                });
                            }
                        }
                        else
                        {
                            using (var fill = new SolidBrush(Color.White))
                                g.FillPath(fill, path);
                            using (var pen = new Pen( Color.FromArgb(165, 160, 150), 1.4f))
                                g.DrawPath(pen, path);
                        }
                    }
                    e.Handled = true;
                }
                else if (colName == "Title")
                {
                    string titleText = Convert.ToString(e.FormattedValue) ?? String.Empty;
                    float iconX = e.CellBounds.X + 8f;
                    float iconY = e.CellBounds.Y + (e.CellBounds.Height - 20f) / 2f;
                    var iconRect = new RectangleF(iconX, iconY, 18f, 20f);
                    bool isDocStyle = isCheckedRow || titleText.IndexOf("fork", StringComparison.OrdinalIgnoreCase) >= 0;

                    using (var iconPath = RoundRect(iconRect, 4f))
                    using (var iconBg = new SolidBrush(isDocStyle ? AccentSoft : Color.FromArgb(238, 236, 230)))
                    using (var iconPen = new Pen(isDocStyle ? AccentPrimary : InkNormal, 1.4f))
                    {
                        g.FillPath(iconBg, iconPath);
                        g.DrawPath(iconPen, iconPath);
                        g.DrawLine(iconPen, iconX + 4.5f, iconY + 6.5f, iconX + 13.5f, iconY + 6.5f);
                        g.DrawLine(iconPen, iconX + 4.5f, iconY + 10.5f, iconX + 13.5f, iconY + 10.5f);
                        g.DrawLine(iconPen, iconX + 4.5f, iconY + 14.5f, iconX + 10.5f, iconY + 14.5f);
                    }

                    var textBounds = new Rectangle(e.CellBounds.X + 34, e.CellBounds.Y, e.CellBounds.Width - 42, e.CellBounds.Height);
                    using (var rowFont = new Font("Microsoft YaHei UI", 10.5F, isCheckedRow ? FontStyle.Bold : FontStyle.Regular))
                    {
                        DrawSharpText(g, titleText, rowFont, textBounds, Ink, rowBg,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    }
                    e.Handled = true;
                }
                else if (colName == "Registered")
                {
                    string stateText = Convert.ToString(e.FormattedValue) ?? String.Empty;
                    bool isReg = stateText.StartsWith("已登记");
                    bool isMissing = stateText.Contains("缺失");

                    Color pillBg = isMissing ? PillGrayBg : (isReg ? PillGreenBg : PillAmberBg);
                    Color pillFg = isMissing ? PillGrayFg : (isReg ? PillGreenFg : PillAmberFg);

                    using (var badgeFont = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Bold))
                    {
                        Size textSz = TextRenderer.MeasureText(g, stateText, badgeFont);
                        float pillW = Math.Min(e.CellBounds.Width - 10f, textSz.Width + 14f);
                        float pillH = 26f;
                        float pillX = e.CellBounds.X + 5f;
                        float pillY = e.CellBounds.Y + (e.CellBounds.Height - pillH) / 2f;
                        var pillRect = new RectangleF(pillX, pillY, pillW, pillH);

                        using (var path = RoundRect(pillRect, 6.5f))
                        using (var fill = new SolidBrush(pillBg))
                            g.FillPath(fill, path);

                        DrawSharpText(g, stateText, badgeFont, Rectangle.Round(pillRect), pillFg, pillBg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                    e.Handled = true;
                }
                else
                {
                    string val = Convert.ToString(e.FormattedValue) ?? String.Empty;
                    var textBounds = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                    using (var timeFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular))
                    {
                        DrawSharpText(g, val, timeFont, textBounds, InkNormal, rowBg,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    }
                    e.Handled = true;
                }
            };

            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                Padding = new Padding(2),
                Margin = new Padding(0)
            };
            gridCard.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new RectangleF(0.5f, 0.5f, gridCard.Width - 1.5f, gridCard.Height - 1.5f);
                using (var path = RoundRect(r, 10f))
                using (var pen = new Pen(BorderColor, 1.2f))
                    e.Graphics.DrawPath(pen, path);
            };
            gridCard.Controls.Add(grid);
            content.Panel1.Controls.Add(gridCard);

            // 2.3.2 预览面板 (严格保留 TableLayoutPanel 4 行结构以保证 RenderWorkspace 兼容性)
            var previewLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                Padding = new Padding(22, 16, 22, 14),
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0)
            };
            previewLayout.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new RectangleF(0.5f, 0.5f, previewLayout.Width - 1.5f, previewLayout.Height - 1.5f);
                using (var path = RoundRect(r, 10f))
                using (var pen = new Pen(BorderColor, 1.2f))
                    e.Graphics.DrawPath(pen, path);
            };
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            content.Panel2.Controls.Add(previewLayout);

            var previewTopBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            previewTopBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            previewTopBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
            previewTopBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var previewBadgeHeader = new Panel { Dock = DockStyle.Fill, BackColor = CardBg, Margin = new Padding(0) };
            previewBadgeHeader.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(CardBg);
                var iconBox = new RectangleF(0f, 5f, 22f, 22f);
                using (var path = RoundRect(iconBox, 5.5f))
                using (var brush = new SolidBrush(AccentPrimary))
                    g.FillPath(brush, path);
                using (var pen = new Pen(Color.White, 1.5f))
                {
                    g.DrawLine(pen, 5.5f, 10f, 16.5f, 10f);
                    g.DrawLine(pen, 5.5f, 14f, 16.5f, 14f);
                    g.DrawLine(pen, 5.5f, 18f, 12.5f, 18f);
                }
                using (var f = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold))
                    DrawSharpTextAt(g, "对话内容预览", f, new Point(30, 4), Ink, CardBg);
            };
            previewTopBar.Controls.Add(previewBadgeHeader, 0, 0);

            StyleSecondaryToolButton(togglePreviewed, "仅选此条会话", new Padding(0, 2, 0, 2), true);
            togglePreviewed.AccessibleName = "仅选当前预览会话";
            previewTopBar.Controls.Add(togglePreviewed, 1, 0);
            previewLayout.Controls.Add(previewTopBar, 0, 0);

            // 自绘 previewHeading：超大号主标题 (12pt Bold) + 深色清晰副标题 (9.8pt Regular)
            previewHeading.Dock = DockStyle.Fill;
            previewHeading.BackColor = CardBg;
            previewHeading.ForeColor = Ink;
            previewHeading.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
            previewHeading.AutoEllipsis = true;
            previewHeading.Text = "点选左侧任意会话，在此处查看对话记录。";
            previewHeading.Margin = new Padding(0);
            previewHeading.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(CardBg);

                string raw = previewHeading.Text ?? String.Empty;
                string mainLine = raw;
                string subLine = String.Empty;

                if (raw.StartsWith("最近对话 · "))
                {
                    string rest = raw.Substring("最近对话 · ".Length);
                    string[] parts = rest.Split(new[] { " · " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        mainLine = parts[0];
                        subLine = "会话 ID: " + String.Join("  ·  ", parts, 1, parts.Length - 1);
                    }
                }

                using (var titleFont = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold))
                using (var metaFont = new Font("Microsoft YaHei UI", 9.8F, FontStyle.Regular))
                {
                    if (subLine.Length > 0)
                    {
                        DrawSharpText(g, mainLine, titleFont, new Rectangle(0, 4, previewHeading.Width, 28), Ink, CardBg,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        DrawSharpText(g, subLine, metaFont, new Rectangle(0, 34, previewHeading.Width, 24), InkNormal, CardBg,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                    else
                    {
                        DrawSharpText(g, mainLine, titleFont, new Rectangle(0, 8, previewHeading.Width, 44), InkNormal, CardBg,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                    }
                }
                using (var linePen = new Pen(BorderColor, 1f))
                    g.DrawLine(linePen, 0, previewHeading.Height - 2, previewHeading.Width, previewHeading.Height - 2);
            };
            previewLayout.Controls.Add(previewHeading, 0, 1);

            preview.Dock = DockStyle.Fill;
            preview.ReadOnly = true;
            preview.DetectUrls = false;
            preview.WordWrap = true;
            preview.BorderStyle = BorderStyle.None;
            preview.BackColor = CardBg;
            preview.ScrollBars = RichTextBoxScrollBars.Vertical;
            preview.AccessibleName = "最近对话预览";
            preview.Font = new Font("Microsoft YaHei UI", 10.5F);
            preview.Margin = new Padding(0, 8, 0, 4);
            previewLayout.Controls.Add(preview, 0, 2);

            previewLayout.Controls.Add(LabelAt("本地只读安全预览 · 不改写原始会话记录", 9.2F, InkNormal, FontStyle.Regular), 0, 3);

            // 2.4 状态栏（10pt 深色字，清晰易读）
            var statusPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PaperBg,
                Margin = new Padding(0)
            };
            status.Dock = DockStyle.Fill;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.ForeColor = InkNormal;
            status.Font = new Font("Microsoft YaHei UI", 9.8F, FontStyle.Regular);
            status.AutoEllipsis = true;
            statusPanel.Controls.Add(status);
            workspace.Controls.Add(statusPanel, 0, 3);

            ResumeLayout(true);
        }
    }
}
