using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AntiScam.Blog.Api.Models;

namespace AntiScam.Blog.Api.Services;

public sealed class PythonAiBlockExplanationProvider(
    WorkspaceOptions workspace,
    ILogger<PythonAiBlockExplanationProvider> logger) : IBlockExplanationProvider
{
    public async Task<string> ExplainAsync(BlogPostInput input, RiskAssessment risk, CancellationToken cancellationToken)
    {
        var text = string.Join(" ", input.Title, input.Summary, input.Content, input.Author);
        var payload = JsonSerializer.Serialize(new
        {
            text,
            scan_status = risk.Status,
            risk_score = risk.RiskScore,
            scan_reasons = risk.Reasons
        });
        var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"-m antiscam.ai {encodedPayload}",
            WorkingDirectory = workspace.RootPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                logger.LogError("Could not start the Python AI explanation process.");
                return risk.BlockExplanation;
            }

            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                logger.LogWarning(
                    "Python AI explanation failed with exit code {ExitCode}: {Error}",
                    process.ExitCode,
                    error);
                return risk.BlockExplanation;
            }

            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            var explanation = root.GetProperty("explanation").GetString();
            var action = root.GetProperty("recommended_action").GetString();

            return string.IsNullOrWhiteSpace(action)
                ? explanation ?? risk.BlockExplanation
                : $"{explanation} Zalecenie: {action}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Python AI explanation failed; returning the built-in risk explanation.");
            return risk.BlockExplanation;
        }
    }
}
