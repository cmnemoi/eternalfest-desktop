using EternalfestDesktop.Application;

namespace EternalfestDesktop.Infrastructure.EternalfestApi;

/// <summary>Anonymous, read-only requests to eternalfest.net that identify the app.</summary>
/// @spec catalog::read-only-anonymous
internal static class EternalfestHttp
{
    public static readonly string UserAgent =
        $"EternalfestDesktop/{typeof(EternalfestHttp).Assembly.GetName().Version?.ToString(3)} (unofficial offline launcher)";

    public static async Task<HttpResponseMessage> Get(this HttpClient http, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        try
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new EternalfestUnreachableException("Eternalfest can't be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EternalfestUnreachableException("Eternalfest took too long to answer.", exception);
        }
    }

    public static async Task EnsureSuccess(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw new EternalfestUnreachableException(
                $"Eternalfest answered {(int)response.StatusCode} to {response.RequestMessage?.RequestUri}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
    }
}
