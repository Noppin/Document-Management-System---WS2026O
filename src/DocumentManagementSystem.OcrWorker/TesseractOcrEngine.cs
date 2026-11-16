using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.OcrWorker;

public sealed class TesseractOcrEngine(
    IOptions<OcrOptions> options,
    ILogger<TesseractOcrEngine> logger) : IOcrEngine
{
    private readonly OcrOptions _options = options.Value;

    public async Task<string> RecognizeAsync(
        byte[] pdfContent,
        CancellationToken cancellationToken = default)
    {
        var workingDirectory = Path.Combine(
            Path.GetTempPath(),
            $"dms-ocr-{Guid.NewGuid():N}");

        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pdfPath = Path.Combine(workingDirectory, "input.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfContent, cancellationToken);

            var pagePattern = Path.Combine(workingDirectory, "page-%03d.png");
            await RunGhostscriptAsync(pdfPath, pagePattern, cancellationToken);

            var pages = Directory
                .EnumerateFiles(workingDirectory, "page-*.png")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (pages.Length == 0)
            {
                throw new OcrExecutionException("Ghostscript did not render any PDF pages.");
            }

            var recognizedText = new StringBuilder();

            foreach (var page in pages)
            {
                var text = await RunTesseractAsync(page, cancellationToken);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (recognizedText.Length > 0)
                    {
                        recognizedText.AppendLine();
                    }

                    recognizedText.Append(text.Trim());
                }
            }

            logger.LogInformation(
                "OCR completed for {PageCount} rendered PDF page(s) with {CharacterCount} recognized characters",
                pages.Length,
                recognizedText.Length);

            return recognizedText.ToString();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OcrExecutionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new OcrExecutionException("OCR processing failed.", exception);
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
    }

    private async Task RunGhostscriptAsync(
        string pdfPath,
        string pagePattern,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateProcessStartInfo("gs");
        startInfo.ArgumentList.Add("-dSAFER");
        startInfo.ArgumentList.Add("-dBATCH");
        startInfo.ArgumentList.Add("-dNOPAUSE");
        startInfo.ArgumentList.Add("-sDEVICE=png16m");
        startInfo.ArgumentList.Add($"-r{_options.Dpi}");
        startInfo.ArgumentList.Add($"-sOutputFile={pagePattern}");
        startInfo.ArgumentList.Add(pdfPath);

        var result = await RunProcessAsync(startInfo, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new OcrExecutionException(
                $"Ghostscript failed with exit code {result.ExitCode}: {result.StandardError}");
        }
    }

    private async Task<string> RunTesseractAsync(
        string imagePath,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateProcessStartInfo("tesseract");
        startInfo.ArgumentList.Add(imagePath);
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(_options.Language);
        startInfo.ArgumentList.Add("--psm");
        startInfo.ArgumentList.Add("6");

        var result = await RunProcessAsync(startInfo, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new OcrExecutionException(
                $"Tesseract failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        return result.StandardOutput;
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName) => new()
    {
        FileName = fileName,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    private static async Task<ProcessResult> RunProcessAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new OcrExecutionException($"Could not start process {startInfo.FileName}.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Do not leave gs/tesseract running when the worker shuts down.
            process.Kill(entireProcessTree: true);
            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
