using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace INatTrailCamAudioBooster;

internal sealed class MediaFileItem : INotifyPropertyChanged
{
    private string _status = "Aguardando";
    private int _progress;
    private double? _durationSeconds;
    private string? _outputPath;

    public required string FullPath { get; init; }
    public string FileName => Path.GetFileName(FullPath);
    public string Extension => Path.GetExtension(FullPath).TrimStart('.').ToUpperInvariant();
    public long SizeBytes => new FileInfo(FullPath).Length;
    public string SizeText => FormatBytes(SizeBytes);
    public string DurationText => DurationSeconds is null ? "—" : FormatDuration(DurationSeconds.Value);

    public double? DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (_durationSeconds == value) return;
            _durationSeconds = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DurationText));
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public int Progress
    {
        get => _progress;
        set
        {
            var safe = Math.Clamp(value, 0, 100);
            if (_progress == safe) return;
            _progress = safe;
            OnPropertyChanged();
        }
    }

    public string? OutputPath
    {
        get => _outputPath;
        set
        {
            if (_outputPath == value) return;
            _outputPath = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private static string FormatDuration(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1
            ? time.ToString(@"hh\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }
}

internal sealed class AppSettings
{
    public int GainDb { get; set; } = 15;
    public bool UseLimiter { get; set; } = true;
    public bool PreserveMetadata { get; set; } = true;
    public bool AutomaticOutputFolder { get; set; } = true;
    public string CustomOutputFolder { get; set; } = "";
}
