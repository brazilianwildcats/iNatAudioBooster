using System.ComponentModel;
using System.Diagnostics;

namespace INatTrailCamAudioBooster;

internal sealed class MainForm : Form
{
    private readonly BindingList<MediaFileItem> _items = [];
    private readonly FfmpegService _ffmpeg = new();
    private readonly AppSettings _settings;

    private readonly DataGridView _fileGrid = new();
    private readonly Label _toolStatus = new();
    private readonly Label _fileCount = new();
    private readonly Label _currentFileLabel = new();
    private readonly Label _overallLabel = new();
    private readonly ModernProgressBar _fileProgress = new();
    private readonly ModernProgressBar _overallProgress = new();
    private readonly ModernButton _startButton = new();
    private readonly ModernButton _cancelButton = new();
    private readonly ModernButton _openOutputButton = new();
    private readonly CheckBox _limiterCheck = new();
    private readonly CheckBox _metadataCheck = new();
    private readonly RadioButton _automaticOutputRadio = new();
    private readonly RadioButton _customOutputRadio = new();
    private readonly TextBox _customOutputText = new();
    private readonly ModernButton _browseOutputButton = new();
    private readonly Dictionary<int, RadioButton> _gainButtons = new();

    private CancellationTokenSource? _processingCancellation;
    private bool _isProcessing;
    private string? _lastOutputDirectory;
    private string _ffmpegVersion = "";

    public MainForm()
    {
        _settings = SettingsService.Load();

        Text = "iNat TrailCam Audio Booster";
        Icon = LoadApplicationIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1040, 700);
        Size = new Size(1220, 820);
        BackColor = Theme.Background;
        Font = new Font("Segoe UI", 9f);
        AllowDrop = true;

        BuildInterface();
        ApplySettings();
        WireEvents();

