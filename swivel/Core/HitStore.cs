using System.Collections.Concurrent;
using System.Text.Json;

namespace Swivel.Core;

/// <summary>Thread-safe store of recent hits plus the counters shown on STATS.</summary>
public sealed class HitStore
{
    public const int Limit = 200;

    readonly List<Hit> _hits = new();
    readonly ConcurrentDictionary<string, int> _countries = new(StringComparer.OrdinalIgnoreCase);
    readonly ConcurrentDictionary<string, int> _isps = new(StringComparer.OrdinalIgnoreCase);
    readonly object _gate = new();

    public int Total { get; private set; }
    public int Clean { get; private set; }
    public int AdBlock { get; private set; }
    public int Networked { get; private set; }
    public int Tor { get; private set; }
    public int Scanners { get; private set; }

    public List<Hit> Recent(int count)
    {
        lock (_gate)
        {
            return _hits.Skip(Math.Max(0, _hits.Count - count)).ToList();
        }
    }

    public Dictionary<string, int> TopCountries => Ranked(_countries);

    public Dictionary<string, int> TopIsps => Ranked(_isps);

    public void Clear()
    {
        lock (_gate)
        {
            _hits.Clear();
        }
    }

    static Dictionary<string, int> Ranked(ConcurrentDictionary<string, int> source) =>
        source.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, p => p.Value);

    internal void Add(Hit hit, string country, string isp, List<string> tags, bool tor)
    {
        lock (_gate)
        {
            _hits.Add(hit);
            if (_hits.Count > Limit)
                _hits.RemoveRange(0, _hits.Count - Limit);
        }
        Total++;
        if (hit.AdBlock)
            AdBlock++;
        if (hit.Scanner)
            Scanners++;
        else
            Clean++;
        if (tags.Count > 0)
            Networked++;
        if (tor)
            Tor++;
        _countries.AddOrUpdate(country, 1, (_, n) => n + 1);
        _isps.AddOrUpdate(isp, 1, (_, n) => n + 1);
    }
}

/// <summary>Builds a scored <see cref="Hit"/> from one inbound request.</summary>
public static class Recorder
{
    public const int MaxFingerprintBytes = 8192;
    const int ScannerFloor = 80;

    public static Hit Capture(Runtime runtime, string ip, string path, Query query, string userAgent, string referer)
    {
        var info = Geo.Lookup(ip);
        var (os, browser) = Classify.Agent(userAgent);
        var (tags, _, score) = Classify.Network(info.Isp, info.Organization, info.AutonomousSystem);

        if (info.Tor)
        {
            tags.Add("TOR");
            score = Math.Min(score + 60, 100);
        }
        if (info.Proxy)
        {
            tags.Add("PX");
            score = Math.Min(score + 45, 100);
        }
        if (info.Hosting)
        {
            tags.Add("HOST");
            score = Math.Min(score + 25, 100);
        }

        var scanner = Classify.IsScanner(userAgent, path, out var signature);
        var fingerprint = ParseFingerprint(query);

        var adMode = query.Value("ab").ToLowerInvariant();
        var adBlock = adMode == "1";
        var webdriver = query.Value("wd") == "1" || FingerprintFlag(fingerprint, "wd");

        if (scanner)
            score = Math.Max(score, ScannerFloor);
        if (adBlock)
            score = Math.Min(score + 10, 100);
        else if (adMode == "nojs")
            score = Math.Min(score + 25, 100);
        if (webdriver)
            score = Math.Min(score + 40, 100);
        if (FingerprintText(fingerprint, "webrtc").Length > 0)
            score = Math.Min(score + 15, 100);
        if (FingerprintNumber(fingerprint, "cams") > 0)
            score = Math.Min(score + 5, 100);

        var hit = new Hit
        {
            Ip = ip,
            Location = info.ShortLocation,
            Flag = Geo.FlagEmoji(info.CountryCode),
            CountryCode = info.CountryCode,
            Isp = info.Isp,
            AutonomousSystem = info.AutonomousSystem,
            UserAgent = userAgent,
            Os = os,
            Browser = browser,
            Referer = referer,
            Path = path,
            Timestamp = DateTime.UtcNow.ToString("HH:mm:ss"),
            Tags = tags.Count > 0 ? string.Join(' ', tags) : "OK",
            Signature = signature,
            Risk = Math.Clamp(score, 0, 100),
            AdBlock = adBlock,
            Scanner = scanner,
            Fingerprint = fingerprint,
        };

        runtime.Hits.Add(hit, info.CountryCode, Shorten(info.Isp, 20), tags, info.Tor);
        // Fire and forget: the visitor must not wait on the webhook round-trip.
        _ = Notify.PostAsync(runtime.Webhook, hit, info);
        return hit;
    }

    static string Shorten(string text, int width) => text.Length > width ? text[..width] : text;

    static JsonElement ParseFingerprint(Query query)
    {
        var raw = query.Value("fp");
        if (raw.Length == 0 || raw.Length > MaxFingerprintBytes)
            return Hit.NoFingerprint;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Hit.NoFingerprint;
        }
    }

    static string FingerprintText(JsonElement fingerprint, string key) =>
        fingerprint.ValueKind == JsonValueKind.Object
        && fingerprint.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    static bool FingerprintFlag(JsonElement fingerprint, string key) =>
        fingerprint.ValueKind == JsonValueKind.Object
        && fingerprint.TryGetProperty(key, out var value)
        && (value.ValueKind == JsonValueKind.True
            || (value.ValueKind == JsonValueKind.Number && value.GetDouble() != 0));

    static double FingerprintNumber(JsonElement fingerprint, string key) =>
        fingerprint.ValueKind == JsonValueKind.Object
        && fingerprint.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : 0;
}