using System.Text.RegularExpressions;

namespace Swivel.Core;

/// <summary>VPN/datacenter tagging, scanner detection and User-Agent parsing.</summary>
public static partial class Classify
{
    static readonly string[] VpnKeys =
        ["vpn", "nord", "express", "surfshark", "mullvad", "proton", "ipvanish", "purevpn"];

    static readonly string[] DatacenterKeys =
    [
        "digitalocean", "linode", "vultr", "ovh", "hetzner", "amazon",
        "aws", "azure", "oracle", "m247", "contabo",
    ];

    static readonly string[] ScannerAgents =
    [
        "sqlmap", "nikto", "nmap", "masscan", "nuclei", "wpscan",
        "acunetix", "burp", "zgrab", "gobuster",
    ];

    static readonly (string Tag, string[] Keys, int Penalty)[] Signals =
    [
        ("VPN", VpnKeys, 45),
        ("DC", DatacenterKeys, 25),
    ];

    /// <summary>Tag an address, returning labels plus a 0-100 base score.</summary>
    public static (List<string> Tags, List<string> Reasons, int Score) Network(string isp, string org, string asn)
    {
        var blob = $"{isp} {org} {asn}".ToLowerInvariant();
        var tags = new List<string>();
        var reasons = new List<string>();
        var score = 0;
        foreach (var (tag, keys, penalty) in Signals)
        {
            foreach (var key in keys)
            {
                if (!blob.Contains(key, StringComparison.Ordinal))
                    continue;
                tags.Add(tag);
                reasons.Add($"{tag.ToLowerInvariant()}:{key}");
                score += penalty;
                break;
            }
        }
        return (tags, reasons, Math.Min(score, 100));
    }

    /// <summary>Flag known scanner agents and probes against sensitive paths.</summary>
    public static bool IsScanner(string? userAgent, string path, out string signature)
    {
        var agent = (userAgent ?? "").ToLowerInvariant();
        foreach (var key in ScannerAgents)
        {
            if (agent.Contains(key, StringComparison.Ordinal))
            {
                signature = "ua:" + key;
                return true;
            }
        }
        var probe = path.ToLowerInvariant();
        foreach (Match pattern in SensitivePath().Matches(probe))
        {
            signature = "path:" + pattern.Value;
            return true;
        }
        signature = "";
        return false;
    }

    [GeneratedRegex(@"\.env|\.git|wp-login|phpmyadmin|\.\./|etc/passwd|eval\(|/admin")]
    private static partial Regex SensitivePath();

    static readonly (string Key, string Name)[] OperatingSystems =
    [
        ("android", "Android"), ("iphone", "iOS"), ("ipad", "iOS"),
        ("windows", "Win"), ("mac", "Mac"), ("linux", "Lin"),
    ];

    static readonly (string Key, string Name)[] Browsers =
    [
        ("edg/", "Edge"), ("chrome", "Chrome"), ("firefox", "FF"),
        ("safari", "Safari"), ("curl", "curl"), ("python", "py"),
    ];

    public static (string Os, string Browser) Agent(string? userAgent)
    {
        var agent = (userAgent ?? "").ToLowerInvariant();
        var os = OperatingSystems.FirstOrDefault(p => agent.Contains(p.Key, StringComparison.Ordinal)).Name ?? "?";
        var browser = Browsers.FirstOrDefault(p => agent.Contains(p.Key, StringComparison.Ordinal)).Name ?? "?";
        return (os, browser);
    }
}