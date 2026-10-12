using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClaudeSessionImporter
{
    public sealed partial class Picker
    {
        private static readonly Color Ink = Color.FromArgb(32, 48, 46);
        private static readonly Color Paper = Color.FromArgb(247, 246, 242);
        private static readonly Color Accent = Color.FromArgb(37, 106, 84);
        private static readonly Color Muted = Color.FromArgb(111, 121, 115);
        private Label LabelAt(string text, float size, Color color, FontStyle weight)
        {
            return new Label { Text = text, Font = new Font("Microsoft YaHei UI", size, weight), ForeColor = color,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = new Padding(0) };
        }
        private void StyleButton(Button button, string text, bool primary)
        {
            button.Text = text; button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(206, 214, 208);
            button.BackColor = primary ? Accent : Color.White; button.ForeColor = primary ? Color.White : Ink;
            button.Font = new Font(Font.FontFamily, 10, FontStyle.Bold); button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 4, 0, 4); button.Cursor = Cursors.Hand;
        }
        private void BuildLayout()
        {
            SuspendLayout(); Text = "Continuity · Claude 账号与会话";
            Font = new Font("Microsoft YaHei UI", 9); Size = new Size(1500, 900); MinimumSize = new Size(1200, 760);
            BackColor = Paper; ForeColor = Ink; StartPosition = FormStartPosition.CenterScreen;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 278)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(root);
            var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Ink, Padding = new Padding(26, 28, 26, 24), ColumnCount = 1, RowCount = 14 };
            int[] heights = { 24, 52, 46, 32, 28, 46, 66, 40, 58, 52, 16, 52, 20 };
            foreach (int height in heights) sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, height)); sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.Controls.Add(sidebar, 0, 0);
            sidebar.Controls.Add(LabelAt("LOCAL WORKSPACE", 8, Color.FromArgb(165, 187, 176), FontStyle.Bold), 0, 0);
            sidebar.Controls.Add(LabelAt("Continuity", 26, Color.White, FontStyle.Bold), 0, 1);
            sidebar.Controls.Add(LabelAt("换个账号，继续手里的工作。", 10, Color.FromArgb(208, 219, 212), FontStyle.Regular), 0, 2);
            sidebar.Controls.Add(LabelAt("接续到", 10, Color.White, FontStyle.Bold), 0, 4);
            accounts.DropDownStyle = ComboBoxStyle.DropDownList; accounts.Dock = DockStyle.Fill;
            accounts.AccessibleName = "选择目标账号"; accounts.Margin = new Padding(0, 6, 0, 0); sidebar.Controls.Add(accounts, 0, 5);
            mode.ForeColor = Color.FromArgb(187, 207, 196); mode.Dock = DockStyle.Fill; mode.AutoEllipsis = true; mode.Padding = new Padding(0, 12, 0, 0); sidebar.Controls.Add(mode, 0, 6);
            sidebar.Controls.Add(LabelAt("01  选择账号\n02  勾选会话\n03  继续工作", 10, Color.FromArgb(208, 219, 212), FontStyle.Regular), 0, 8);
            StyleButton(switchOpen, "切换账号并接续  →", true); sidebar.Controls.Add(switchOpen, 0, 9);
            StyleButton(open, "在当前账号打开", false); open.AccessibleName = "导入或打开选中会话"; sidebar.Controls.Add(open, 0, 11);
            var lower = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 120, RowCount = 3 };
            lower.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); lower.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); lower.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            StyleButton(manage, "添加账号 / 备份…", false); lower.Controls.Add(manage, 0, 0);
            lower.Controls.Add(LabelAt("通过附带的账号管理器完成", 8, Color.FromArgb(165, 187, 176), FontStyle.Regular), 0, 1);
            lower.Controls.Add(LabelAt("切换会重启 Claude Desktop。\n请先完成正在运行的任务。", 8, Color.FromArgb(165, 187, 176), FontStyle.Regular), 0, 2); sidebar.Controls.Add(lower, 0, 13);
            var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 18), RowCount = 4, ColumnCount = 1 };
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 108)); workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); root.Controls.Add(workspace, 1, 0);
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); header.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); header.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            header.Controls.Add(LabelAt("SESSION LIBRARY  /  本地会话", 9, Accent, FontStyle.Bold), 0, 0);
            header.Controls.Add(LabelAt("从上次停下的地方继续", 23, Ink, FontStyle.Bold), 0, 1);
            header.Controls.Add(LabelAt("最近 " + settings.activeSessionHours + " 小时有对话的会话已默认勾选。点选一行，先看看聊到了哪里。", 10, Muted, FontStyle.Regular), 0, 2); workspace.Controls.Add(header, 0, 0);
            var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(0, 0, 0, 12) };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            filters.Controls.Add(LabelAt("项目", 9, Muted, FontStyle.Regular), 0, 0); filters.Controls.Add(LabelAt("搜索标题或会话 ID", 9, Muted, FontStyle.Regular), 1, 0);
            project.DropDownStyle = ComboBoxStyle.DropDownList; project.Dock = DockStyle.Fill; project.Margin = new Padding(0, 0, 14, 0); filters.Controls.Add(project, 0, 1);
            search.Dock = DockStyle.Fill; search.AccessibleName = "搜索会话"; search.Margin = new Padding(0, 0, 14, 0); filters.Controls.Add(search, 1, 1);
            StyleButton(refresh, "刷新", false); refresh.Margin = new Padding(0); filters.Controls.Add(refresh, 2, 1); workspace.Controls.Add(filters, 0, 1);
            var content = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, Size = new Size(1120, 600),
                SplitterDistance = 675, SplitterWidth = 18, Panel1MinSize = 410, Panel2MinSize = 285, BackColor = Paper };
            workspace.Controls.Add(content, 0, 2);
            grid.Dock = DockStyle.Fill; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false;
            grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None; grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(236, 239, 234); grid.RowTemplate.Height = 56; grid.ColumnHeadersHeight = 40; grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(235, 239, 232), ForeColor = Muted, Font = new Font(Font.FontFamily, 9, FontStyle.Bold) };
            grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Ink, SelectionBackColor = Color.FromArgb(221, 237, 223), SelectionForeColor = Ink, Padding = new Padding(5, 8, 5, 8) };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(251, 252, 249);
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Choose", HeaderText = "", Width = 36, SortMode = DataGridViewColumnSortMode.NotSortable });
            AddColumn("Title", "会话", 230); AddColumn("Modified", "文件修改", 140); AddColumn("Conversation", "最后对话", 140);
            AddColumn("Registered", "当前账号", 130); AddColumn("Id", "会话 ID", 105); AddColumn("Input", "请求输入", 110);
            grid.Columns["Title"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; grid.Columns["Title"].MinimumWidth = 150;
            grid.Columns["Modified"].Visible = false; grid.Columns["Id"].Visible = false; grid.Columns["Input"].Visible = false;
            grid.AccessibleName = "本地会话列表"; content.Panel1.Controls.Add(grid);
            var previewLayout = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 12, 20, 14), ColumnCount = 1, RowCount = 4 };
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); content.Panel2.Controls.Add(previewLayout);
            previewLayout.Controls.Add(LabelAt("CONVERSATION  /  对话预览", 9, Accent, FontStyle.Bold), 0, 0);
            previewHeading.Dock = DockStyle.Fill; previewHeading.ForeColor = Ink; previewHeading.Font = new Font(Font.FontFamily, 10, FontStyle.Bold); previewHeading.AutoEllipsis = true;
            previewHeading.Text = "点选一条会话\n在这里查看最近的对话。"; previewLayout.Controls.Add(previewHeading, 0, 1);
            preview.Dock = DockStyle.Fill; preview.ReadOnly = true; preview.DetectUrls = false; preview.WordWrap = true; preview.BorderStyle = BorderStyle.None;
            preview.BackColor = Color.White; preview.ScrollBars = RichTextBoxScrollBars.Vertical; preview.AccessibleName = "最近对话预览"; previewLayout.Controls.Add(preview, 0, 2);
            previewLayout.Controls.Add(LabelAt("只读预览 · 原始会话记录保持完整", 8, Muted, FontStyle.Regular), 0, 3);
            status.Dock = DockStyle.Fill; status.TextAlign = ContentAlignment.MiddleLeft; status.ForeColor = Muted; status.AutoEllipsis = true; workspace.Controls.Add(status, 0, 3);
            ResumeLayout(true);
        }
    }
}
