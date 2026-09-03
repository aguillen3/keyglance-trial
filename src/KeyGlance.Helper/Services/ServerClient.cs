using System.Net;
using System.Net.Http.Json;
using KeyGlance.Helper.Models;

namespace KeyGlance.Helper.Services;

public sealed class ServerClient(HttpClient http)
{
    public async Task<ImportJob?> ClaimAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync("/claim", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ImportJob>(cancellationToken);
    }

    public async Task ReportResultAsync(
        string jobId,
        ImportResult result,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            status = result.Status.ToString().ToLowerInvariant(),
            landedFields = result.LandedFields,
            reason = result.Reason
        };

        using var response = await http.PostAsJsonAsync(
            $"/jobs/{Uri.EscapeDataString(jobId)}/result",
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
