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
        this.tabAddSongs = new System.Windows.Forms.TabPage();
        this.tabSongSorter = new System.Windows.Forms.TabPage();
        this.tabDanGenerator = new System.Windows.Forms.TabPage();
        this.tabDanConvertor = new System.Windows.Forms.TabPage();
        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
        this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
        this.logBox = new System.Windows.Forms.TextBox();

        // Add Songs Tab Controls
        this.lblAddSongsFolder = new System.Windows.Forms.Label();
        this.txtAddSongsFolder = new System.Windows.Forms.TextBox();
        this.btnBrowseAddSongsFolder = new System.Windows.Forms.Button();
        this.btnExecuteAddSongs = new System.Windows.Forms.Button();

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
        this.lblDanOutputFolder = new System.Windows.Forms.Label();
        this.txtDanOutputFolder = new System.Windows.Forms.TextBox();
        this.btnBrowseDanOutputFolder = new System.Windows.Forms.Button();
        this.lblDanSongsPath = new System.Windows.Forms.Label();
        this.txtDanSongsPath = new System.Windows.Forms.TextBox();
        this.btnBrowseDanSongs = new System.Windows.Forms.Button();
        this.btnGenerateDan = new System.Windows.Forms.Button();
        this.lblWikiFilter = new System.Windows.Forms.Label();
        this.txtWikiFilter = new System.Windows.Forms.TextBox();

        // Dan Convertor Tab Controls
        this.lblTjaFile = new System.Windows.Forms.Label();
        this.txtTjaFile = new System.Windows.Forms.TextBox();
        this.btnBrowseTjaFile = new System.Windows.Forms.Button();
        this.lblDanConvertSimu = new System.Windows.Forms.Label();
        this.txtDanConvertSimu = new System.Windows.Forms.TextBox();
        this.btnBrowseDanConvertSimu = new System.Windows.Forms.Button();
        this.lblDanConvertOutputFolder = new System.Windows.Forms.Label();
        this.txtDanConvertOutputFolder = new System.Windows.Forms.TextBox();
        this.btnBrowseDanConvertOutputFolder = new System.Windows.Forms.Button();
        this.btnConvertDan = new System.Windows.Forms.Button();

        this.tabControl.SuspendLayout();
        this.tabAddSongs.SuspendLayout();
        this.tabSongSorter.SuspendLayout();
        this.tabDanGenerator.SuspendLayout();
        this.tabDanConvertor.SuspendLayout();
        this.statusStrip.SuspendLayout();
        this.SuspendLayout();

        // tabControl
        this.tabControl.Controls.Add(this.tabAddSongs);
        this.tabControl.Controls.Add(this.tabSongSorter);
        this.tabControl.Controls.Add(this.tabDanGenerator);
        this.tabControl.Controls.Add(this.tabDanConvertor);
        this.tabControl.Dock = System.Windows.Forms.DockStyle.Top;
        this.tabControl.Location = new System.Drawing.Point(0, 0);
        this.tabControl.Name = "tabControl";
        this.tabControl.SelectedIndex = 0;
        this.tabControl.Size = new System.Drawing.Size(684, 400); // Expanded slightly for output folder
        this.tabControl.TabIndex = 0;

        // tabAddSongs
        this.tabAddSongs.BackColor = System.Drawing.Color.White;
        this.tabAddSongs.Controls.Add(this.btnExecuteAddSongs);
        this.tabAddSongs.Controls.Add(this.btnBrowseAddSongsFolder);
        this.tabAddSongs.Controls.Add(this.txtAddSongsFolder);
        this.tabAddSongs.Controls.Add(this.lblAddSongsFolder);
        this.tabAddSongs.Location = new System.Drawing.Point(4, 24);
        this.tabAddSongs.Name = "tabAddSongs";
        this.tabAddSongs.Padding = new System.Windows.Forms.Padding(15);
        this.tabAddSongs.Size = new System.Drawing.Size(676, 372);
        this.tabAddSongs.TabIndex = 0;
        this.tabAddSongs.Text = "AddSongs";

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
        this.tabSongSorter.Size = new System.Drawing.Size(676, 372);
        this.tabSongSorter.TabIndex = 1;
        this.tabSongSorter.Text = "SongSorter";

        this.lblTempSongs.Location = new System.Drawing.Point(20, 20);
        this.lblTempSongs.Text = "コピー元の楽曲フォルダを選択:";
        this.lblTempSongs.Size = new System.Drawing.Size(400, 20);

        this.txtTempSongs.Location = new System.Drawing.Point(20, 45);
        this.txtTempSongs.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseTemp.Location = new System.Drawing.Point(560, 43);
        this.btnBrowseTemp.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseTemp.Text = "参照...";

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

        this.btnOrganize.Location = new System.Drawing.Point(180, 170);
        this.btnOrganize.Size = new System.Drawing.Size(180, 45);
        this.btnOrganize.Text = "並び替え開始";
        this.btnOrganize.BackColor = System.Drawing.Color.FromArgb(0, 153, 255);
        this.btnOrganize.ForeColor = System.Drawing.Color.White;
        this.btnOrganize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnOrganize.Font = new System.Drawing.Font("Noto Sans", 10F, System.Drawing.FontStyle.Bold);

        // tabDanGenerator
        this.tabDanGenerator.BackColor = System.Drawing.Color.White;
        this.tabDanGenerator.Controls.Add(this.btnBrowseDanOutputFolder);
        this.tabDanGenerator.Controls.Add(this.txtDanOutputFolder);
        this.tabDanGenerator.Controls.Add(this.lblDanOutputFolder);
        this.tabDanGenerator.Controls.Add(this.btnGenerateDan);
        this.tabDanGenerator.Controls.Add(this.btnBrowseDanSongs);
        this.tabDanGenerator.Controls.Add(this.txtDanSongsPath);
        this.tabDanGenerator.Controls.Add(this.lblDanSongsPath);
        this.tabDanGenerator.Controls.Add(this.txtWikiFilter);
        this.tabDanGenerator.Controls.Add(this.lblWikiFilter);
        this.tabDanGenerator.Controls.Add(this.txtWikiUrl);
        this.tabDanGenerator.Controls.Add(this.lblWikiUrl);
        this.tabDanGenerator.Location = new System.Drawing.Point(4, 24);
        this.tabDanGenerator.Name = "tabDanGenerator";
        this.tabDanGenerator.Padding = new System.Windows.Forms.Padding(15);
        this.tabDanGenerator.Size = new System.Drawing.Size(676, 372);
        this.tabDanGenerator.TabIndex = 2;
        this.tabDanGenerator.Text = "DanGenerator";

        this.lblWikiUrl.Location = new System.Drawing.Point(20, 20);
        this.lblWikiUrl.Text = "太鼓Wikiの段位URL:";
        this.lblWikiUrl.Size = new System.Drawing.Size(250, 20);

        this.txtWikiUrl.Location = new System.Drawing.Point(20, 45);
        this.txtWikiUrl.Size = new System.Drawing.Size(630, 23);

        this.lblWikiFilter.Location = new System.Drawing.Point(20, 85);
        this.lblWikiFilter.Text = "特定の段位のみ抽出 (例: 九段 / 空欄で全取得):";
        this.lblWikiFilter.Size = new System.Drawing.Size(350, 20);

        this.txtWikiFilter.Location = new System.Drawing.Point(20, 110);
        this.txtWikiFilter.Size = new System.Drawing.Size(250, 23);

        this.lblDanOutputFolder.Location = new System.Drawing.Point(290, 85);
        this.lblDanOutputFolder.Text = "出力フォルダ:";
        this.lblDanOutputFolder.Size = new System.Drawing.Size(200, 20);

        this.txtDanOutputFolder.Location = new System.Drawing.Point(290, 110);
        this.txtDanOutputFolder.Size = new System.Drawing.Size(360, 23);

        this.btnBrowseDanOutputFolder.Location = new System.Drawing.Point(560, 168);
        this.btnBrowseDanOutputFolder.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseDanOutputFolder.Text = "参照...";

        this.lblDanSongsPath.Location = new System.Drawing.Point(20, 145);
        this.lblDanSongsPath.Text = "シミュフォルダを選択:";
        this.lblDanSongsPath.Size = new System.Drawing.Size(400, 20);

        this.txtDanSongsPath.Location = new System.Drawing.Point(20, 170);
        this.txtDanSongsPath.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseDanSongs.Location = new System.Drawing.Point(560, 168);
        this.btnBrowseDanSongs.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseDanSongs.Text = "参照...";

        this.btnGenerateDan.Location = new System.Drawing.Point(20, 220);
        this.btnGenerateDan.Size = new System.Drawing.Size(200, 45);
        this.btnGenerateDan.Text = "段位生成";
        this.btnGenerateDan.BackColor = System.Drawing.Color.FromArgb(0, 153, 255);
        this.btnGenerateDan.ForeColor = System.Drawing.Color.White;
        this.btnGenerateDan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnGenerateDan.Font = new System.Drawing.Font("Noto Sans", 10F, System.Drawing.FontStyle.Bold);

        // tabDanConvertor
        this.tabDanConvertor.BackColor = System.Drawing.Color.White;
        this.tabDanConvertor.AllowDrop = true;
        this.tabDanConvertor.Controls.Add(this.lblDanConvertOutputFolder);
        this.tabDanConvertor.Controls.Add(this.txtDanConvertOutputFolder);
        this.tabDanConvertor.Controls.Add(this.btnBrowseDanConvertOutputFolder);
        this.tabDanConvertor.Controls.Add(this.btnConvertDan);
        this.tabDanConvertor.Controls.Add(this.btnBrowseDanConvertSimu);
        this.tabDanConvertor.Controls.Add(this.txtDanConvertSimu);
        this.tabDanConvertor.Controls.Add(this.lblDanConvertSimu);
        this.tabDanConvertor.Controls.Add(this.btnBrowseTjaFile);
        this.tabDanConvertor.Controls.Add(this.txtTjaFile);
        this.tabDanConvertor.Controls.Add(this.lblTjaFile);
        this.tabDanConvertor.Location = new System.Drawing.Point(4, 24);
        this.tabDanConvertor.Name = "tabDanConvertor";
        this.tabDanConvertor.Padding = new System.Windows.Forms.Padding(15);
        this.tabDanConvertor.Size = new System.Drawing.Size(676, 372);
        this.tabDanConvertor.TabIndex = 3;
        this.tabDanConvertor.Text = "DanConvertor";

        this.lblTjaFile.Location = new System.Drawing.Point(20, 20);
        this.lblTjaFile.Text = "変換対象のtjaを選択(またはドラッグアンドドロップ):";
        this.lblTjaFile.Size = new System.Drawing.Size(400, 20);

        this.txtTjaFile.Location = new System.Drawing.Point(20, 45);
        this.txtTjaFile.Size = new System.Drawing.Size(530, 23);
        this.txtTjaFile.AllowDrop = true;

        this.btnBrowseTjaFile.Location = new System.Drawing.Point(560, 43);
        this.btnBrowseTjaFile.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseTjaFile.Text = "参照...";

        this.lblDanConvertOutputFolder.Location = new System.Drawing.Point(20, 85);
        this.lblDanConvertOutputFolder.Text = "出力フォルダ:";
        this.lblDanConvertOutputFolder.Size = new System.Drawing.Size(200, 20);

        this.txtDanConvertOutputFolder.Location = new System.Drawing.Point(20, 110);
        this.txtDanConvertOutputFolder.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseDanConvertOutputFolder.Location = new System.Drawing.Point(560, 108);
        this.btnBrowseDanConvertOutputFolder.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseDanConvertOutputFolder.Text = "参照...";

        this.lblDanConvertSimu.Location = new System.Drawing.Point(20, 145);
        this.lblDanConvertSimu.Text = "シミュフォルダを選択 (省略可・内部検索用):";
        this.lblDanConvertSimu.Size = new System.Drawing.Size(400, 20);

        this.txtDanConvertSimu.Location = new System.Drawing.Point(20, 170);
        this.txtDanConvertSimu.Size = new System.Drawing.Size(530, 23);

        this.btnBrowseDanConvertSimu.Location = new System.Drawing.Point(560, 168);
        this.btnBrowseDanConvertSimu.Size = new System.Drawing.Size(90, 27);
        this.btnBrowseDanConvertSimu.Text = "参照...";

        this.btnConvertDan.Location = new System.Drawing.Point(20, 220);
        this.btnConvertDan.Size = new System.Drawing.Size(200, 45);
        this.btnConvertDan.Text = "変換実行";
        this.btnConvertDan.BackColor = System.Drawing.Color.FromArgb(0, 153, 255);
        this.btnConvertDan.ForeColor = System.Drawing.Color.White;
        this.btnConvertDan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnConvertDan.Font = new System.Drawing.Font("Noto Sans", 10F, System.Drawing.FontStyle.Bold);

        // logBox
        this.logBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        this.logBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.logBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.logBox.Font = new System.Drawing.Font("Consolas", 9F);
        this.logBox.ForeColor = System.Drawing.Color.LightGray;
        this.logBox.Location = new System.Drawing.Point(0, 400);
        this.logBox.Multiline = true;
        this.logBox.Name = "logBox";
        this.logBox.ReadOnly = true;
        this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.logBox.Size = new System.Drawing.Size(684, 199);
        this.logBox.TabIndex = 1;

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
        this.tabAddSongs.ResumeLayout(false);
        this.tabAddSongs.PerformLayout();
        this.tabSongSorter.ResumeLayout(false);
        this.tabSongSorter.PerformLayout();
        this.tabDanGenerator.ResumeLayout(false);
        this.tabDanGenerator.PerformLayout();
        this.tabDanConvertor.ResumeLayout(false);
        this.tabDanConvertor.PerformLayout();
        this.statusStrip.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.TabControl tabControl;
    private System.Windows.Forms.TabPage tabSongSorter;
    private System.Windows.Forms.TabPage tabDanGenerator;
    private System.Windows.Forms.TabPage tabAddSongs;
    private System.Windows.Forms.TabPage tabDanConvertor;
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
    private System.Windows.Forms.Label lblDanOutputFolder;
    private System.Windows.Forms.TextBox txtDanOutputFolder;
    private System.Windows.Forms.Button btnBrowseDanOutputFolder;
    private System.Windows.Forms.Button btnGenerateDan;
    private System.Windows.Forms.Label lblWikiFilter;
    private System.Windows.Forms.TextBox txtWikiFilter;
    private System.Windows.Forms.Label lblDanSongsPath;
    private System.Windows.Forms.TextBox txtDanSongsPath;
    private System.Windows.Forms.Button btnBrowseDanSongs;

    private System.Windows.Forms.Label lblAddSongsFolder;
    private System.Windows.Forms.TextBox txtAddSongsFolder;
    private System.Windows.Forms.Button btnBrowseAddSongsFolder;
    private System.Windows.Forms.Button btnExecuteAddSongs;

    private System.Windows.Forms.Label lblTjaFile;
    private System.Windows.Forms.TextBox txtTjaFile;
    private System.Windows.Forms.Button btnBrowseTjaFile;
    private System.Windows.Forms.Label lblDanConvertSimu;
    private System.Windows.Forms.TextBox txtDanConvertSimu;
    private System.Windows.Forms.Button btnBrowseDanConvertSimu;
    private System.Windows.Forms.Label lblDanConvertOutputFolder;
    private System.Windows.Forms.TextBox txtDanConvertOutputFolder;
    private System.Windows.Forms.Button btnBrowseDanConvertOutputFolder;
    private System.Windows.Forms.Button btnConvertDan;
}
