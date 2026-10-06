using System.Diagnostics;

namespace Swivel.Core;

/// <summary>Shared mutable state: configuration, listener control and hit store.</summary>
public sealed class Runtime
{
    readonly HttpService _http;

    public Runtime() => _http = new HttpService(this);

    public string Target { get; set; } = "http://localhost:8080/";
    public string? Webhook { get; set; }
    public string? PublicUrl { get; set; }
    public bool Started { get; set; }
    public int Port { get; set; } = 5000;
    public Process? TunnelProcess { get; set; }
    public HitStore Hits { get; } = new();

    /// <summary>Diagnostics collected in the background, printed on exit.</summary>
    public List<string> Notes { get; } = [];

    /// <summary>Apply the form values, bind the listener and open the tunnel.</summary>
    public void Start(string webhook, string target)
    {
        if (Started)
        {
            Notes.Add("server: already running");
            return;
        }
        Started = true;
        Webhook = string.IsNullOrWhiteSpace(webhook) ? null : webhook.Trim();
        Target = string.IsNullOrWhiteSpace(target) ? "http://localhost:8080/" : target.Trim();
        try
        {
            _http.Start();
        }
        catch (IOException e)
        {
            Started = false;
            Notes.Add("server: " + e.Message);
            return;
        }
        Notes.Add($"server: listening on 0.0.0.0:{Port}");
        TunnelProcess = Tunnel.Start(Port, url => PublicUrl = url, Notes);
        if (TunnelProcess is null)
            Notes.Add("tunnel failed - local only mode");
    }

    public void Stop()
    {
        Started = false;
        _http.Stop();
        var process = TunnelProcess;
        TunnelProcess = null;
        if (process is null)
            return;
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            process.Dispose();
        }
        catch (Exception e) when (e is InvalidOperationException
                                      or System.ComponentModel.Win32Exception)
        {
            // Already gone; nothing to clean up.
        }
    }
}