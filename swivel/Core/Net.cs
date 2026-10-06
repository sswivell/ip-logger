namespace Swivel.Core;

/// <summary>Shared HTTP client with short timeouts for outbound lookups.</summary>
public static class Net
{
    static readonly Lazy<HttpClient> Client = new(() => new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(6),
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0" },
        },
    });

    public static async Task<string> GetStringAsync(string url, CancellationToken token = default)
    {
        using var response = await Client.Value.GetAsync(url, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
    }

    public static async Task PostJsonAsync(string url, string json, CancellationToken token = default)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.Value.PostAsync(url, content, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Stream a remote file to disk, reporting bytes received.</summary>
    public static async Task DownloadAsync(string url, string path, Action<long> progress, CancellationToken token = default)
    {
        using var response = await Client.Value
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        await using var sink = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
        {
            await sink.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
            total += read;
            progress(total);
        }
    }
}