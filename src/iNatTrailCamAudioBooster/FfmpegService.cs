using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace INatTrailCamAudioBooster;

internal sealed record ToolValidationResult(bool Success, string Message, string VersionLine);

internal sealed record ConversionOptions(
    int GainDb,
    bool UseLimiter,
    bool PreserveMetadata,
    string OutputPath);

internal sealed class FfmpegService
{
    public async Task<ToolValidationResult> ValidateToolsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(AppPaths.FfmpegPath))
            return new(false, $"ffmpeg.exe não encontrado em:\n{AppPaths.FfmpegPath}", "");

        if (!File.Exists(AppPaths.FfprobePath))
            return new(false, $"ffprobe.exe não encontrado em:\n{AppPaths.FfprobePath}", "");

        try
        {
            var hashValidation = await ValidatePublishedHashesAsync(cancellationToken);
            if (!hashValidation.Success)
                return hashValidation;

            var version = await RunAndCaptureAsync(
                AppPaths.FfmpegPath,
                ["-hide_banner", "-version"],
                cancellationToken);

            if (version.ExitCode != 0)
                return new(false, $"O FFmpeg foi encontrado, mas não conseguiu iniciar.\n\n{version.Error}", "");

            var line = version.Output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "FFmpeg BtbN";

            return new(true, "FFmpeg BtbN incluído e validado.", line);
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Falha ao validar o FFmpeg", ex);
            return new(false, $"Falha ao validar o FFmpeg:\n{ex.Message}", "");
        }
    }

    public async Task<double?> GetDurationSecondsAsync(string inputPath, CancellationToken cancellationToken)
    {
        var result = await RunAndCaptureAsync(
            AppPaths.FfprobePath,
            [
                "-v", "error",
                "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1",
                inputPath
            ],
            cancellationToken);

        if (result.ExitCode != 0)
            return null;

        return double.TryParse(
            result.Output.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var seconds)
            ? seconds
            : null;
    }

    public async Task<bool> HasAudioStreamAsync(string inputPath, CancellationToken cancellationToken)
    {
        var result = await RunAndCaptureAsync(
            AppPaths.FfprobePath,
            [
                "-v", "error",
                "-select_streams", "a:0",
                "-show_entries", "stream=index",
                "-of", "csv=p=0",
                inputPath
            ],
            cancellationToken);

        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output);
    }

    public async Task<int> ConvertAsync(
        string inputPath,
        ConversionOptions options,
        double? durationSeconds,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(inputPath).ToLowerInvariant();
        var filter = options.UseLimiter
            ? $"volume={options.GainDb}dB,alimiter=limit=0.98:attack=5:release=50"
            : $"volume={options.GainDb}dB";

        var arguments = new List<string>
        {
            "-hide_banner",
            "-nostdin",
            "-y",
            "-i", inputPath,
            "-map", "0:v:0",
            "-map", "0:a:0"
        };

        if (options.PreserveMetadata)
        {
            arguments.AddRange(["-map_metadata", "0"]);
        }

        arguments.AddRange([
            "-c:v", "copy",
            "-af", filter
        ]);

        if (extension == ".mp4")
        {
            arguments.AddRange([
                "-c:a", "aac",
                "-b:a", "192k",
                "-movflags", "+faststart"
            ]);
        }
        else
        {
            arguments.AddRange([
                "-c:a", "libmp3lame",
                "-b:a", "192k"
            ]);
        }

        arguments.AddRange([
            "-progress", "pipe:1",
            "-nostats",
            options.OutputPath
        ]);

        var psi = new ProcessStartInfo
        {
            FileName = AppPaths.FfmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppPaths.BaseDirectory
        };

        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        AppLog.Write($"FFmpeg: {BuildDisplayCommand(psi.FileName, arguments)}");

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!process.Start())
            throw new InvalidOperationException("Não foi possível iniciar o FFmpeg.");

        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            {
                if (line.StartsWith("out_time_us=", StringComparison.Ordinal))
                {
                    if (durationSeconds is > 0 &&
                        long.TryParse(line.AsSpan("out_time_us=".Length), out var microseconds))
                    {
                        var currentSeconds = microseconds / 1_000_000d;
                        var percent = (int)Math.Round(currentSeconds / durationSeconds.Value * 100d);
                        progress.Report(Math.Clamp(percent, 0, 99));
                    }
                }
                else if (line.Equals("progress=end", StringComparison.Ordinal))
                {
                    progress.Report(100);
                }
            }

            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var error = await errorTask;
        if (!string.IsNullOrWhiteSpace(error))
            AppLog.Write(error.Trim());

        return process.ExitCode;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Não foi possível encerrar o FFmpeg", ex);
        }
    }

    private static string BuildDisplayCommand(string executable, IEnumerable<string> arguments)
    {
        static string Quote(string value) =>
            value.Any(char.IsWhiteSpace) || value.Contains('"')
                ? $"\"{value.Replace("\"", "\\\"")}\""
                : value;

        return $"{Quote(executable)} {string.Join(" ", arguments.Select(Quote))}";
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAndCaptureAsync(
        string executable,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppPaths.BaseDirectory
        };

        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
            throw new InvalidOperationException($"Não foi possível iniciar {Path.GetFileName(executable)}.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return (process.ExitCode, await outputTask, await errorTask);
    }

    private static async Task<ToolValidationResult> ValidatePublishedHashesAsync(
        CancellationToken cancellationToken)
    {
        var hashFile = Path.Combine(AppPaths.LicensesDirectory, "FFmpeg-SHA256.txt");
        if (!File.Exists(hashFile))
            return new(true, "Arquivo de hashes não encontrado; executáveis serão testados diretamente.", "");

        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in await File.ReadAllLinesAsync(hashFile, cancellationToken))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            var parts = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                expected[Path.GetFileName(parts[^1].TrimStart('*'))] = parts[0];
        }

        foreach (var path in new[] { AppPaths.FfmpegPath, AppPaths.FfprobePath })
        {
            var name = Path.GetFileName(path);
            if (!expected.TryGetValue(name, out var expectedHash))
                continue;

            await using var stream = File.OpenRead(path);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));

            if (!actual.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                return new(
                    false,
                    $"A verificação de integridade falhou para {name}.\n\nEsperado: {expectedHash}\nObtido: {actual}",
                    "");
            }
        }

        return new(true, "Integridade do FFmpeg confirmada.", "");
    }
}
