using System.Text;
using System.Text.Json;
using SongConverter.Core;

namespace SongConverter.UI;

public partial class MainForm : Form
{
    private string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly (string SourceCategory, string FetchFileName)[] CategoryMap =
    {
        ("00 ポップス", "pops.php"),
        ("01 キッズ", "kids.php"),
        ("02 アニメ", "anime.php"),
        ("03 ボーカロイド曲", "vocaloid.php"),
        ("04 ゲームミュージック", "game.php"),
        ("05 バラエティ", "variety.php"),
        ("06 クラシック", "classic.php"),
        ("07 ナムコオリジナル", "namco.php")
    };

    private readonly HashSet<string> _selectedSourceCategories = new(SongSorterCore.SourceCategories, StringComparer.OrdinalIgnoreCase);
    private Button? _btnCategorySelect;
    private ToolStripStatusLabel? _cancelStatusLink;
    private CancellationTokenSource? _operationCts;

    public MainForm()
    {
        InitializeComponent();
        if (File.Exists("SongConverter.ico"))
        {
            this.Icon = new Icon("SongConverter.ico");
        }
        logBox.Text = "準備完了。" + Environment.NewLine;
        LoadSettings();
        
        // Browsing
        btnBrowseAddSongsFolder.Click += (s, e) => BrowseFolder(txtAddSongsFolder, true);
        btnBrowseTemp.Click += (s, e) => BrowseFolder(txtTempSongs, true);
        btnBrowseRoot.Click += (s, e) => BrowseFolder(txtTaikoRoot, false, true);
        btnBrowseDanSongs.Click += (s, e) => BrowseFolder(txtDanSongsPath, false, true, true);
        btnBrowseDanConvertSimu.Click += (s, e) => BrowseFolder(txtDanConvertSimu, false, true, true);
        btnBrowseTjaFile.Click += (s, e) => BrowseFile(txtTjaFile, "TJA files (*.tja)|*.tja|All files (*.*)|*.*");

        // Sync Source Folders
        txtAddSongsFolder.TextChanged += (s, e) => SyncSourceFolders(txtAddSongsFolder.Text);
        txtTempSongs.TextChanged += (s, e) => SyncSourceFolders(txtTempSongs.Text);
        
        // Sync Simu Folders
        txtTaikoRoot.Leave += (s, e) => SyncSimuFolders(txtTaikoRoot.Text);
        txtDanSongsPath.Leave += (s, e) => SyncSimuFolders(txtDanSongsPath.Text);
        txtDanConvertSimu.Leave += (s, e) => SyncSimuFolders(txtDanConvertSimu.Text);

        // Sync Output Sub Folders
        txtDanOutputSub.TextChanged += (s, e) => { SyncOutputSubFolders(txtDanOutputSub.Text); SaveSettings(); };
        txtDanConvertOutputSub.TextChanged += (s, e) => { SyncOutputSubFolders(txtDanConvertOutputSub.Text); SaveSettings(); };
        
        // Operations
        btnFetchLists.Click += async (s, e) => await OnFetchListsClick();
        btnOrganize.Click += async (s, e) => await OnOrganizeClick();
        btnGenerateDan.Click += async (s, e) => await OnGenerateDanClick();
        btnExecuteAddSongs.Click += async (s, e) => await OnExecuteAddSongsClick();
        btnConvertDan.Click += async (s, e) => await OnConvertDanClick();

        // D&D
        tabDanConvertor.DragEnter += Control_DragEnter;
        tabDanConvertor.DragDrop += Control_DragDrop;
        txtTjaFile.DragEnter += Control_DragEnter;
        txtTjaFile.DragDrop += Control_DragDrop;

        InitializeCategorySelectorUi();
        InitializeCancelUi();
        UpdateCategoryButtonText();
    }