        Shown += async (_, _) => await ValidateToolsAsync();
    }

    private void BuildInterface()
    {
        Controls.Add(BuildFooter());
        Controls.Add(BuildMainArea());
        Controls.Add(BuildHeader());
    }

    private Control BuildHeader()
    {
        var header = new GradientHeader();

        var logo = new PictureBox
        {
            Image = TryLoadImage(Path.Combine(AppPaths.AssetsDirectory, "inat-trailcam-logo.png")),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(86, 86),
            Location = new Point(24, 13),
            BackColor = Color.Transparent
        };

        var title = new Label
        {
            AutoSize = true,
            Text = "iNat TrailCam\nAudio Booster",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            Location = new Point(126, 20)
        };

        var subtitle = new Label
        {
            AutoSize = true,
            Text = "Aumente somente o áudio e preserve o vídeo sem recodificação",
            ForeColor = Color.FromArgb(230, 240, 232),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10f),
            Location = new Point(430, 50)
        };

        var version = new Label
        {
            AutoSize = true,
            Text = "V02",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(70, 255, 255, 255),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Padding = new Padding(8, 4, 8, 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(1135, 14)
        };

        var aboutButton = new ModernButton
        {
            Text = "Sobre",
            BackColor = Color.FromArgb(70, 255, 255, 255),
            ForeColor = Color.White,
            Width = 86,
            Height = 34,
            Radius = 17,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(1084, 58)
        };
        aboutButton.Click += (_, _) => ShowAbout();

        header.Resize += (_, _) =>
        {
            version.Left = header.ClientSize.Width - version.Width - 22;
            aboutButton.Left = header.ClientSize.Width - aboutButton.Width - 22;
        };

        header.Controls.AddRange([logo, title, subtitle, version, aboutButton]);
        return header;
    }

    private Control BuildMainArea()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 1
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 322));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildOptionsPanel(), 0, 0);
        root.Controls.Add(BuildFilesPanel(), 1, 0);

        return root;
    }

    private Control BuildOptionsPanel()
    {
        var panel = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 11,
            BackColor = Color.Transparent
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(SectionTitle("Ganho de áudio"), 0, 0);
        layout.Controls.Add(Hint("Escolha o ganho aplicado antes do limitador."), 0, 1);

        var warning = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(274, 0),
            Text = "Ganhos acima de +30 dB podem elevar bastante o ruído e fazer o limitador atuar intensamente.",
            ForeColor = Theme.Warning,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Margin = new Padding(0, 6, 0, 8)
        };
        layout.Controls.Add(warning, 0, 2);
        layout.Controls.Add(BuildGainGrid(), 0, 3);

        layout.Controls.Add(SectionTitle("Opções"), 0, 4);

        _limiterCheck.Text = "Aplicar limitador de áudio";
        _limiterCheck.AutoSize = true;
        _limiterCheck.ForeColor = Theme.Ink;
        _limiterCheck.Margin = new Padding(0, 8, 0, 4);
        layout.Controls.Add(_limiterCheck, 0, 5);

        _metadataCheck.Text = "Preservar metadados e datas do arquivo";
        _metadataCheck.AutoSize = true;
        _metadataCheck.ForeColor = Theme.Ink;
        _metadataCheck.Margin = new Padding(0, 4, 0, 10);
        layout.Controls.Add(_metadataCheck, 0, 6);

        layout.Controls.Add(SectionTitle("Pasta de saída"), 0, 7);
        layout.Controls.Add(BuildOutputControls(), 0, 8);

        _toolStatus.AutoSize = true;
        _toolStatus.MaximumSize = new Size(274, 0);
        _toolStatus.Text = "Verificando FFmpeg BtbN...";
        _toolStatus.ForeColor = Theme.Muted;
        _toolStatus.Margin = new Padding(0, 14, 0, 8);
        layout.Controls.Add(_toolStatus, 0, 9);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };

        _startButton.Text = "INICIAR";
        _startButton.Width = 170;
        _startButton.Height = 44;
        _startButton.Enabled = false;

        _cancelButton.Text = "Cancelar";
        _cancelButton.Width = 94;
        _cancelButton.Height = 44;
        _cancelButton.BackColor = Theme.Danger;
        _cancelButton.Enabled = false;

        buttons.Controls.AddRange([_startButton, _cancelButton]);
        layout.Controls.Add(buttons, 0, 10);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildGainGrid()
    {
        var gains = new[] { 10, 15, 20, 30, 40, 50, 60, 70, 80, 100 };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        for (var row = 0; row < 5; row++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        for (var i = 0; i < gains.Length; i++)
        {
            var gain = gains[i];
            var radio = new RadioButton
            {
                Appearance = Appearance.Button,
                Text = $"+{gain} dB",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.GreenSoft,
                ForeColor = Theme.GreenStrong,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(3),
                Tag = gain
            };
            radio.FlatAppearance.BorderColor = Theme.Line;
            radio.FlatAppearance.CheckedBackColor = Theme.Green;
            radio.CheckedChanged += (_, _) =>
            {
                radio.ForeColor = radio.Checked ? Color.White : Theme.GreenStrong;
            };

            _gainButtons[gain] = radio;
            grid.Controls.Add(radio, i % 2, i / 2);
        }

        return grid;
    }

    private Control BuildOutputControls()
    {
        var container = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

        _automaticOutputRadio.Text = "Criar “Audio_Aumentado” ao lado do original";
        _automaticOutputRadio.AutoSize = true;
        _automaticOutputRadio.ForeColor = Theme.Ink;
        container.Controls.Add(_automaticOutputRadio, 0, 0);
        container.SetColumnSpan(_automaticOutputRadio, 2);

        _customOutputRadio.Text = "Usar uma pasta específica";
        _customOutputRadio.AutoSize = true;
        _customOutputRadio.ForeColor = Theme.Ink;
        _customOutputRadio.Margin = new Padding(0, 8, 0, 4);
        container.Controls.Add(_customOutputRadio, 0, 1);
        container.SetColumnSpan(_customOutputRadio, 2);

        _customOutputText.Dock = DockStyle.Fill;
        _customOutputText.ReadOnly = true;
        _customOutputText.BackColor = Color.FromArgb(246, 248, 245);
        _customOutputText.BorderStyle = BorderStyle.FixedSingle;
        _customOutputText.Margin = new Padding(0, 4, 6, 0);

        _browseOutputButton.Text = "Escolher";
        _browseOutputButton.Width = 86;
        _browseOutputButton.Height = 30;
        _browseOutputButton.BackColor = Theme.GreenSoft;
        _browseOutputButton.ForeColor = Theme.GreenStrong;
        _browseOutputButton.Margin = new Padding(0, 4, 0, 0);

        container.Controls.Add(_customOutputText, 0, 2);
        container.Controls.Add(_browseOutputButton, 1, 2);

        return container;
    }

    private Control BuildFilesPanel()
    {
        var panel = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var top = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleWrap = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0)
        };
        titleWrap.Controls.Add(SectionTitle("Vídeos selecionados"));
        _fileCount.AutoSize = true;
        _fileCount.Text = "Nenhum arquivo";
        _fileCount.ForeColor = Theme.Muted;
        titleWrap.Controls.Add(_fileCount);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };

        var addButton = SecondaryButton("Adicionar vídeos", 126);
        var removeButton = SecondaryButton("Remover", 88);
        var clearButton = SecondaryButton("Limpar", 76);
        addButton.Click += (_, _) => SelectFiles();
        removeButton.Click += (_, _) => RemoveSelected();
        clearButton.Click += (_, _) => ClearFiles();

        actions.Controls.AddRange([addButton, removeButton, clearButton]);
        top.Controls.Add(titleWrap, 0, 0);
        top.Controls.Add(actions, 1, 0);

        var dropHint = new Label
        {
            AutoSize = true,
            Text = "Arraste arquivos MP4 ou AVI para esta janela, ou use “Adicionar vídeos”.",
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 8, 0, 10)
        };

        ConfigureGrid();

        var progressPanel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0, 12, 0, 0)
        };

        _currentFileLabel.AutoSize = true;
        _currentFileLabel.Text = "Aguardando";
        _currentFileLabel.ForeColor = Theme.Ink;
        _currentFileLabel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

        _overallLabel.AutoSize = true;
        _overallLabel.Text = "Progresso geral: 0%";
        _overallLabel.ForeColor = Theme.Muted;
        _overallLabel.Margin = new Padding(0, 8, 0, 2);

        progressPanel.Controls.Add(_currentFileLabel, 0, 0);
        progressPanel.Controls.Add(_fileProgress, 0, 1);
        progressPanel.Controls.Add(_overallLabel, 0, 2);
        progressPanel.Controls.Add(_overallProgress, 0, 3);

        var bottomActions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0)
        };

        _openOutputButton.Text = "Abrir pasta de saída";
        _openOutputButton.Width = 170;
        _openOutputButton.BackColor = Theme.GreenSoft;
        _openOutputButton.ForeColor = Theme.GreenStrong;
        _openOutputButton.Enabled = false;
        bottomActions.Controls.Add(_openOutputButton);

        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(dropHint, 0, 1);
        layout.Controls.Add(_fileGrid, 0, 2);
        layout.Controls.Add(progressPanel, 0, 3);
        layout.Controls.Add(bottomActions, 0, 4);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            BackColor = Theme.Cream,
            Padding = new Padding(18, 7, 18, 7)
        };

        var text = new Label
        {
            AutoSize = true,
            Text = "Processamento 100% local • O vídeo é copiado sem recodificação • Projeto e desenvolvimento: poLoNes",
            ForeColor = Theme.Muted,
            Location = new Point(18, 14)
        };

        var logo = new PictureBox
        {
            Image = TryLoadImage(Path.Combine(AppPaths.AssetsDirectory, "logo-polones-footer.png")),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(74, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(1120, 7)
        };

        footer.Resize += (_, _) => logo.Left = footer.ClientSize.Width - logo.Width - 18;
        footer.Controls.AddRange([text, logo]);
        return footer;
    }

    private void ConfigureGrid()
    {
        _fileGrid.Dock = DockStyle.Fill;
        _fileGrid.AutoGenerateColumns = false;
        _fileGrid.DataSource = _items;
        _fileGrid.AllowUserToAddRows = false;
        _fileGrid.AllowUserToDeleteRows = false;
        _fileGrid.AllowUserToResizeRows = false;
        _fileGrid.ReadOnly = true;
        _fileGrid.MultiSelect = true;
        _fileGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _fileGrid.RowHeadersVisible = false;
        _fileGrid.BackgroundColor = Color.White;
        _fileGrid.BorderStyle = BorderStyle.FixedSingle;
        _fileGrid.GridColor = Theme.Line;
        _fileGrid.EnableHeadersVisualStyles = false;
        _fileGrid.ColumnHeadersDefaultCellStyle.BackColor = Theme.GreenSoft;
        _fileGrid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.GreenStrong;
        _fileGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _fileGrid.ColumnHeadersHeight = 36;
        _fileGrid.RowTemplate.Height = 34;
        _fileGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 232, 220);
        _fileGrid.DefaultCellStyle.SelectionForeColor = Theme.Ink;

        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MediaFileItem.FileName),
            HeaderText = "Arquivo",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 220
        });
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MediaFileItem.Extension),
            HeaderText = "Tipo",
            Width = 64
        });
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MediaFileItem.SizeText),
            HeaderText = "Tamanho",
            Width = 86
        });
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MediaFileItem.DurationText),
            HeaderText = "Duração",
            Width = 78
        });
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MediaFileItem.Progress),
            HeaderText = "%",
            Width = 48
        });
        _fileGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(MediaFileItem.Status),
            HeaderText = "Status",
            Width = 150
        });

        _fileGrid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var status = _items[e.RowIndex].Status;

            if (status.StartsWith("Concluído", StringComparison.OrdinalIgnoreCase))
                e.CellStyle.ForeColor = Theme.GreenStrong;
            else if (status.StartsWith("Erro", StringComparison.OrdinalIgnoreCase))
                e.CellStyle.ForeColor = Theme.Danger;
            else if (status.StartsWith("Ignorado", StringComparison.OrdinalIgnoreCase))
                e.CellStyle.ForeColor = Theme.Warning;
        };
    }

    private void WireEvents()
    {
        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };

        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                AddFiles(paths);
        };

        _startButton.Click += async (_, _) => await StartProcessingAsync();
        _cancelButton.Click += (_, _) => _processingCancellation?.Cancel();
        _openOutputButton.Click += (_, _) => OpenLastOutput();
        _browseOutputButton.Click += (_, _) => BrowseOutputFolder();

        _automaticOutputRadio.CheckedChanged += (_, _) => UpdateOutputControls();
        _customOutputRadio.CheckedChanged += (_, _) => UpdateOutputControls();

        FormClosing += (_, e) =>
        {
            if (!_isProcessing) return;

            var answer = MessageBox.Show(
                "Há um processamento em andamento. Deseja cancelar e fechar?",
                "Fechar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _processingCancellation?.Cancel();
        };
    }

    private async Task ValidateToolsAsync()
    {
        _toolStatus.Text = "Verificando FFmpeg BtbN...";
        _toolStatus.ForeColor = Theme.Muted;
        _startButton.Enabled = false;

        var result = await _ffmpeg.ValidateToolsAsync();

        _ffmpegVersion = result.VersionLine;
        _toolStatus.Text = result.Message;
        _toolStatus.ForeColor = result.Success ? Theme.GreenStrong : Theme.Danger;

        UpdateStartState();

        if (!result.Success)
        {
            MessageBox.Show(
                $"{result.Message}\n\nEsta V02 não baixa executáveis. O FFmpeg deve vir incluído no pacote portátil oficial.",
                "FFmpeg não disponível",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ApplySettings()
    {
        if (_gainButtons.TryGetValue(_settings.GainDb, out var gain))
            gain.Checked = true;
        else
            _gainButtons[15].Checked = true;

        _limiterCheck.Checked = _settings.UseLimiter;
        _metadataCheck.Checked = _settings.PreserveMetadata;
        _automaticOutputRadio.Checked = _settings.AutomaticOutputFolder;
        _customOutputRadio.Checked = !_settings.AutomaticOutputFolder;
        _customOutputText.Text = _settings.CustomOutputFolder;
        UpdateOutputControls();
        UpdateFileCount();
    }

    private AppSettings ReadSettings() => new()
    {
        GainDb = GetSelectedGain(),
        UseLimiter = _limiterCheck.Checked,
        PreserveMetadata = _metadataCheck.Checked,
        AutomaticOutputFolder = _automaticOutputRadio.Checked,
        CustomOutputFolder = _customOutputText.Text.Trim()
    };

    private void SelectFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Selecione os vídeos",
            Filter = "Vídeos compatíveis (*.mp4;*.avi)|*.mp4;*.avi|MP4 (*.mp4)|*.mp4|AVI (*.avi)|*.avi",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddFiles(dialog.FileNames);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        if (_isProcessing) return;

        var existing = new HashSet<string>(
            _items.Select(i => i.FullPath),
            StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var raw in paths)
        {
            try
            {
                var path = Path.GetFullPath(raw);
                var extension = Path.GetExtension(path).ToLowerInvariant();

                if (!File.Exists(path) || (extension != ".mp4" && extension != ".avi"))
                    continue;

                if (!existing.Add(path))
                    continue;

                _items.Add(new MediaFileItem { FullPath = path });
                added++;
            }
            catch (Exception ex)
            {
                AppLog.WriteException($"Falha ao adicionar arquivo: {raw}", ex);
            }
        }

        if (added == 0)
        {
            MessageBox.Show(
                "Nenhum arquivo MP4 ou AVI novo foi adicionado.",
                "Adicionar vídeos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        UpdateFileCount();
        UpdateStartState();
    }

    private void RemoveSelected()
    {
        if (_isProcessing) return;

        var selected = _fileGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem as MediaFileItem)
            .Where(item => item is not null)
            .Cast<MediaFileItem>()
            .ToList();

        foreach (var item in selected)
            _items.Remove(item);

        UpdateFileCount();
        UpdateStartState();
    }

    private void ClearFiles()
    {
        if (_isProcessing || _items.Count == 0) return;

        _items.Clear();
        _fileProgress.Value = 0;
        _overallProgress.Value = 0;
        _currentFileLabel.Text = "Aguardando";
        _overallLabel.Text = "Progresso geral: 0%";
        UpdateFileCount();
        UpdateStartState();
    }

    private void UpdateFileCount()
    {
        _fileCount.Text = _items.Count switch
        {
            0 => "Nenhum arquivo",
            1 => "1 arquivo",
            _ => $"{_items.Count} arquivos"
        };
    }

    private void UpdateStartState()
    {
        var customReady = _automaticOutputRadio.Checked ||
                          Directory.Exists(_customOutputText.Text.Trim());

        var toolsReady = File.Exists(AppPaths.FfmpegPath) &&
                         File.Exists(AppPaths.FfprobePath);

        _startButton.Enabled = !_isProcessing &&
                               _items.Count > 0 &&
                               customReady &&
                               toolsReady;
    }

    private void UpdateOutputControls()
    {
        var enabled = _customOutputRadio.Checked && !_isProcessing;
        _customOutputText.Enabled = enabled;
        _browseOutputButton.Enabled = enabled;
        UpdateStartState();
    }

    private void BrowseOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Escolha a pasta onde os novos vídeos serão salvos",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (Directory.Exists(_customOutputText.Text))
            dialog.SelectedPath = _customOutputText.Text;

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _customOutputText.Text = dialog.SelectedPath;
            UpdateStartState();
        }
    }

    private int GetSelectedGain() =>
        _gainButtons.FirstOrDefault(pair => pair.Value.Checked).Key is var value && value > 0
            ? value
            : 15;

    private async Task StartProcessingAsync()
    {
        if (_isProcessing || _items.Count == 0) return;

        var settings = ReadSettings();

        if (!settings.AutomaticOutputFolder &&
            !Directory.Exists(settings.CustomOutputFolder))
        {
            MessageBox.Show(
                "Escolha uma pasta de saída válida.",
                "Pasta de saída",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        SettingsService.Save(settings);
        _processingCancellation = new CancellationTokenSource();
        _isProcessing = true;
        SetProcessingUi(true);

        var completed = 0;
        var errors = 0;
        var ignored = 0;

        try
        {
            for (var index = 0; index < _items.Count; index++)
            {
                _processingCancellation.Token.ThrowIfCancellationRequested();

                var item = _items[index];
                item.Progress = 0;
                item.Status = "Analisando";
                _currentFileLabel.Text = $"Arquivo {index + 1} de {_items.Count}: {item.FileName}";
                _fileProgress.Value = 0;

                try
                {
                    if (!await _ffmpeg.HasAudioStreamAsync(item.FullPath, _processingCancellation.Token))
                    {
                        item.Status = "Ignorado: sem áudio";
                        ignored++;
                        UpdateOverallProgress(index + 1, _items.Count);
                        continue;
                    }

                    item.DurationSeconds = await _ffmpeg.GetDurationSecondsAsync(
                        item.FullPath,
                        _processingCancellation.Token);

                    var output = BuildOutputPath(item.FullPath, settings);
                    item.OutputPath = output;
                    _lastOutputDirectory = Path.GetDirectoryName(output);

                    item.Status = "Processando";

                    var progress = new Progress<int>(percent =>
                    {
                        item.Progress = percent;
                        _fileProgress.Value = percent;

                        var overall = (int)Math.Round(
                            ((index + percent / 100d) / _items.Count) * 100d);
                        _overallProgress.Value = Math.Clamp(overall, 0, 100);
                        _overallLabel.Text = $"Progresso geral: {_overallProgress.Value}%";
                    });

                    var exitCode = await _ffmpeg.ConvertAsync(
                        item.FullPath,
                        new ConversionOptions(
                            settings.GainDb,
                            settings.UseLimiter,
                            settings.PreserveMetadata,
                            output),
                        item.DurationSeconds,
                        progress,
                        _processingCancellation.Token);

                    if (exitCode != 0)
                    {
                        TryDelete(output);
                        item.Status = $"Erro: FFmpeg ({exitCode})";
                        errors++;
                    }
                    else
                    {
                        if (settings.PreserveMetadata)
                            PreserveFileDates(item.FullPath, output);

                        item.Progress = 100;
                        item.Status = "Concluído";
                        completed++;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (item.OutputPath is not null)
                        TryDelete(item.OutputPath);

                    item.Status = "Cancelado";
                    throw;
                }
                catch (Exception ex)
                {
                    AppLog.WriteException($"Falha em {item.FullPath}", ex);
                    if (item.OutputPath is not null)
                        TryDelete(item.OutputPath);

                    item.Status = $"Erro: {ex.Message}";
                    errors++;
                }

                UpdateOverallProgress(index + 1, _items.Count);
            }

            _overallProgress.Value = 100;
            _overallLabel.Text = "Progresso geral: 100%";
            _currentFileLabel.Text = "Processamento concluído.";
            _openOutputButton.Enabled = !string.IsNullOrWhiteSpace(_lastOutputDirectory);

            MessageBox.Show(
                $"Processamento concluído.\n\nConvertidos: {completed}\nIgnorados: {ignored}\nErros: {errors}\n\nO vídeo original não foi recodificado.",
                "iNat TrailCam Audio Booster",
                MessageBoxButtons.OK,
                errors > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            _currentFileLabel.Text = "Processamento cancelado.";
            MessageBox.Show(
                "O processamento foi cancelado. Arquivos incompletos foram removidos.",
                "Cancelado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        finally
        {
            _processingCancellation.Dispose();
            _processingCancellation = null;
            _isProcessing = false;
            SetProcessingUi(false);
            UpdateStartState();
        }
    }

    private string BuildOutputPath(string inputPath, AppSettings settings)
    {
        var outputDirectory = settings.AutomaticOutputFolder
            ? Path.Combine(Path.GetDirectoryName(inputPath)!, "Audio_Aumentado")
            : settings.CustomOutputFolder;

        Directory.CreateDirectory(outputDirectory);

        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath).ToLowerInvariant();
        var proposed = Path.Combine(
            outputDirectory,
            $"{baseName}_audio_+{settings.GainDb}dB{extension}");

        if (!File.Exists(proposed))
            return proposed;

        for (var number = 2; number < 10000; number++)
        {
            var candidate = Path.Combine(
                outputDirectory,
                $"{baseName}_audio_+{settings.GainDb}dB_{number}{extension}");

            if (!File.Exists(candidate))
                return candidate;
        }

        throw new IOException("Não foi possível gerar um nome livre para o arquivo de saída.");
    }

    private static void PreserveFileDates(string source, string destination)
    {
        try
        {
            File.SetCreationTime(destination, File.GetCreationTime(source));
            File.SetLastWriteTime(destination, File.GetLastWriteTime(source));
            File.SetLastAccessTime(destination, File.GetLastAccessTime(source));
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Não foi possível preservar as datas do arquivo", ex);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            AppLog.WriteException($"Não foi possível remover arquivo parcial: {path}", ex);
        }
    }

    private void UpdateOverallProgress(int finished, int total)
    {
        var percent = total == 0 ? 0 : (int)Math.Round(finished / (double)total * 100d);
        _overallProgress.Value = Math.Clamp(percent, 0, 100);
        _overallLabel.Text = $"Progresso geral: {_overallProgress.Value}%";
    }

    private void SetProcessingUi(bool processing)
    {
        _startButton.Enabled = false;
        _cancelButton.Enabled = processing;
        _fileGrid.Enabled = !processing;
        _automaticOutputRadio.Enabled = !processing;
        _customOutputRadio.Enabled = !processing;
        _limiterCheck.Enabled = !processing;
        _metadataCheck.Enabled = !processing;

        foreach (var radio in _gainButtons.Values)
            radio.Enabled = !processing;

        UpdateOutputControls();
    }

    private void OpenLastOutput()
    {
        if (string.IsNullOrWhiteSpace(_lastOutputDirectory) ||
            !Directory.Exists(_lastOutputDirectory))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = _lastOutputDirectory,
            UseShellExecute = true
        });
    }

    private void ShowAbout()
    {
        var sourceFile = Path.Combine(AppPaths.LicensesDirectory, "FFmpeg-SOURCE.txt");
        var source = File.Exists(sourceFile)
            ? File.ReadAllText(sourceFile).Trim()
            : "FFmpeg BtbN incluído no pacote portátil.";

        MessageBox.Show(
            $"iNat TrailCam Audio Booster V02\n\n" +
            $"Aplicativo nativo em C#/.NET 10 para Windows 11.\n" +
            $"Processa somente o áudio e copia o vídeo sem recodificação.\n\n" +
            $"FFmpeg:\n{_ffmpegVersion}\n\n" +
            $"{source}\n\n" +
            $"Projeto e desenvolvimento: poLoNes",
            "Sobre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static Icon? LoadApplicationIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
            {
                var associated = Icon.ExtractAssociatedIcon(executablePath);
                if (associated is not null)
                    return associated;
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Não foi possível carregar o ícone incorporado ao executável", ex);
        }

        try
        {
            var externalPath = Path.Combine(AppPaths.AssetsDirectory, "app-icon.ico");
            if (File.Exists(externalPath))
                return new Icon(externalPath);
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Não foi possível carregar o ícone externo", ex);
        }

        return SystemIcons.Application;
    }

    private static Image? TryLoadImage(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                AppLog.Write($"Imagem opcional não encontrada: {path}");
                return null;
            }

            using var stream = File.OpenRead(path);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch (Exception ex)
        {
            AppLog.WriteException($"Não foi possível carregar a imagem: {path}", ex);
            return null;
        }
    }

    private static Label SectionTitle(string text) => new()
    {
        AutoSize = true,
        Text = text,
        ForeColor = Theme.Ink,
        Font = new Font("Segoe UI", 12f, FontStyle.Bold),
        Margin = new Padding(0, 0, 0, 2)
    };

    private static Label Hint(string text) => new()
    {
        AutoSize = true,
        MaximumSize = new Size(274, 0),
        Text = text,
        ForeColor = Theme.Muted,
        Margin = new Padding(0, 0, 0, 4)
    };

    private static ModernButton SecondaryButton(string text, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 34,
        BackColor = Theme.GreenSoft,
        ForeColor = Theme.GreenStrong,
        Margin = new Padding(4, 0, 0, 0)
    };
}
