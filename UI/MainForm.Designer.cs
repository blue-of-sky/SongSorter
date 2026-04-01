namespace SongConverter.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.tabControl = new System.Windows.Forms.TabControl();
        this.tabSongSorter = new System.Windows.Forms.TabPage();
        this.tabDanGenerator = new System.Windows.Forms.TabPage();
        this.tabAddSongs = new System.Windows.Forms.TabPage();
        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
        this.logBox = new System.Windows.Forms.TextBox();

        // Song Sorter Tab Controls
        this.btnOrganize = new System.Windows.Forms.Button();
        this.btnFetchLists = new System.Windows.Forms.Button();
        this.lblTempSongs = new System.Windows.Forms.Label();
        this.txtTempSongs = new System.Windows.Forms.TextBox();
        this.btnBrowseTemp = new System.Windows.Forms.Button();
        this.lblTaikoRoot = new System.Windows.Forms.Label();
        this.txtTaikoRoot = new System.Windows.Forms.TextBox();
        this.btnBrowseRoot = new System.Windows.Forms.Button();

        // Dan Generator Tab Controls
        this.lblWikiUrl = new System.Windows.Forms.Label();
        this.txtWikiUrl = new System.Windows.Forms.TextBox();
        this.lblDanOutput = new System.Windows.Forms.Label();
        this.txtDanOutputSub = new System.Windows.Forms.TextBox();
        this.lblDanSongs = new System.Windows.Forms.Label();
        this.txtDanSongsPath = new System.Windows.Forms.TextBox();
        this.btnBrowseDanSongs = new System.Windows.Forms.Button();
        this.btnGenerateDan = new System.Windows.Forms.Button();

        // Add Songs Tab Controls
        this.lblAddSongsFolder = new System.Windows.Forms.Label();
        this.txtAddSongsFolder = new System.Windows.Forms.TextBox();
        this.btnBrowseAddSongsFolder = new System.Windows.Forms.Button();
        this.btnExecuteAddSongs = new System.Windows.Forms.Button();

        this.tabControl.SuspendLayout();
        this.tabSongSorter.SuspendLayout();
        this.tabDanGenerator.SuspendLayout();
        this.tabAddSongs.SuspendLayout();
        this.statusStrip.SuspendLayout();
        this.SuspendLayout();

        // tabControl
        this.tabControl.Controls.Add(this.tabAddSongs);
        this.tabControl.Controls.Add(this.tabSongSorter);
        this.tabControl.Controls.Add(this.tabDanGenerator);
        this.tabControl.Dock = System.Windows.Forms.DockStyle.Top;
        this.tabControl.Location = new System.Drawing.Point(0, 0);
        this.tabControl.Name = "tabControl";
        this.tabControl.SelectedIndex = 0;
        this.tabControl.Size = new System.Drawing.Size(684, 320);
        this.tabControl.TabIndex = 0;

        // tabSongSorter
        this.tabSongSorter.BackColor = System.Drawing.Color.White;
        this.tabSongSorter.Controls.Add(this.btnBrowseRoot);
        this.tabSongSorter.Controls.Add(this.txtTaikoRoot);
        this.tabSongSorter.Controls.Add(this.lblTaikoRoot);
        this.tabSongSorter.Controls.Add(this.btnBrowseTemp);
        this.tabSongSorter.Controls.Add(this.txtTempSongs);
        this.tabSongSorter.Controls.Add(this.lblTempSongs);
        this.tabSongSorter.Controls.Add(this.btnFetchLists);
        this.tabSongSorter.Controls.Add(this.btnOrganize);
        this.tabSongSorter.Location = new System.Drawing.Point(4, 24);
        this.tabSongSorter.Name = "tabSongSorter";
        this.tabSongSorter.Padding = new System.Windows.Forms.Padding(15);
        this.tabSongSorter.Size = new System.Drawing.Size(676, 292);
        this.tabSongSorter.TabIndex = 1;
        this.tabSongSorter.Text = "SongSorter";

        // tabDanGenerator
        this.tabDanGenerator.BackColor = System.Drawing.Color.White;
        this.tabDanGenerator.Controls.Add(this.btnGenerateDan);
        this.tabDanGenerator.Controls.Add(this.btnBrowseDanSongs);
        this.tabDanGenerator.Controls.Add(this.txtDanSongsPath);
        this.tabDanGenerator.Controls.Add(this.lblDanSongs);
        this.tabDanGenerator.Controls.Add(this.txtDanOutputSub);
        this.tabDanGenerator.Controls.Add(this.lblDanOutput);
        this.tabDanGenerator.Controls.Add(this.txtWikiUrl);
        this.tabDanGenerator.Controls.Add(this.lblWikiUrl);
        this.tabDanGenerator.Location = new System.Drawing.Point(4, 24);
        this.tabDanGenerator.Name = "tabDanGenerator";
        this.tabDanGenerator.Padding = new System.Windows.Forms.Padding(15);
        this.tabDanGenerator.Size = new System.Drawing.Size(676, 292);
        this.tabDanGenerator.TabIndex = 2;
        this.tabDanGenerator.Text = "DanGenerator";

        // tabAddSongs
        this.tabAddSongs.BackColor = System.Drawing.Color.White;
        this.tabAddSongs.Controls.Add(this.btnExecuteAddSongs);
        this.tabAddSongs.Controls.Add(this.btnBrowseAddSongsFolder);
        this.tabAddSongs.Controls.Add(this.txtAddSongsFolder);
        this.tabAddSongs.Controls.Add(this.lblAddSongsFolder);
        this.tabAddSongs.Location = new System.Drawing.Point(4, 24);
        this.tabAddSongs.Name = "tabAddSongs";
        this.tabAddSongs.Padding = new System.Windows.Forms.Padding(15);
        this.tabAddSongs.Size = new System.Drawing.Size(676, 292);
        this.tabAddSongs.TabIndex = 0;
        this.tabAddSongs.Text = "AddSongs";

        // lblAddSongsFolder
        this.lblAddSongsFolder.Location = new System.Drawing.Point(20, 20);
        this.lblAddSongsFolder.Text = "楽曲をダウンロードするフォルダを選択:";
        this.lblAddSongsFolder.Size = new System.Drawing.Size(400, 20);

        this.txtAddSongsFolder.Location = new System.Drawing.Point(20, 45);
        this.txtAddSongsFolder.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseAddSongsFolder.Location = new System.Drawing.Point(560, 43);
        this.btnBrowseAddSongsFolder.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseAddSongsFolder.Text = "参照...";

        this.btnExecuteAddSongs.Location = new System.Drawing.Point(20, 100);
        this.btnExecuteAddSongs.Size = new System.Drawing.Size(200, 45);
        this.btnExecuteAddSongs.Text = "曲追加実行";
        this.btnExecuteAddSongs.BackColor = System.Drawing.Color.FromArgb(0, 153, 255);
        this.btnExecuteAddSongs.ForeColor = System.Drawing.Color.White;
        this.btnExecuteAddSongs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnExecuteAddSongs.Font = new System.Drawing.Font("Noto Sans", 10F, System.Drawing.FontStyle.Bold);

        // logBox
        this.logBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        this.logBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.logBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.logBox.Font = new System.Drawing.Font("Consolas", 9F);
        this.logBox.ForeColor = System.Drawing.Color.LightGray;
        this.logBox.Location = new System.Drawing.Point(0, 320);
        this.logBox.Multiline = true;
        this.logBox.Name = "logBox";
        this.logBox.ReadOnly = true;
        this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.logBox.Size = new System.Drawing.Size(684, 279);
        this.logBox.TabIndex = 1;

        // lblTempSongs
        this.lblTempSongs.Location = new System.Drawing.Point(20, 20);
        this.lblTempSongs.Text = "コピー元のSongsフォルダを選択:";
        this.lblTempSongs.Size = new System.Drawing.Size(400, 20);

        this.txtTempSongs.Location = new System.Drawing.Point(20, 45);
        this.txtTempSongs.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseTemp.Location = new System.Drawing.Point(560, 43);
        this.btnBrowseTemp.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseTemp.Text = "参照...";

        // lblTaikoRoot
        this.lblTaikoRoot.Location = new System.Drawing.Point(20, 90);
        this.lblTaikoRoot.Text = "コピー先のシミュフォルダを選択:";
        this.lblTaikoRoot.Size = new System.Drawing.Size(400, 20);

        this.txtTaikoRoot.Location = new System.Drawing.Point(20, 115);
        this.txtTaikoRoot.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseRoot.Location = new System.Drawing.Point(560, 113);
        this.btnBrowseRoot.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseRoot.Text = "参照...";

        this.btnFetchLists.Location = new System.Drawing.Point(20, 170);
        this.btnFetchLists.Size = new System.Drawing.Size(150, 45);
        this.btnFetchLists.Text = "曲名リスト更新";
        this.btnFetchLists.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
        this.btnFetchLists.ForeColor = System.Drawing.Color.White;
        this.btnFetchLists.FlatStyle = System.Windows.Forms.FlatStyle.Flat;

        // btnOrganize
        this.btnOrganize.Location = new System.Drawing.Point(180, 170);
        this.btnOrganize.Size = new System.Drawing.Size(180, 45);
        this.btnOrganize.Text = "並び替え開始";
        this.btnOrganize.BackColor = System.Drawing.Color.FromArgb(0, 153, 255);
        this.btnOrganize.ForeColor = System.Drawing.Color.White;
        this.btnOrganize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnOrganize.Font = new System.Drawing.Font("Noto Sans", 10F, System.Drawing.FontStyle.Bold);

        // Dan Generator UI elements
        this.lblWikiUrl.Location = new System.Drawing.Point(20, 20);
        this.lblWikiUrl.Text = "太鼓Wikiの段位URL:";
        this.lblWikiUrl.Size = new System.Drawing.Size(250, 20);

        this.txtWikiUrl.Location = new System.Drawing.Point(20, 45);
        this.txtWikiUrl.Size = new System.Drawing.Size(630, 23);
        this.txtWikiUrl.Text = "";

        this.lblDanOutput.Location = new System.Drawing.Point(20, 90);
        this.lblDanOutput.Text = "出力フォルダ名:";
        this.lblDanOutput.Size = new System.Drawing.Size(200, 20);

        this.txtDanOutputSub.Location = new System.Drawing.Point(20, 115);
        this.txtDanOutputSub.Size = new System.Drawing.Size(250, 23);
        this.txtDanOutputSub.Text = "今段位";

        this.lblDanSongs.Location = new System.Drawing.Point(20, 160);
        this.lblDanSongs.Text = "シミュフォルダを選択:";
        this.lblDanSongs.Size = new System.Drawing.Size(200, 20);

        this.txtDanSongsPath.Location = new System.Drawing.Point(20, 185);
        this.txtDanSongsPath.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseDanSongs.Location = new System.Drawing.Point(560, 183);
        this.btnBrowseDanSongs.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseDanSongs.Text = "参照...";

        this.btnGenerateDan.Location = new System.Drawing.Point(20, 230);
        this.btnGenerateDan.Size = new System.Drawing.Size(200, 45);
        this.btnGenerateDan.Text = "段位生成";
        this.btnGenerateDan.BackColor = System.Drawing.Color.FromArgb(0, 153, 255);
        this.btnGenerateDan.ForeColor = System.Drawing.Color.White;
        this.btnGenerateDan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnGenerateDan.Font = new System.Drawing.Font("Noto Sans", 10F, System.Drawing.FontStyle.Bold);

        // statusStrip
        this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.statusLabel, this.progressBar });
        this.statusStrip.Location = new System.Drawing.Point(0, 599);
        this.statusStrip.Name = "statusStrip";
        this.statusStrip.Size = new System.Drawing.Size(684, 22);
        this.statusStrip.TabIndex = 2;

        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new System.Drawing.Size(567, 17);
        this.statusLabel.Spring = true;
        this.statusLabel.Text = "準備完了";
        this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        this.progressBar.Name = "progressBar";
        this.progressBar.Size = new System.Drawing.Size(100, 16);
        this.progressBar.Visible = false;

        // MainForm
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(684, 621);
        this.Controls.Add(this.logBox);
        this.Controls.Add(this.statusStrip);
        this.Controls.Add(this.tabControl);
        this.Font = new System.Drawing.Font("Noto Sans", 9F);
        this.Name = "MainForm";
        this.Text = "SongConverter";
        this.tabControl.ResumeLayout(false);
        this.tabSongSorter.ResumeLayout(false);
        this.tabSongSorter.PerformLayout();
        this.tabDanGenerator.ResumeLayout(false);
        this.tabDanGenerator.PerformLayout();
        this.tabAddSongs.ResumeLayout(false);
        this.tabAddSongs.PerformLayout();
        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.TabControl tabControl;
    private System.Windows.Forms.TabPage tabSongSorter;
    private System.Windows.Forms.TabPage tabDanGenerator;
    private System.Windows.Forms.TabPage tabAddSongs;
    private System.Windows.Forms.TextBox logBox;
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel statusLabel;
    private System.Windows.Forms.ToolStripProgressBar progressBar;

    private System.Windows.Forms.Button btnOrganize;
    private System.Windows.Forms.Button btnFetchLists;
    private System.Windows.Forms.Label lblTempSongs;
    private System.Windows.Forms.TextBox txtTempSongs;
    private System.Windows.Forms.Button btnBrowseTemp;
    private System.Windows.Forms.Label lblTaikoRoot;
    private System.Windows.Forms.TextBox txtTaikoRoot;
    private System.Windows.Forms.Button btnBrowseRoot;

    private System.Windows.Forms.Label lblWikiUrl;
    private System.Windows.Forms.TextBox txtWikiUrl;
    private System.Windows.Forms.Label lblDanOutput;
    private System.Windows.Forms.TextBox txtDanOutputSub;
    private System.Windows.Forms.Label lblDanSongs;
    private System.Windows.Forms.TextBox txtDanSongsPath;
    private System.Windows.Forms.Button btnBrowseDanSongs;
    private System.Windows.Forms.Button btnGenerateDan;

    private System.Windows.Forms.Label lblAddSongsFolder;
    private System.Windows.Forms.TextBox txtAddSongsFolder;
    private System.Windows.Forms.Button btnBrowseAddSongsFolder;
    private System.Windows.Forms.Button btnExecuteAddSongs;
}