    private void Control_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private void Control_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data == null) return;
        string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (files == null || files.Length == 0) return;

        // If it's the DanConvertor tab or txtTjaFile
        txtTjaFile.Text = string.Join(";", files);
        SaveSettings();
    }

    private void SyncSourceFolders(string value)
    {
        if (txtAddSongsFolder.Text != value) txtAddSongsFolder.Text = value;
        if (txtTempSongs.Text != value) txtTempSongs.Text = value;
    }

    private void SyncOutputSubFolders(string value)
    {
        if (txtDanOutputSub.Text != value) txtDanOutputSub.Text = value;
        if (txtDanConvertOutputSub.Text != value) txtDanConvertOutputSub.Text = value;
    }

    private void SyncSimuFolders(string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue)) return;
        if (string.IsNullOrWhiteSpace(txtTaikoRoot.Text)) txtTaikoRoot.Text = newValue;
        if (string.IsNullOrWhiteSpace(txtDanSongsPath.Text)) txtDanSongsPath.Text = newValue;
        if (string.IsNullOrWhiteSpace(txtDanConvertSimu.Text)) txtDanConvertSimu.Text = newValue;
    }

    private void BrowseFile(TextBox target, string filter)
    {
        using var ofd = new OpenFileDialog { Filter = filter };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            target.Text = ofd.FileName;
            SaveSettings();
        }
    }

    private void BrowseFolder(TextBox target, bool isSource = false, bool isSimu = false, bool isConvertOrGenSimu = false)
    {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            target.Text = fbd.SelectedPath;
            if (isSource) SyncSourceFolders(target.Text);
            if (isSimu) SyncSimuFolders(target.Text);
            SaveSettings();
        }
    }

    private void Log(string msg)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => Log(msg)));
            return;
        }
        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
    }

    private void SetStatus(string msg, bool showProgress = false)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => SetStatus(msg, showProgress)));
            return;
        }
        statusLabel.Text = msg;
        progressBar.Visible = showProgress;
    }

    private void SetActionButtonsEnabled(bool enabled)
    {
        btnFetchLists.Enabled = enabled;
        btnOrganize.Enabled = enabled;
        btnGenerateDan.Enabled = enabled;
        btnExecuteAddSongs.Enabled = enabled;
        btnConvertDan.Enabled = enabled;
    }

    private void InitializeCategorySelectorUi()
    {
        _btnCategorySelect = new Button
        {
            Name = "btnCategorySelect",
            Text = "カテゴリー: 全て",
            Location = new Point(370, 170),
            Size = new Size(280, 45),
            BackColor = Color.FromArgb(80, 80, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnCategorySelect.Click += (s, e) =>
        {
            if (ShowCategoryDialog())
            {
                UpdateCategoryButtonText();
                SaveSettings();
            }
        };
        tabSongSorter.Controls.Add(_btnCategorySelect);
    }

    private void InitializeCancelUi()
    {
        _cancelStatusLink = new ToolStripStatusLabel
        {
            IsLink = true,
            Text = "中断",
            Enabled = false,
            ForeColor = Color.Red
        };
        _cancelStatusLink.Click += (s, e) =>
        {
            if (_operationCts == null || _operationCts.IsCancellationRequested) return;
            _operationCts.Cancel();
            Log("中断を受け付けました。");
        };
        statusStrip.Items.Add(_cancelStatusLink);
    }

    private CancellationToken BeginOperation()
    {
        _operationCts?.Dispose();
        _operationCts = new CancellationTokenSource();
        if (_cancelStatusLink != null) _cancelStatusLink.Enabled = true;
        SetActionButtonsEnabled(false);
        return _operationCts.Token;
    }

    private void EndOperation()
    {
        _operationCts?.Dispose();
        _operationCts = null;
        if (_cancelStatusLink != null) _cancelStatusLink.Enabled = false;
        SetActionButtonsEnabled(true);
        ResetProgress();
    }

    private void SetIndeterminateProgress(bool visible)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => SetIndeterminateProgress(visible)));
            return;
        }

        progressBar.Style = visible ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        progressBar.Visible = visible;
    }

    private void SetProgressValue(int current, int total)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => SetProgressValue(current, total)));
            return;
        }

        progressBar.Style = ProgressBarStyle.Blocks;
        progressBar.Visible = total > 0;
        progressBar.Maximum = total <= 0 ? 1 : total;
        progressBar.Value = Math.Max(0, Math.Min(progressBar.Maximum, current));
    }

    private void ResetProgress()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(ResetProgress));
            return;
        }

        progressBar.Style = ProgressBarStyle.Blocks;
        progressBar.Visible = false;
        progressBar.Value = 0;
    }

    private bool ShowCategoryDialog()
    {
        using var dialog = new Form
        {
            Text = "カテゴリー選択",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(360, 380),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var checkedList = new CheckedListBox
        {
            Dock = DockStyle.Top,
            Height = 290,
            CheckOnClick = true
        };

        foreach (var category in SongSorterCore.SourceCategories)
        {
            var isChecked = _selectedSourceCategories.Count == 0 || _selectedSourceCategories.Contains(category);
            checkedList.Items.Add(category, isChecked);
        }

        var btnAll = new Button { Text = "全て", Location = new Point(12, 300), Size = new Size(80, 30) };
        var btnNone = new Button { Text = "なし", Location = new Point(100, 300), Size = new Size(80, 30) };
        var btnOk = new Button { Text = "決定", Location = new Point(190, 340), Size = new Size(75, 30), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "キャンセル", Location = new Point(273, 340), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

        btnAll.Click += (s, e) =>
        {
            for (var i = 0; i < checkedList.Items.Count; i++) checkedList.SetItemChecked(i, true);
        };
        btnNone.Click += (s, e) =>
        {
            for (var i = 0; i < checkedList.Items.Count; i++) checkedList.SetItemChecked(i, false);
        };

        dialog.Controls.Add(checkedList);
        dialog.Controls.Add(btnAll);
        dialog.Controls.Add(btnNone);
        dialog.Controls.Add(btnOk);
        dialog.Controls.Add(btnCancel);
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        if (dialog.ShowDialog(this) != DialogResult.OK) return false;

        _selectedSourceCategories.Clear();
        foreach (var item in checkedList.CheckedItems)
        {
            if (item is string category) _selectedSourceCategories.Add(category);
        }

        if (_selectedSourceCategories.Count == 0)
        {
            foreach (var category in SongSorterCore.SourceCategories)
                _selectedSourceCategories.Add(category);
        }

        return true;
    }

    private void UpdateCategoryButtonText()
    {
        if (_btnCategorySelect == null) return;
        var total = SongSorterCore.SourceCategories.Length;
        var selected = _selectedSourceCategories.Count == 0 ? total : _selectedSourceCategories.Count;
        _btnCategorySelect.Text = selected >= total ? "カテゴリー: 全て" : $"カテゴリー: {selected}/{total}";
    }

    private IReadOnlyCollection<string> GetSelectedSourceCategories()
    {
        if (_selectedSourceCategories.Count == 0)
            return SongSorterCore.SourceCategories;
        return _selectedSourceCategories.ToArray();
    }

    private IReadOnlyCollection<string> GetSelectedFetchFileNames()
    {
        var selected = new HashSet<string>(GetSelectedSourceCategories(), StringComparer.OrdinalIgnoreCase);
        return CategoryMap
            .Where(m => selected.Contains(m.SourceCategory))
            .Select(m => m.FetchFileName)
            .ToArray();
    }

    private async Task OnFetchListsClick()
    {
        var ct = BeginOperation();
        SetStatus("譜面リスト取得中...", true);
        SetIndeterminateProgress(false);
        Log("公開譜面リストの取得を開始します。");

        try
        {
            var exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Export");
            Directory.CreateDirectory(exportDir);

            var selectedFiles = new HashSet<string>(GetSelectedFetchFileNames(), StringComparer.OrdinalIgnoreCase);
            var categories = SongListBase.Categories.Where(c => selectedFiles.Contains(c.FileName)).ToArray();
            SetProgressValue(0, categories.Length);

            for (var i = 0; i < categories.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                var cat = categories[i];
                Log($"取得中: {cat.FileName}");

                var songs = await SongListFetcher.FetchSongsAsync(cat.FileName, ct);
                var filePath = Path.Combine(exportDir, $"songlist_{cat.DisplayName}.txt");
                var lines = songs.Select((s, n) => $"{n + 1:000}\t{s.Title}\t{s.Subtitle}");
                await File.WriteAllLinesAsync(filePath, lines, Utf8NoBom, ct);

                SetProgressValue(i + 1, categories.Length);
            }

            Log("譜面リストの更新が完了しました。");
            MessageBox.Show("譜面リストの更新が完了しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("ユーザー操作で中断しました。");
            MessageBox.Show("処理を中断しました。", "中断", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"エラー: {ex.Message}");
            MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetStatus("待機中");
            EndOperation();
        }
    }

    private async Task OnOrganizeClick()
    {
        if (string.IsNullOrWhiteSpace(txtTempSongs.Text) || string.IsNullOrWhiteSpace(txtTaikoRoot.Text))
        {
            MessageBox.Show("コピー元とシミュフォルダを設定してください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ct = BeginOperation();
        SetStatus("曲フォルダー整理中...", true);
        SetProgressValue(0, 1);
        Log("曲フォルダー整理を開始します。");

        try
        {
            var runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var selectedCategories = GetSelectedSourceCategories();
            var result = await Task.Run(() =>
                SongSorterCore.OrganizeSongsDetailed(
                    txtTempSongs.Text,
                    txtTaikoRoot.Text,
                    runId,
                    selectedCategories,
                    Log,
                    ct,
                    p => SetProgressValue(p.ProcessedFolders, p.TotalFolders)), ct);

            Log(result.Summary);
            Log($"詳細レポート: {result.ReportPath}");
            MessageBox.Show($"{result.Summary}\n\n詳細レポート:\n{result.ReportPath}", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("ユーザー操作で中断しました。");
            MessageBox.Show("処理を中断しました。", "中断", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"エラー: {ex.Message}");
            MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetStatus("待機中");
            EndOperation();
        }
    }

    private async Task OnGenerateDanClick()
    {
        if (string.IsNullOrWhiteSpace(txtWikiUrl.Text) || string.IsNullOrWhiteSpace(txtDanSongsPath.Text))
        {
            MessageBox.Show("Wiki URLとシミュフォルダを入力してください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ct = BeginOperation();
        SetStatus("Dan.json 生成中...", true);
        SetIndeterminateProgress(true);
        Log("Dan.json 生成を開始します。");

        try
        {
            string subDir = txtDanOutputSub.Text.Trim();
            if (string.IsNullOrEmpty(subDir)) subDir = "Default";
            string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DanLists", subDir);

            string filter = txtWikiFilter.Text.Trim();
            await DanGeneratorCore.GenerateAsync(txtWikiUrl.Text, outputDir, txtDanSongsPath.Text, filter, Log, ct);
            Log("Dan.json 生成が完了しました。");
            Log($"出力先: {outputDir}");
            MessageBox.Show("Dan.json 生成が完了しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("ユーザー操作で中断しました。");
            MessageBox.Show("処理を中断しました。", "中断", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"エラー: {ex.Message}");
            MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetStatus("待機中");
            EndOperation();
        }
    }

    private async Task OnConvertDanClick()
    {
        if (string.IsNullOrWhiteSpace(txtTjaFile.Text) || string.IsNullOrWhiteSpace(txtDanConvertSimu.Text))
        {
            MessageBox.Show("変換対象(TJA)とシミュフォルダを選択してください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ct = BeginOperation();
        SetStatus("TJA 変換中...", true);
        SetIndeterminateProgress(true);
        Log("TJA から Dan.json への変換を開始します...");

        try
        {
            string subDir = txtDanConvertOutputSub.Text.Trim();
            if (string.IsNullOrEmpty(subDir)) subDir = "Default";
            string outputRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DanLists", subDir);

            var paths = txtTjaFile.Text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var tjaFiles = new List<string>();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    tjaFiles.AddRange(Directory.GetFiles(path, "*.tja", SearchOption.AllDirectories));
                }
                else if (File.Exists(path))
                {
                    if (path.EndsWith(".tja", StringComparison.OrdinalIgnoreCase))
                        tjaFiles.Add(path);
                }
            }

            if (tjaFiles.Count == 0)
            {
                Log("有効なTJAファイルが見つかりませんでした。");
                return;
            }

            Log($"{tjaFiles.Count} 個のTJAファイルを処理します。");

            foreach (var tja in tjaFiles)
            {
                ct.ThrowIfCancellationRequested();
                Log($"処理開始: {Path.GetFileName(tja)}");
                await DanConvertorCore.ConvertAsync(tja, outputRoot, txtDanConvertSimu.Text, Log, ct);
            }

            Log("すべての変換が完了しました。");
            MessageBox.Show($"{tjaFiles.Count} 件の変換が完了しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("中断されました。");
        }
        catch (Exception ex)
        {
            Log($"エラー: {ex.Message}");
            MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetStatus("待機中");
            EndOperation();
        }
    }

    private async Task OnExecuteAddSongsClick()
    {
        if (string.IsNullOrWhiteSpace(txtAddSongsFolder.Text))
        {
            MessageBox.Show("作業フォルダーを設定してください。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ct = BeginOperation();
        SetStatus("AddSongs 同期中...", true);
        SetIndeterminateProgress(true);
        Log("AddSongs 同期を開始します。");

        try
        {
            bool gitInstalled = await CheckGitInstalledAsync();
            if (!gitInstalled)
            {
                Log("Git がインストールされていません。");
                Log("インストール先: https://git-scm.com/");
                MessageBox.Show("Git が必要です。https://git-scm.com/ からインストールしてください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var targetDir = txtAddSongsFolder.Text;
            Directory.CreateDirectory(targetDir);

            var songsDir = Path.Combine(targetDir, "Songs");
            var gitDir = Path.Combine(songsDir, ".git");

            if (Directory.Exists(gitDir))
            {
                Log("既存の Songs リポジトリを検出しました。pull を実行します。");
                await RunGitPullAsync(targetDir, ct);
            }
            else if (Directory.Exists(songsDir))
            {
                throw new Exception("Songs フォルダーは存在しますが、Git リポジトリではありません。");
            }
            else
            {
                Log("Songs リポジトリが見つからないため clone を実行します。");
                await RunGitCloneAsync(targetDir, ct);
            }

            Log("AddSongs 同期が完了しました。");
            MessageBox.Show("AddSongs 同期が完了しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("ユーザー操作で中断しました。");
            MessageBox.Show("処理を中断しました。", "中断", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"エラー: {ex.Message}");
            MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetStatus("待機中");
            EndOperation();
        }
    }

    private async Task<bool> CheckGitInstalledAsync()
    {
#if TEST
        return false;
#else
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = "--version";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
#endif
    }

    private Task RunGitCloneAsync(string workingDir, CancellationToken ct)
    {
        return RunGitCommandAsync(workingDir, "clone https://ese.tjadataba.se/ESE/ESE.git Songs", ct);
    }

    private Task RunGitPullAsync(string workingDir, CancellationToken ct)
    {
        return RunGitCommandAsync(workingDir, "-C Songs pull --ff-only", ct);
    }

    private async Task RunGitCommandAsync(string workingDir, string arguments, CancellationToken ct)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = arguments;
        process.StartInfo.WorkingDirectory = workingDir;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.StandardOutputEncoding = Encoding.Default;
        process.StartInfo.StandardErrorEncoding = Encoding.Default;

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data);
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var reg = ct.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill(true);
            }
            catch { }
        });

        await process.WaitForExitAsync();
        ct.ThrowIfCancellationRequested();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Git コマンドが失敗しました (終了コード: {process.ExitCode}): git {arguments}");
        }
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            AddSongsFolder = txtAddSongsFolder.Text,
            TempSongs = txtTempSongs.Text,
            TaikoRoot = txtTaikoRoot.Text,
            DanSongsPath = txtDanSongsPath.Text,
            DanConvertSimu = txtDanConvertSimu.Text,
            WikiUrl = txtWikiUrl.Text,
            OutputSub = txtDanOutputSub.Text,
            ConvertOutputSub = txtDanConvertOutputSub.Text,
            WikiFilter = txtWikiFilter.Text,
            TjaFile = txtTjaFile.Text,
            SelectedCategoriesCsv = string.Join("|", GetSelectedSourceCategories())
        };

        var json = JsonSerializer.Serialize(settings);
        File.WriteAllText(SettingsPath, json, Utf8NoBom);
    }

    private void LoadSettings()
    {
        if (!File.Exists(SettingsPath)) return;

        try
        {
            var json = ReadTextWithFallback(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings == null) return;

            txtAddSongsFolder.Text = settings.AddSongsFolder ?? "";
            txtTempSongs.Text = settings.TempSongs ?? "";
            txtTaikoRoot.Text = settings.TaikoRoot ?? "";
            txtDanSongsPath.Text = settings.DanSongsPath ?? "";
            txtDanConvertSimu.Text = settings.DanConvertSimu ?? "";
            txtTjaFile.Text = settings.TjaFile ?? "";
            
            txtDanOutputSub.Text = settings.OutputSub ?? "今段位";
            txtDanConvertOutputSub.Text = settings.ConvertOutputSub ?? txtDanOutputSub.Text;

            txtWikiUrl.Text = "";
            txtWikiFilter.Text = "";

            _selectedSourceCategories.Clear();
            var raw = settings.SelectedCategoriesCsv ?? string.Empty;
            var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (SongSorterCore.SourceCategories.Contains(part, StringComparer.OrdinalIgnoreCase))
                    _selectedSourceCategories.Add(part);
            }

            if (_selectedSourceCategories.Count == 0)
            {
                foreach (var category in SongSorterCore.SourceCategories)
                    _selectedSourceCategories.Add(category);
            }
        }
        catch { }
    }

    private static string ReadTextWithFallback(string path)
    {
        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return File.ReadAllText(path, Encoding.GetEncoding(932));
        }
    }

    class AppSettings
    {
        public string AddSongsFolder { get; set; } = "";
        public string TempSongs { get; set; } = "";
        public string TaikoRoot { get; set; } = "";
        public string DanSongsPath { get; set; } = "";
        public string DanConvertSimu { get; set; } = "";
        public string WikiUrl { get; set; } = "";
        public string OutputSub { get; set; } = "";
        public string ConvertOutputSub { get; set; } = "";
        public string WikiFilter { get; set; } = "";
        public string TjaFile { get; set; } = "";
        public string SelectedCategoriesCsv { get; set; } = "";
    }
}
