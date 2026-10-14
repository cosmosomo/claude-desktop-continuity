using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClaudeSessionImporter
{
    public sealed partial class Picker
    {
        // 视觉色彩系统（Claude Official Warm Ivory + Oatmeal + Terracotta Coral）
        private static readonly Color SidebarBg = Color.FromArgb(240, 238, 230);      // #F0EEE6 燕麦浅暖灰侧栏
        private static readonly Color SidebarCard = Color.FromArgb(230, 227, 216);    // #E6E3D8 暖沙卡片底色
        private static readonly Color SidebarBorder = Color.FromArgb(216, 212, 200);  // #D8D4C8 侧栏细边框
        private static readonly Color PaperBg = Color.FromArgb(250, 249, 245);        // #FAF9F5 Claude 标志性暖白画布
        private static readonly Color CardBg = Color.White;                           // #FFFFFF 纯白内容卡片
        private static readonly Color Ink = Color.FromArgb(20, 20, 19);               // #141413 Anthropic 暖炭黑
        private static readonly Color InkSub = Color.FromArgb(61, 57, 41);            // #3D3929 暖深褐副标题
        private static readonly Color Muted = Color.FromArgb(124, 120, 106);          // #7C786A 暖石灰辅助字
        private static readonly Color AccentPrimary = Color.FromArgb(217, 119, 87);   // #D97757 Claude 陶土珊瑚色 (Crail)
        private static readonly Color AccentHover = Color.FromArgb(198, 97, 63);      // #C6613F 陶土深色悬停
        private static readonly Color BorderColor = Color.FromArgb(230, 228, 221);    // #E6E4DD 暖调微边框
        private static readonly Color RowSelectColor = Color.FromArgb(253, 242, 237); // #FDF2ED 柔和浅蜜桃选中态
        private static readonly Color TableHeaderBg = Color.FromArgb(244, 242, 236);  // #F4F2EC 燕麦表头底色

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

        private void StyleButton(Button button, string text, bool primary)
        {
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = primary ? AccentPrimary : SidebarBorder;
            button.BackColor = primary ? AccentPrimary : PaperBg;
            button.ForeColor = primary ? Color.White : InkSub;
            button.Font = new Font("Microsoft YaHei UI", 9.8F, FontStyle.Bold);
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 4, 0, 4);
            button.Cursor = Cursors.Hand;
            button.MouseEnter += delegate
            {
                if (button.Enabled)
                {
                    button.BackColor = primary ? AccentHover : RowSelectColor;
                    if (!primary) button.FlatAppearance.BorderColor = AccentPrimary;
                }
            };
            button.MouseLeave += delegate
            {
                if (button.Enabled)
                {
                    button.BackColor = primary ? AccentPrimary : PaperBg;
                    if (!primary) button.FlatAppearance.BorderColor = SidebarBorder;
                }
            };
        }

        private void StyleSecondaryToolButton(Button button, string text, Padding margin)
        {
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = BorderColor;
            button.BackColor = Color.White;
            button.ForeColor = InkSub;
            button.Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Regular);
            button.Dock = DockStyle.Fill;
            button.Margin = margin;
            button.Cursor = Cursors.Hand;
            button.MouseEnter += delegate
            {
                if (button.Enabled)
                {
                    button.BackColor = RowSelectColor;
                    button.ForeColor = AccentHover;
                    button.FlatAppearance.BorderColor = AccentPrimary;
                }
            };
            button.MouseLeave += delegate
            {
                if (button.Enabled)
                {
                    button.BackColor = Color.White;
                    button.ForeColor = InkSub;
                    button.FlatAppearance.BorderColor = BorderColor;
                }
            };
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

            // 根网格布局：左侧边栏 (296px) + 右工作区 (自适应)
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = PaperBg
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 296F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            // ==================== 1. 侧边栏容器（Claude 燕麦暖色风） ====================
            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarBg,
                Padding = new Padding(24, 28, 24, 24),
                Margin = new Padding(0)
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
            // 品牌头(96px), 账号操作卡片(236px), 步骤提示(94px), 弹性空间(100%), 底部操作(140px)
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 236F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            sidebar.Controls.Add(sidebarLayout);

            // 1.1 品牌头
            var brandBox = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
            brandBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            brandBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            brandBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            brandBox.Controls.Add(LabelAt("CLAUDE CONTINUITY", 8F, AccentPrimary, FontStyle.Bold), 0, 0);
            brandBox.Controls.Add(LabelAt("Continuity", 25F, Ink, FontStyle.Bold), 0, 1);
            brandBox.Controls.Add(LabelAt("换个账号，无缝继续手头工作。", 9.5F, Muted, FontStyle.Regular), 0, 2);
            sidebarLayout.Controls.Add(brandBox, 0, 0);

            // 1.2 目标账号与核心动作卡片
            var actionCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0, 8, 0, 8),
                Padding = new Padding(0)
            };
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            actionCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            actionCard.Controls.Add(LabelAt("目标账号", 9.5F, InkSub, FontStyle.Bold), 0, 0);

            accounts.DropDownStyle = ComboBoxStyle.DropDownList;
            accounts.Dock = DockStyle.Fill;
            accounts.AccessibleName = "选择目标账号";
            accounts.BackColor = Color.White;
            accounts.ForeColor = Ink;
            accounts.Font = new Font("Microsoft YaHei UI", 9.5F);
            accounts.Margin = new Padding(0, 2, 0, 6);
            actionCard.Controls.Add(accounts, 0, 1);

            mode.ForeColor = Muted;
            mode.Dock = DockStyle.Fill;
            mode.AutoEllipsis = true;
            mode.Font = new Font("Microsoft YaHei UI", 8.5F);
            mode.Padding = new Padding(0, 4, 0, 6);
            actionCard.Controls.Add(mode, 0, 2);

            StyleButton(switchOpen, "切换账号并接续  →", true);
            actionCard.Controls.Add(switchOpen, 0, 3);

            StyleButton(open, "在当前账号打开", false);
            open.AccessibleName = "导入或打开选中会话";
            actionCard.Controls.Add(open, 0, 4);

            sidebarLayout.Controls.Add(actionCard, 0, 1);

            // 1.3 流程引导卡片
            var guideBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarCard,
                Padding = new Padding(14, 12, 14, 12),
                Margin = new Padding(0, 6, 0, 6)
            };
            var guideLabel = LabelAt("01 选择目标账号（或保持当前）\n02 勾选新会话或单选预览行\n03 一键唤醒或无损迁移至 Desktop", 8.8F, InkSub, FontStyle.Regular);
            guideLabel.Dock = DockStyle.Fill;
            guideBox.Controls.Add(guideLabel);
            sidebarLayout.Controls.Add(guideBox, 0, 2);

            // 1.4 底部管理与提示区
            var lowerBox = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            lowerBox.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            StyleButton(manage, "添加账号 / 备份…", false);
            lowerBox.Controls.Add(manage, 0, 0);

            lowerBox.Controls.Add(LabelAt("通过附带账号管理器管理凭据", 8F, Muted, FontStyle.Regular), 0, 1);
            lowerBox.Controls.Add(LabelAt("跨账号切换会自动重启 Claude Desktop。\n同账号直接唤醒不会重启。", 8F, Muted, FontStyle.Regular), 0, 2);

            sidebarLayout.Controls.Add(lowerBox, 0, 4);

            // ==================== 2. 主工作区 ====================
            var workspace = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(32, 26, 32, 20),
                RowCount = 4,
                ColumnCount = 1,
                BackColor = PaperBg,
                Margin = new Padding(0)
            };
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
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
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            header.Controls.Add(LabelAt("SESSION CONTINUITY LIBRARY  /  本地会话工作区", 9F, AccentPrimary, FontStyle.Bold), 0, 0);
            header.Controls.Add(LabelAt("从上次停下的地方继续", 22F, Ink, FontStyle.Bold), 0, 1);
            header.Controls.Add(LabelAt("最近 " + settings.activeSessionHours + " 小时内有新对话的会话已预先勾选。点击任意行，可在右侧查阅历史记录。", 9.5F, Muted, FontStyle.Regular), 0, 2);
            workspace.Controls.Add(header, 0, 0);

            // 2.2 筛选栏与快捷勾选操作
            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(0, 0, 0, 8),
                Margin = new Padding(0)
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            filters.Controls.Add(LabelAt("项目目录", 9F, Muted, FontStyle.Bold), 0, 0);
            filters.Controls.Add(LabelAt("搜索会话标题、ID 或项目名称", 9F, Muted, FontStyle.Bold), 1, 0);
            var quickSelectLabel = LabelAt("快捷勾选", 9F, Muted, FontStyle.Bold);
            filters.Controls.Add(quickSelectLabel, 2, 0);
            filters.SetColumnSpan(quickSelectLabel, 2);

            project.DropDownStyle = ComboBoxStyle.DropDownList;
            project.Dock = DockStyle.Fill;
            project.Font = new Font("Microsoft YaHei UI", 9.5F);
            project.Margin = new Padding(0, 0, 14, 0);
            filters.Controls.Add(project, 0, 1);

            search.Dock = DockStyle.Fill;
            search.AccessibleName = "搜索会话";
            search.Font = new Font("Microsoft YaHei UI", 9.5F);
            search.Margin = new Padding(0, 0, 14, 0);
            filters.Controls.Add(search, 1, 1);

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
                SplitterDistance = 680,
                SplitterWidth = 16,
                Panel1MinSize = 420,
                Panel2MinSize = 300,
                BackColor = PaperBg
            };
            workspace.Controls.Add(content, 0, 2);

            // 2.3.1 会话表格配置
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = CardBg;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(240, 238, 232);
            grid.RowTemplate.Height = 48;
            grid.ColumnHeadersHeight = 38;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = TableHeaderBg,
                ForeColor = InkSub,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Padding = new Padding(8, 0, 8, 0)
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Ink,
                SelectionBackColor = RowSelectColor,
                SelectionForeColor = Ink,
                Font = new Font("Microsoft YaHei UI", 9F),
                Padding = new Padding(8, 6, 8, 6)
            };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 251, 248);

            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Choose",
                HeaderText = "",
                Width = 38,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            AddColumn("Title", "会话标题", 240);
            AddColumn("Modified", "文件修改", 140);
            AddColumn("Conversation", "最后对话时间", 145);
            AddColumn("Registered", "Desktop 状态", 125);
            AddColumn("Id", "会话 ID", 105);
            AddColumn("Input", "请求输入", 110);

            grid.Columns["Title"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["Title"].MinimumWidth = 160;
            grid.Columns["Modified"].Visible = false;
            grid.Columns["Id"].Visible = false;
            grid.Columns["Input"].Visible = false;
            grid.AccessibleName = "本地会话列表";

            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BorderColor,
                Padding = new Padding(1),
                Margin = new Padding(0)
            };
            gridCard.Controls.Add(grid);
            content.Panel1.Controls.Add(gridCard);

            // 2.3.2 预览面板 (严格保留 TableLayoutPanel 4 行结构以保证 RenderWorkspace 兼容性)
            var previewLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                Padding = new Padding(22, 16, 22, 16),
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0)
            };
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
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
            previewTopBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
            previewTopBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewTopBar.Controls.Add(LabelAt("CONVERSATION TRANSCRIPT  /  最近对话预览", 8.5F, AccentPrimary, FontStyle.Bold), 0, 0);
            StyleSecondaryToolButton(togglePreviewed, "仅选此条会话", new Padding(0, 1, 0, 1));
            togglePreviewed.AccessibleName = "仅选当前预览会话";
            previewTopBar.Controls.Add(togglePreviewed, 1, 0);
            previewLayout.Controls.Add(previewTopBar, 0, 0);

            previewHeading.Dock = DockStyle.Fill;
            previewHeading.ForeColor = Ink;
            previewHeading.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
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
            preview.Font = new Font("Microsoft YaHei UI", 9F);
            preview.Margin = new Padding(0, 4, 0, 4);
            previewLayout.Controls.Add(preview, 0, 2);

            previewLayout.Controls.Add(LabelAt("只读预览 · 本地安全解析 · 不改写原始会话记录", 8F, Muted, FontStyle.Regular), 0, 3);

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
