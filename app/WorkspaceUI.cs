using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ClaudeSessionImporter
{
    public sealed partial class Picker
    {
        // ==================== Claude 现代小清新色彩系统 ====================
        private static readonly Color SidebarBg = Color.FromArgb(246, 244, 239);       // #F6F4EF 清新浅燕麦侧栏
        private static readonly Color SidebarBorder = Color.FromArgb(230, 227, 219);   // #E6E3DB 侧栏柔和分割线
        private static readonly Color PaperBg = Color.FromArgb(250, 249, 246);         // #FAF9F6 主工作区暖白画布
        private static readonly Color CardBg = Color.White;                            // #FFFFFF 纯白卡片
        private static readonly Color Ink = Color.FromArgb(24, 24, 23);                // #181817 主标题炭黑
        private static readonly Color InkSub = Color.FromArgb(58, 55, 48);             // #3A3730 正文深暖灰
        private static readonly Color Muted = Color.FromArgb(132, 128, 118);           // #848076 辅助说明暖灰
        private static readonly Color AccentPrimary = Color.FromArgb(224, 93, 52);     // #E05D34 Claude 活力珊瑚陶土橙
        private static readonly Color AccentHover = Color.FromArgb(201, 77, 38);       // #C94D26 悬停深珊瑚橙
        private static readonly Color AccentSoft = Color.FromArgb(253, 239, 233);      // #FDEFE9 浅蜜桃粉徽章/胶囊底色
        private static readonly Color BorderColor = Color.FromArgb(228, 225, 218);     // #E4E1DA 卡片与控件圆角细边框
        private static readonly Color RowSelectColor = Color.FromArgb(253, 242, 236);  // #FDF2EC 选中行浅暖橙底色
        private static readonly Color TableHeaderBg = Color.FromArgb(247, 246, 242);   // #F7F6F2 现代浅灰白表头
        private static readonly Color PillGreenBg = Color.FromArgb(231, 246, 237);     // #E7F6ED 已登记薄荷绿胶囊底色
        private static readonly Color PillGreenFg = Color.FromArgb(31, 136, 73);       // #1F8849 已登记翠绿文字
        private static readonly Color PillAmberBg = Color.FromArgb(254, 242, 232);     // #FEF2E8 未登记暖橙胶囊底色
        private static readonly Color PillAmberFg = Color.FromArgb(212, 96, 46);       // #D4602E 未登记暖橙文字
        private static readonly Color PillGrayBg = Color.FromArgb(241, 239, 235);      // #F1EFEB 缺失目录灰底
        private static readonly Color PillGrayFg = Color.FromArgb(135, 130, 122);     // #87827A 缺失目录灰字

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

        // GDI+ 抗锯齿圆角主/次按钮
        private void StyleButton(Button button, string text, bool primary)
        {
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = SidebarBg;
            button.ForeColor = primary ? Color.White : InkSub;
            button.Font = new Font("Microsoft YaHei UI", 9.8F, FontStyle.Bold);
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

                var rect = new RectangleF(1f, 1f, button.Width - 2.5f, button.Height - 2.5f);
                using (var path = RoundRect(rect, 9f))
                {
                    Color fill = primary
                        ? (hovered && button.Enabled ? AccentHover : AccentPrimary)
                        : (hovered && button.Enabled ? RowSelectColor : Color.White);
                    if (!button.Enabled) fill = Color.FromArgb(236, 234, 228);
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);

                    if (!primary)
                    {
                        Color border = hovered && button.Enabled ? AccentPrimary : BorderColor;
                        using (var pen = new Pen(border, 1.1f))
                            g.DrawPath(pen, path);
                    }
                }

                Color fg = !button.Enabled ? Muted : (primary ? Color.White : (hovered ? AccentHover : Ink));
                string raw = button.Text ?? String.Empty;
                bool hasArrow = raw.Contains("→");
                string cleanText = raw.Replace("  →", "").Replace("→", "").Trim();

                if (hasArrow)
                {
                    var textRect = new Rectangle(16, 0, Math.Max(10, button.Width - 44), button.Height);
                    TextRenderer.DrawText(g, cleanText, button.Font, textRect, fg,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    var arrowRect = new Rectangle(Math.Max(0, button.Width - 34), 0, 22, button.Height);
                    using (var arrowFont = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold))
                        TextRenderer.DrawText(g, "→", arrowFont, arrowRect, fg,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }
                else
                {
                    TextRenderer.DrawText(g, cleanText, button.Font, button.ClientRectangle, fg,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
                }
            };
        }

        // GDI+ 抗锯齿圆角工具栏胶囊按钮
        private void StyleSecondaryToolButton(Button button, string text, Padding margin, bool isPreviewBadge = false)
        {
            Color clearBg = isPreviewBadge ? CardBg : PaperBg;
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = clearBg;
            button.ForeColor = InkSub;
            button.Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Regular);
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
                var rect = new RectangleF(1f, 1f, button.Width - 2.5f, button.Height - 2.5f);
                using (var path = RoundRect(rect, 7f))
                {
                    Color fill = isSelectedBadge
                        ? AccentSoft
                        : (hovered && button.Enabled ? RowSelectColor : Color.White);
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);

                    Color border = isSelectedBadge
                        ? Color.FromArgb(245, 206, 190)
                        : (hovered && button.Enabled ? AccentPrimary : BorderColor);
                    using (var pen = new Pen(border, 1f))
                        g.DrawPath(pen, path);
                }

                Color fg = isSelectedBadge
                    ? AccentPrimary
                    : (hovered && button.Enabled ? AccentHover : InkSub);
                var font = isSelectedBadge ? new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold) : button.Font;
                string label = button.Text ?? String.Empty;
                if (label == "刷新") label = "↻  刷新";
                TextRenderer.DrawText(g, label, font, button.ClientRectangle, fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
                if (isSelectedBadge) font.Dispose();
            };
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        // 将原生 ComboBox 放入内层 HWND 裁切面板中，物理裁掉原生 2px 灰框与原生右侧按钮，外层自绘圆角框与精致箭头
        private Panel CreateModernComboContainer(ComboBox combo, bool drawUserBadge, Color parentBg)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = parentBg,
                Padding = new Padding(drawUserBadge ? 34 : 12, 6, 28, 6),
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
                combo.Height = innerClip.Height + 4;
            };
            combo.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                e.DrawBackground();
                using (var bg = new SolidBrush(
                    (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0
                        ? RowSelectColor : Color.White))
                {
                    e.Graphics.FillRectangle(bg, e.Bounds);
                }
                if (e.Index >= 0 && e.Index < combo.Items.Count)
                {
                    string itemText = combo.GetItemText(combo.Items[e.Index]);
                    TextRenderer.DrawText(e.Graphics, itemText, combo.Font,
                        new Rectangle(e.Bounds.X + 2, e.Bounds.Y, e.Bounds.Width - 22, e.Bounds.Height),
                        Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
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
                using (var pen = new Pen(BorderColor, 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
                if (drawUserBadge)
                {
                    var badgeRect = new RectangleF(9f, (host.Height - 20f) / 2f, 20f, 20f);
                    using (var bBrush = new SolidBrush(AccentSoft))
                        g.FillEllipse(bBrush, badgeRect);
                    using (var pPen = new Pen(AccentPrimary, 1.4f))
                    {
                        g.DrawEllipse(pPen, badgeRect.X + 6.5f, badgeRect.Y + 3.8f, 7f, 7f);
                        g.DrawArc(pPen, badgeRect.X + 4f, badgeRect.Y + 11.2f, 12f, 8f, 180, 180);
                    }
                }
                // 右侧自绘现代精致下拉箭头 ⌄
                float ax = host.Width - 16f, ay = host.Height / 2f - 1.5f;
                using (var arrowPen = new Pen(Muted, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
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

        // 将搜索框 TextBox 包裹在带放大镜与占位符提示的圆角白底框中
        private Panel CreateModernSearchContainer(TextBox textBox, Color parentBg)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = parentBg,
                Padding = new Padding(32, 8, 10, 6),
                Margin = new Padding(0, 0, 12, 0),
                Cursor = Cursors.IBeam
            };
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = Color.White;
            textBox.ForeColor = Ink;
            textBox.Dock = DockStyle.Fill;
            textBox.HandleCreated += delegate
            {
                SendMessage(textBox.Handle, 0x1501, new IntPtr(1), "搜索会话标题、ID 或项目名称...");
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
                using (var pen = new Pen(focused ? AccentPrimary : BorderColor, focused ? 1.3f : 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
                // 绘制放大镜图标
                float cx = 15f, cy = host.Height / 2f - 1f;
                using (var iconPen = new Pen(Muted, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawEllipse(iconPen, cx - 5.5f, cy - 5.5f, 9.5f, 9.5f);
                    g.DrawLine(iconPen, cx + 2.2f, cy + 2.2f, cx + 5.8f, cy + 5.8f);
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
            Font = new Font("Microsoft YaHei UI", 9F);
            Size = new Size(1520, 920);
            MinimumSize = new Size(1220, 780);
            BackColor = PaperBg;
            ForeColor = Ink;
            StartPosition = FormStartPosition.CenterScreen;

            // 根网格布局：左侧边栏 (302px) + 右工作区 (自适应)
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = PaperBg
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 302F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            // ==================== 1. 侧边栏容器（对标参考图：浅燕麦底 + 橙色星芒徽标 + 圆角步骤圈） ====================
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
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0)
            };
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 246F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 148F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 138F));
            sidebar.Controls.Add(sidebarLayout);

            // 1.1 品牌头（自绘 44x44 Claude 星芒圆角徽标 + 标题 + 副标题）
            var brandPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            brandPanel.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // 左侧 44x44 珊瑚橙圆角方块 + 白色星芒
                var logoRect = new RectangleF(0f, 4f, 44f, 44f);
                using (var path = RoundRect(logoRect, 11f))
                using (var brush = new SolidBrush(AccentPrimary))
                    g.FillPath(brush, path);

                float cx = 22f, cy = 26f;
                using (var sparkPen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    for (int i = 0; i < 6; i++)
                    {
                        double a = i * Math.PI / 3.0 + 0.22;
                        g.DrawLine(sparkPen, cx - (float)Math.Cos(a) * 11f, cy - (float)Math.Sin(a) * 11f,
                            cx + (float)Math.Cos(a) * 11f, cy + (float)Math.Sin(a) * 11f);
                    }
                }

                using (var tagFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold))
                using (var titleFont = new Font("Microsoft YaHei UI", 21F, FontStyle.Bold))
                using (var subFont = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Regular))
                {
                    TextRenderer.DrawText(g, "CLAUDE CONTINUITY", tagFont, new Point(54, 3), AccentPrimary);
                    TextRenderer.DrawText(g, "Continuity", titleFont, new Point(52, 17), Ink);
                    TextRenderer.DrawText(g, "换个账号，无缝继续手头工作。", subFont, new Point(0, 62), Muted);
                }
            };
            sidebarLayout.Controls.Add(brandPanel, 0, 0);

            // 1.2 目标账号与核心动作卡片
            var actionCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0, 4, 0, 6),
                Padding = new Padding(0)
            };
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            actionCard.Controls.Add(LabelAt("当前 / 目标账号", 9.2F, InkSub, FontStyle.Bold), 0, 0);

            accounts.DropDownStyle = ComboBoxStyle.DropDownList;
            accounts.AccessibleName = "选择目标账号";
            accounts.Font = new Font("Microsoft YaHei UI", 9.5F);
            actionCard.Controls.Add(CreateModernComboContainer(accounts, true, SidebarBg), 0, 1);

            mode.ForeColor = Muted;
            mode.Dock = DockStyle.Fill;
            mode.AutoEllipsis = true;
            mode.Font = new Font("Microsoft YaHei UI", 8.3F);
            mode.Padding = new Padding(2, 2, 0, 4);
            actionCard.Controls.Add(mode, 0, 2);

            StyleButton(switchOpen, "切换账号并接续  →", true);
            actionCard.Controls.Add(switchOpen, 0, 3);

            StyleButton(open, "在当前账号打开", false);
            open.AccessibleName = "导入或打开选中会话";
            actionCard.Controls.Add(open, 0, 4);

            sidebarLayout.Controls.Add(actionCard, 0, 1);

            // 1.3 快速上手（对标参考图：⚡ 快速上手 + 01/02/03 浅珊瑚橙圆形徽章）
            var guideBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 4)
            };
            guideBox.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using (var linePen = new Pen(SidebarBorder, 1f))
                    g.DrawLine(linePen, 0, 2, guideBox.Width, 2);

                using (var headFont = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Bold))
                using (var numFont = new Font("Microsoft YaHei UI", 7.8F, FontStyle.Bold))
                using (var stepFont = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Regular))
                {
                    TextRenderer.DrawText(g, "⚡  快速上手", headFont, new Point(0, 14), Ink);

                    string[] nums = { "01", "02", "03" };
                    string[] steps = {
                        "选择目标账号（或保持当前）",
                        "勾选新会话或单选预览行",
                        "一键唤醒或无损迁移至 Desktop"
                    };
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 44f + i * 31f;
                        var pill = new RectangleF(0f, y, 26f, 20f);
                        using (var path = RoundRect(pill, 10f))
                        using (var bBrush = new SolidBrush(AccentSoft))
                            g.FillPath(bBrush, path);

                        TextRenderer.DrawText(g, nums[i], numFont, Rectangle.Round(pill), AccentPrimary,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        TextRenderer.DrawText(g, steps[i], stepFont, new Point(34, (int)y + 1), InkSub);
                    }
                }
            };
            sidebarLayout.Controls.Add(guideBox, 0, 2);

            // 1.4 底部管理与提示区
            var lowerBox = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            StyleButton(manage, "＋  添加账号 / 备份…", false);
            lowerBox.Controls.Add(manage, 0, 0);

            lowerBox.Controls.Add(LabelAt("通过附带账号管理器管理凭据", 8F, Muted, FontStyle.Regular), 0, 1);
            lowerBox.Controls.Add(LabelAt("ⓘ 跨账号切换会自动重启 Claude Desktop。\n     同账号直接唤醒不会重启。", 8F, Muted, FontStyle.Regular), 0, 2);

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
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            root.Controls.Add(workspace, 1, 0);

            // 2.1 顶部大标题与说明
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Margin = new Padding(0)
            };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            header.Controls.Add(LabelAt("SESSION CONTINUITY LIBRARY  /  本地会话工作区", 8.8F, AccentPrimary, FontStyle.Bold), 0, 0);
            header.Controls.Add(LabelAt("从上次停下的地方继续", 21.5F, Ink, FontStyle.Bold), 0, 1);
            header.Controls.Add(LabelAt("最近 " + settings.activeSessionHours + " 小时内有新对话的会话已预先勾选。点击任意行，可在右侧查阅历史记录。", 9.2F, Muted, FontStyle.Regular), 0, 2);
            workspace.Controls.Add(header, 0, 0);

            // 2.2 筛选栏与快捷勾选操作（圆角下拉框 + 带放大镜圆角搜索框 + 圆角胶囊按钮）
            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(0, 0, 0, 8),
                Margin = new Padding(0)
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

            filters.Controls.Add(LabelAt("项目目录", 8.8F, Muted, FontStyle.Bold), 0, 0);
            filters.Controls.Add(LabelAt("搜索会话标题、ID 或项目名称", 8.8F, Muted, FontStyle.Bold), 1, 0);
            var quickSelectLabel = LabelAt("快捷勾选", 8.8F, Muted, FontStyle.Bold);
            filters.Controls.Add(quickSelectLabel, 2, 0);
            filters.SetColumnSpan(quickSelectLabel, 2);

            project.DropDownStyle = ComboBoxStyle.DropDownList;
            project.Font = new Font("Microsoft YaHei UI", 9.5F);
            var projectHost = CreateModernComboContainer(project, false, PaperBg);
            projectHost.Margin = new Padding(0, 0, 12, 0);
            filters.Controls.Add(projectHost, 0, 1);

            search.AccessibleName = "搜索会话";
            search.Font = new Font("Microsoft YaHei UI", 9.5F);
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

            // 2.3.1 会话表格配置（对标参考图：自绘珊瑚橙圆角复选框 + 文档小图标 + 绿色/暖橙状态胶囊 Pill Badge）
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = CardBg;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(241, 239, 233);
            grid.RowTemplate.Height = 46;
            grid.ColumnHeadersHeight = 40;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = TableHeaderBg,
                ForeColor = Muted,
                Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Bold),
                Padding = new Padding(8, 0, 8, 0)
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Ink,
                SelectionBackColor = RowSelectColor,
                SelectionForeColor = Ink,
                Font = new Font("Microsoft YaHei UI", 9.2F),
                Padding = new Padding(8, 6, 8, 6)
            };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(253, 252, 249);

            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Choose",
                HeaderText = "",
                Width = 44,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            AddColumn("Title", "会话标题", 240);
            AddColumn("Modified", "文件修改", 140);
            AddColumn("Conversation", "最后对话时间", 142);
            AddColumn("Registered", "Desktop 状态", 136);
            AddColumn("Id", "会话 ID", 105);
            AddColumn("Input", "请求输入", 110);

            grid.Columns["Title"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["Title"].MinimumWidth = 160;
            grid.Columns["Modified"].Visible = false;
            grid.Columns["Id"].Visible = false;
            grid.Columns["Input"].Visible = false;
            grid.AccessibleName = "本地会话列表";

            // 自绘表格单元格：珊瑚橙圆角勾选框 + 会话类型图标 + 绿色/暖橙圆角状态胶囊
            grid.CellPainting += delegate(object sender, DataGridViewCellPaintingEventArgs e)
            {
                if (e.ColumnIndex < 0) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                string colName = grid.Columns[e.ColumnIndex].Name;

                // 表头第一列绘制占位复选框装饰
                if (e.RowIndex == -1 && colName == "Choose")
                {
                    e.PaintBackground(e.ClipBounds, false);
                    var box = new RectangleF(e.CellBounds.X + (e.CellBounds.Width - 16f) / 2f,
                        e.CellBounds.Y + (e.CellBounds.Height - 16f) / 2f, 16f, 16f);
                    using (var path = RoundRect(box, 4f))
                    using (var pen = new Pen(Color.FromArgb(195, 191, 182), 1.3f))
                        g.DrawPath(pen, path);
                    e.Handled = true;
                    return;
                }

                if (e.RowIndex < 0) return;

                bool isCheckedRow = Equals(grid.Rows[e.RowIndex].Cells["Choose"].Value, true);
                bool isSelectedRow = (e.State & DataGridViewElementStates.Selected) != 0;
                Color rowBg = (isSelectedRow || isCheckedRow)
                    ? RowSelectColor
                    : (e.RowIndex % 2 == 1 ? Color.FromArgb(253, 252, 249) : Color.White);

                using (var bgBrush = new SolidBrush(rowBg))
                    g.FillRectangle(bgBrush, e.CellBounds);
                using (var linePen = new Pen(grid.GridColor, 1f))
                    g.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                if (colName == "Choose")
                {
                    var box = new RectangleF(e.CellBounds.X + (e.CellBounds.Width - 17f) / 2f,
                        e.CellBounds.Y + (e.CellBounds.Height - 17f) / 2f, 17f, 17f);
                    using (var path = RoundRect(box, 4.5f))
                    {
                        if (isCheckedRow)
                        {
                            using (var fill = new SolidBrush(AccentPrimary))
                                g.FillPath(fill, path);
                            using (var checkPen = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                            {
                                g.DrawLines(checkPen, new[] {
                                    new PointF(box.X + 4f, box.Y + 8.8f),
                                    new PointF(box.X + 7.3f, box.Y + 12.2f),
                                    new PointF(box.X + 13.2f, box.Y + 5.2f)
                                });
                            }
                        }
                        else
                        {
                            using (var fill = new SolidBrush(Color.White))
                                g.FillPath(fill, path);
                            using (var pen = new Pen(Color.FromArgb(196, 192, 183), 1.3f))
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
                    using (var iconBg = new SolidBrush(isDocStyle ? AccentSoft : Color.FromArgb(241, 239, 234)))
                    using (var iconPen = new Pen(isDocStyle ? AccentPrimary : InkSub, 1.3f))
                    {
                        g.FillPath(iconBg, iconPath);
                        g.DrawPath(iconPen, iconPath);
                        g.DrawLine(iconPen, iconX + 4.5f, iconY + 6.5f, iconX + 13.5f, iconY + 6.5f);
                        g.DrawLine(iconPen, iconX + 4.5f, iconY + 10.5f, iconX + 13.5f, iconY + 10.5f);
                        g.DrawLine(iconPen, iconX + 4.5f, iconY + 14.5f, iconX + 10.5f, iconY + 14.5f);
                    }

                    var textBounds = new Rectangle(e.CellBounds.X + 34, e.CellBounds.Y, e.CellBounds.Width - 42, e.CellBounds.Height);
                    var font = isCheckedRow ? new Font(e.CellStyle.Font, FontStyle.Bold) : e.CellStyle.Font;
                    TextRenderer.DrawText(g, titleText, font, textBounds, Ink,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    if (isCheckedRow) font.Dispose();
                    e.Handled = true;
                }
                else if (colName == "Registered")
                {
                    string stateText = Convert.ToString(e.FormattedValue) ?? String.Empty;
                    bool isReg = stateText.StartsWith("已登记");
                    bool isMissing = stateText.Contains("缺失");

                    Color pillBg = isMissing ? PillGrayBg : (isReg ? PillGreenBg : PillAmberBg);
                    Color pillFg = isMissing ? PillGrayFg : (isReg ? PillGreenFg : PillAmberFg);

                    using (var badgeFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Bold))
                    {
                        Size textSz = TextRenderer.MeasureText(g, stateText, badgeFont);
                        float pillW = Math.Min(e.CellBounds.Width - 12f, textSz.Width + 16f);
                        float pillH = 24f;
                        float pillX = e.CellBounds.X + 6f;
                        float pillY = e.CellBounds.Y + (e.CellBounds.Height - pillH) / 2f;
                        var pillRect = new RectangleF(pillX, pillY, pillW, pillH);

                        using (var path = RoundRect(pillRect, 6f))
                        using (var fill = new SolidBrush(pillBg))
                            g.FillPath(fill, path);

                        TextRenderer.DrawText(g, stateText, badgeFont, Rectangle.Round(pillRect), pillFg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                    e.Handled = true;
                }
                else
                {
                    string val = Convert.ToString(e.FormattedValue) ?? String.Empty;
                    var textBounds = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                    TextRenderer.DrawText(g, val, e.CellStyle.Font, textBounds, InkSub,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
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
                using (var pen = new Pen(BorderColor, 1f))
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
                using (var pen = new Pen(BorderColor, 1f))
                    e.Graphics.DrawPath(pen, path);
            };
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            content.Panel2.Controls.Add(previewLayout);

            var previewTopBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            previewTopBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            previewTopBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
            previewTopBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var previewBadgeHeader = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            previewBadgeHeader.Paint += delegate(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var iconBox = new RectangleF(0f, 3f, 22f, 22f);
                using (var path = RoundRect(iconBox, 5f))
                using (var brush = new SolidBrush(AccentPrimary))
                    g.FillPath(brush, path);
                using (var pen = new Pen(Color.White, 1.4f))
                {
                    g.DrawLine(pen, 5.5f, 9.5f, 16.5f, 9.5f);
                    g.DrawLine(pen, 5.5f, 13.5f, 16.5f, 13.5f);
                    g.DrawLine(pen, 5.5f, 17.5f, 12.5f, 17.5f);
                }
                using (var f = new Font("Microsoft YaHei UI", 8.1F, FontStyle.Bold))
                    TextRenderer.DrawText(g, "CONVERSATION TRANSCRIPT / 最近对话预览", f, new Point(28, 5), AccentPrimary);
            };
            previewTopBar.Controls.Add(previewBadgeHeader, 0, 0);

            StyleSecondaryToolButton(togglePreviewed, "仅选此条会话", new Padding(0, 1, 0, 1), true);
            togglePreviewed.AccessibleName = "仅选当前预览会话";
            previewTopBar.Controls.Add(togglePreviewed, 1, 0);
            previewLayout.Controls.Add(previewTopBar, 0, 0);

            previewHeading.Dock = DockStyle.Fill;
            previewHeading.ForeColor = Ink;
            previewHeading.Font = new Font("Microsoft YaHei UI", 9.8F, FontStyle.Bold);
            previewHeading.AutoEllipsis = true;
            previewHeading.Text = "点选一条会话\n在此处查看对话上下文与执行进度。";
            previewHeading.Margin = new Padding(0);
            previewLayout.Controls.Add(previewHeading, 0, 1);

            preview.Dock = DockStyle.Fill;
            preview.ReadOnly = true;
            preview.DetectUrls = false;
            preview.WordWrap = true;
            preview.BorderStyle = BorderStyle.None;
            preview.BackColor = CardBg;
            preview.ScrollBars = RichTextBoxScrollBars.Vertical;
            preview.AccessibleName = "最近对话预览";
            preview.Font = new Font("Microsoft YaHei UI", 9.2F);
            preview.Margin = new Padding(0, 4, 0, 4);
            previewLayout.Controls.Add(preview, 0, 2);

            previewLayout.Controls.Add(LabelAt("ⓘ 只读预览 · 本地安全解析 · 不改写原始会话记录", 8F, Muted, FontStyle.Regular), 0, 3);

            // 2.4 状态栏
            var statusPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            status.Dock = DockStyle.Fill;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.ForeColor = Muted;
            status.Font = new Font("Microsoft YaHei UI", 8.5F);
            status.AutoEllipsis = true;
            statusPanel.Controls.Add(status);
            workspace.Controls.Add(statusPanel, 0, 3);

            ResumeLayout(true);
        }
    }
}
