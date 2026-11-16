// Mit AI geschrieben.
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.OcrWorker.Tests;

public sealed class TesseractOcrEngineIntegrationTests
{
    [OcrFact, Trait("Category", "Integration")]
    public async Task Recognize_RealTwoPagePdfContainsExpectedTextFromBothPages()
    {
        var output = Path.Combine(Path.GetTempPath(), $"dms-two-page-{Guid.NewGuid():N}.pdf");
        try
        {
            var input = Path.Combine(AppContext.BaseDirectory, "HelloWorld.pdf");
            var info = new ProcessStartInfo("gs") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (var argument in new[] { "-q", "-dBATCH", "-dNOPAUSE", "-sDEVICE=pdfwrite", $"-sOutputFile={output}", input, input })
                info.ArgumentList.Add(argument);
            using var process = Process.Start(info)!;
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            await stdout;
            Assert.True(process.ExitCode == 0, await stderr);

            var engine = new TesseractOcrEngine(Options.Create(new OcrOptions()), NullLogger<TesseractOcrEngine>.Instance);
            var text = await engine.RecognizeAsync(await File.ReadAllBytesAsync(output, timeout.Token), timeout.Token);
            Assert.True(Regex.Matches(text, "Hello", RegexOptions.IgnoreCase).Count >= 2, text);
        }
        finally
        {
            if (File.Exists(output)) File.Delete(output);
        }
    }

    public sealed class OcrFactAttribute : FactAttribute
    {
        public OcrFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("DMS_INTEGRATION_TESTS") != "true")
                Skip = "Set DMS_INTEGRATION_TESTS=true and install Ghostscript/Tesseract to run real OCR.";
        }
    }
}
