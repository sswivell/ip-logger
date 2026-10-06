namespace Swivel.Core;

/// <summary>Query string reader for the landing page's fingerprint hand-off.</summary>
public sealed class Query
{
    readonly Dictionary<string, string> _values;

    Query(Dictionary<string, string> values) => _values = values;

    public static Query Parse(string? query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var split = pair.Split('=', 2);
                var key = Decode(split[0]);
                if (key.Length == 0 || values.ContainsKey(key))
                    continue;
                values[key] = split.Length > 1 ? Decode(split[1]) : "";
            }
        }
        return new Query(values);
    }

    public string Value(string key) => _values.TryGetValue(key, out var value) ? value : "";

    public bool Has(string key) => _values.ContainsKey(key);

    static string Decode(string raw)
    {
        // '+' stays a literal plus: the fingerprint is encoded with
        // encodeURIComponent, which escapes real spaces as %20 and '+' as %2B.
        return Uri.UnescapeDataString(raw);
    }
}