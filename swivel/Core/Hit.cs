using System.Text.Json;

namespace Swivel.Core;

/// <summary>One recorded visit: identity, request context, score and fingerprint.</summary>
public sealed record Hit
{
    public required string Ip { get; init; }
    public required string Location { get; init; }
    public required string Flag { get; init; }
    public required string CountryCode { get; init; }
    public required string Isp { get; init; }
    public required string AutonomousSystem { get; init; }
    public required string UserAgent { get; init; }
    public required string Os { get; init; }
    public required string Browser { get; init; }
    public required string Referer { get; init; }
    public required string Path { get; init; }
    public required string Timestamp { get; init; }
    public required string Tags { get; init; }
    public required string Signature { get; init; }
    public int Risk { get; init; }
    public bool AdBlock { get; init; }
    public bool Scanner { get; init; }
    public JsonElement Fingerprint { get; init; }

    public static readonly JsonElement NoFingerprint = default;
}