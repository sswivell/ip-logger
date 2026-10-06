using System.Collections.Concurrent;
using System.Text.Json;

namespace Swivel.Core;

/// <summary>Geolocation and network ownership for one address.</summary>
public sealed record IpInfo
{
    public string CountryCode { get; init; } = "?";
    public string Country { get; init; } = "?";
    public string City { get; init; } = "?";
    public string Region { get; init; } = "?";
    public string Isp { get; init; } = "?";
    public string Organization { get; init; } = "?";
    public string AutonomousSystem { get; init; } = "?";
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public bool Proxy { get; init; }
    public bool Hosting { get; init; }
    public bool Tor { get; init; }

    public string ShortLocation
    {
        get
        {
            var parts = new[] { City, Region, Country }
                .Where(p => p.Length > 0 && p != "?")
                .ToArray();
            return parts.Length == 0 ? "?" : string.Join(", ", parts);
        }
    }
}

/// <summary>ip-api.com lookups plus the Tor bulk exit address list.</summary>
public static class Geo
{
    const string Endpoint = "http://ip-api.com/json/{0}?fields=66846719";
    const string ExitListUrl = "https://check.torproject.org/torbulkexitlist";

    static readonly ConcurrentDictionary<string, IpInfo> Cache = new();
    static HashSet<string> _exits = new(StringComparer.Ordinal);

    public static int ExitCount
    {
        get { lock (_exits) { return _exits.Count; } }
    }

    /// <summary>Refresh the Tor exit list; failures leave the previous set intact.</summary>
    public static async Task LoadExitListAsync(CancellationToken token = default)
    {
        try
        {
            var text = await Net.GetStringAsync(ExitListUrl, token).ConfigureAwait(false);
            var exits = new HashSet<string>(StringComparer.Ordinal);
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length > 0 && !line.StartsWith('#'))
                    exits.Add(line);
            }
            lock (_exits)
            {
                _exits = exits;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Offline or blocked; Tor tagging simply stays disabled.
        }
    }

    public static IpInfo Lookup(string ip)
    {
        var cached = Cache.GetOrAdd(ip, _ => Fetch(ip));
        return cached with { Tor = IsExit(ip) };
    }

    static bool IsExit(string ip)
    {
        lock (_exits)
        {
            return _exits.Contains(ip);
        }
    }

static IpInfo Fetch(string ip)
    {
        try
        {
            var body = Net.GetStringAsync(Endpoint.Replace("{0}", ip, StringComparison.Ordinal))
                .ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("status", out var status) && status.GetString() != "success")
                return new IpInfo();
            return new IpInfo
            {
                CountryCode = Str(root, "countryCode"),
                Country = Str(root, "country"),
                City = Str(root, "city"),
                Region = Str(root, "regionName"),
                Isp = Str(root, "isp"),
                Organization = Str(root, "org"),
                AutonomousSystem = Str(root, "as"),
                Latitude = Num(root, "lat"),
                Longitude = Num(root, "lon"),
                Proxy = Flag(root, "proxy"),
                Hosting = Flag(root, "hosting"),
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new IpInfo();
        }
    }

    static string Str(JsonElement doc, string name) =>
        doc.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "?"
            : "?";

    static double? Num(JsonElement doc, string name) =>
        doc.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            && v.TryGetDouble(out var d) ? d : null;

    static bool Flag(JsonElement doc, string name) =>
        doc.TryGetProperty(name, out var v)
        && v.ValueKind is JsonValueKind.True or JsonValueKind.Number
        && (v.ValueKind == JsonValueKind.True || v.GetDouble() != 0);

    /// <summary>Regional-indicator flag emoji for a 2-letter country code.</summary>
    public static string FlagEmoji(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 2)
            return "";
        return string.Concat(code.ToUpperInvariant().Select(c => (char)(0x1F1E6 + c - 'A')));
    }
}