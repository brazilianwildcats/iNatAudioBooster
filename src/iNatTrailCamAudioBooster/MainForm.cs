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
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        MinimumSize = new Size(980, 680);

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);
        Size = new Size(
            Math.Min(1280, Math.Max(980, workingArea.Width - 40)),
            Math.Min(860, Math.Max(680, workingArea.Height - 40)));

        if (workingArea.Width < 1120 || workingArea.Height < 740)
            WindowState = FormWindowState.Maximized;

        BackColor = Theme.Background;
        Font = new Font("Segoe UI", 9f);
        AllowDrop = true;
        DoubleBuffered = true;

        BuildInterface();
        ApplySettings();
        WireEvents();

        Shown += async (_, _) => await ValidateToolsAsync();
    }

    private void BuildInterface()
    {
        SuspendLayout();

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Theme.Background
        };

        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        shell.Controls.Add(BuildHeader(), 0, 0);
        shell.Controls.Add(BuildMainArea(), 0, 1);
        shell.Controls.Add(BuildFooter(), 0, 2);

        Controls.Add(shell);
        ResumeLayout(performLayout: true);
    }

    private Control BuildHeader()
    {
        var header = new GradientHeader
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(18, 10, 18, 10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var logo = new PictureBox
        {
            Image = TryLoadImage(Path.Combine(AppPaths.AssetsDirectory, "inat-trailcam-logo.png")),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 12, 2),
            BackColor = Color.Transparent
        };

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "iNat TrailCam\r\nAudio Booster",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Aumente somente o áudio e preserve o vídeo sem recodificação",
            ForeColor = Color.FromArgb(230, 240, 232),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0)
        };

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

        var version = new Label
        {
            Dock = DockStyle.Fill,
            Text = "V02.4",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(4, 0, 4, 2)
        };

        var aboutButton = new ModernButton
        {
            Text = "Sobre",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(90, 255, 255, 255),
            ForeColor = Color.White,
            Radius = 16,
            Margin = new Padding(4, 2, 4, 4)
        };
        aboutButton.Click += (_, _) => ShowAbout();

        right.Controls.Add(version, 0, 0);
        right.Controls.Add(aboutButton, 0, 1);

        layout.Controls.Add(logo, 0, 0);
        layout.Controls.Add(title, 1, 0);
        layout.Controls.Add(subtitle, 2, 0);
        layout.Controls.Add(right, 3, 0);

        header.Controls.Add(layout);
        return header;
    }

    private Control BuildMainArea()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(16, 14, 16, 14),
            Margin = new Padding(0),
            ColumnCount = 2,
            RowCount = 1
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 370));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildOptionsPanel(), 0, 0);
        root.Controls.Add(BuildFilesPanel(), 1, 0);

        return root;
    }

    private Control BuildOptionsPanel()
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(8),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 12,
            BackColor = Color.Transparent,
            Padding = new Padding(10),
            Margin = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(SectionTitle("Ganho de áudio"), 0, 0);
        layout.Controls.Add(Hint("Escolha o ganho aplicado antes do limitador."), 0, 1);

        var warning = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            MaximumSize = new Size(320, 0),
            Text = "Ganhos acima de +30 dB podem elevar bastante o ruído e fazer o limitador atuar intensamente.",
            ForeColor = Theme.Warning,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Margin = new Padding(0, 6, 0, 8)
        };
        layout.Controls.Add(warning, 0, 2);
        layout.Controls.Add(BuildGainGrid(), 0, 3);

        var optionsTitle = SectionTitle("Opções");
        optionsTitle.Margin = new Padding(0, 14, 0, 2);
        layout.Controls.Add(optionsTitle, 0, 4);

        _limiterCheck.Text = "Aplicar limitador de áudio";
        _limiterCheck.AutoSize = true;
        _limiterCheck.Dock = DockStyle.Top;
        _limiterCheck.ForeColor = Theme.Ink;
        _limiterCheck.Margin = new Padding(0, 8, 0, 5);
        layout.Controls.Add(_limiterCheck, 0, 5);

        _metadataCheck.Text = "Preservar metadados e datas do arquivo";
        _metadataCheck.AutoSize = true;
        _metadataCheck.Dock = DockStyle.Top;
        _metadataCheck.ForeColor = Theme.Ink;
        _metadataCheck.Margin = new Padding(0, 5, 0, 10);
        layout.Controls.Add(_metadataCheck, 0, 6);

        var outputTitle = SectionTitle("Pasta de saída");
        outputTitle.Margin = new Padding(0, 12, 0, 2);
        layout.Controls.Add(outputTitle, 0, 7);
        layout.Controls.Add(BuildOutputControls(), 0, 8);

        _toolStatus.AutoSize = true;
        _toolStatus.Dock = DockStyle.Top;
        _toolStatus.MaximumSize = new Size(320, 0);
        _toolStatus.Text = "Verificando FFmpeg BtbN...";
        _toolStatus.ForeColor = Theme.Muted;
        _toolStatus.Margin = new Padding(0, 16, 0, 10);
        layout.Controls.Add(_toolStatus, 0, 9);

        var buttons = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 0)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        buttons.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _startButton.Text = "INICIAR";
        _startButton.Dock = DockStyle.Fill;
        _startButton.Enabled = false;
        _startButton.Margin = new Padding(0, 0, 5, 0);

        _cancelButton.Text = "Cancelar";
        _cancelButton.Dock = DockStyle.Fill;
        _cancelButton.BackColor = Theme.Danger;
        _cancelButton.Enabled = false;
        _cancelButton.Margin = new Padding(5, 0, 0, 0);

        buttons.Controls.Add(_startButton, 0, 0);
        buttons.Controls.Add(_cancelButton, 1, 0);
        layout.Controls.Add(buttons, 0, 10);

        var localNote = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            MaximumSize = new Size(320, 0),
            Text = "Processamento local: o aplicativo não envia os vídeos para a internet.",
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 8.25f),
            Margin = new Padding(0, 12, 0, 4)
        };
        layout.Controls.Add(localNote, 0, 11);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildGainGrid()
    {
        var gains = new[] { 10, 15, 20, 30, 40, 50, 60, 70, 80, 100 };
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        for (var row = 0; row < 5; row++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

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
                Tag = gain,
                AutoSize = false
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
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _automaticOutputRadio.Text = "Criar “Audio_Aumentado” ao lado do original";
        _automaticOutputRadio.AutoSize = true;
        _automaticOutputRadio.Dock = DockStyle.Top;
        _automaticOutputRadio.ForeColor = Theme.Ink;
        _automaticOutputRadio.Margin = new Padding(0, 2, 0, 5);
        container.Controls.Add(_automaticOutputRadio, 0, 0);

        _customOutputRadio.Text = "Usar uma pasta específica";
        _customOutputRadio.AutoSize = true;
        _customOutputRadio.Dock = DockStyle.Top;
        _customOutputRadio.ForeColor = Theme.Ink;
        _customOutputRadio.Margin = new Padding(0, 6, 0, 5);
        container.Controls.Add(_customOutputRadio, 0, 1);

        _customOutputText.Dock = DockStyle.Top;
        _customOutputText.ReadOnly = true;
        _customOutputText.BackColor = Color.FromArgb(246, 248, 245);
        _customOutputText.BorderStyle = BorderStyle.FixedSingle;
        _customOutputText.Margin = new Padding(0, 2, 0, 6);
        container.Controls.Add(_customOutputText, 0, 2);

        _browseOutputButton.Text = "Escolher pasta";
        _browseOutputButton.Dock = DockStyle.Top;
        _browseOutputButton.Height = 36;
        _browseOutputButton.BackColor = Theme.GreenSoft;
        _browseOutputButton.ForeColor = Theme.GreenStrong;
        _browseOutputButton.Margin = new Padding(0);
        container.Controls.Add(_browseOutputButton, 0, 3);

        return container;
    }

    private Control BuildFilesPanel()
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(16)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLine = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0)
        };

        titleLine.Controls.Add(SectionTitle("Vídeos selecionados"));

        _fileCount.AutoSize = true;
        _fileCount.Text = "Nenhum arquivo";
        _fileCount.ForeColor = Theme.Muted;
        _fileCount.Margin = new Padding(10, 5, 0, 0);
        titleLine.Controls.Add(_fileCount);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 8, 0, 4)
        };

        var addButton = SecondaryButton("Adicionar vídeos", 132);
        var removeButton = SecondaryButton("Remover", 92);
        var clearButton = SecondaryButton("Limpar", 80);

        addButton.Margin = new Padding(0, 0, 6, 6);
        removeButton.Margin = new Padding(0, 0, 6, 6);
        clearButton.Margin = new Padding(0, 0, 0, 6);

        addButton.Click += (_, _) => SelectFiles();
        removeButton.Click += (_, _) => RemoveSelected();
        clearButton.Click += (_, _) => ClearFiles();

        actions.Controls.AddRange([addButton, removeButton, clearButton]);

        var dropHint = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Text = "Arraste arquivos MP4 ou AVI para esta janela, ou use “Adicionar vídeos”.",
            ForeColor = Theme.Muted,
            MaximumSize = new Size(760, 0),
            Margin = new Padding(0, 0, 0, 10)
        };

        heading.Controls.Add(titleLine, 0, 0);
        heading.Controls.Add(actions, 0, 1);
        heading.Controls.Add(dropHint, 0, 2);

        ConfigureGrid();

        var progressPanel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0, 12, 0, 0)
        };
        progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _currentFileLabel.AutoSize = true;
        _currentFileLabel.Dock = DockStyle.Top;
        _currentFileLabel.Text = "Aguardando";
        _currentFileLabel.ForeColor = Theme.Ink;
        _currentFileLabel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _currentFileLabel.AutoEllipsis = true;
        _currentFileLabel.Margin = new Padding(0, 0, 0, 4);

        _fileProgress.Dock = DockStyle.Top;
        _fileProgress.Margin = new Padding(0, 0, 0, 7);

        _overallLabel.AutoSize = true;
        _overallLabel.Dock = DockStyle.Top;
        _overallLabel.Text = "Progresso geral: 0%";
        _overallLabel.ForeColor = Theme.Muted;
        _overallLabel.Margin = new Padding(0, 0, 0, 4);

        _overallProgress.Dock = DockStyle.Top;

        progressPanel.Controls.Add(_currentFileLabel, 0, 0);
        progressPanel.Controls.Add(_fileProgress, 0, 1);
        progressPanel.Controls.Add(_overallLabel, 0, 2);
        progressPanel.Controls.Add(_overallProgress, 0, 3);

        var bottomActions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Margin = new Padding(0, 10, 0, 0)
        };

        _openOutputButton.Text = "Abrir pasta de saída";
        _openOutputButton.Width = 176;
        _openOutputButton.Height = 38;
        _openOutputButton.BackColor = Theme.GreenSoft;
        _openOutputButton.ForeColor = Theme.GreenStrong;
        _openOutputButton.Enabled = false;
        _openOutputButton.Margin = new Padding(6, 0, 0, 0);
        bottomActions.Controls.Add(_openOutputButton);

        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Theme.Line, Margin = new Padding(0, 2, 0, 10) }, 0, 1);
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
            Dock = DockStyle.Fill,
            BackColor = Theme.Cream,
            Padding = new Padding(18, 6, 18, 6),
            Margin = new Padding(0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var text = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Processamento 100% local • Vídeo sem recodificação • Projeto e desenvolvimento: poLoNes",
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        var logo = new PictureBox
        {
            Image = TryLoadImage(Path.Combine(AppPaths.AssetsDirectory, "logo-polones-footer.png")),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0)
        };

        layout.Controls.Add(text, 0, 0);
        layout.Controls.Add(logo, 1, 0);
        footer.Controls.Add(layout);

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
        _fileGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        _fileGrid.ColumnHeadersHeight = 40;
        _fileGrid.RowTemplate.Height = 38;
        _fileGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _fileGrid.ScrollBars = ScrollBars.Both;
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
            $"iNat TrailCam Audio Booster V02.4\n\n" +
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
