using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Swivel.Core;

/// <summary>Minimal HTTP/1.1 listener: serves the payload and records visits.</summary>
public sealed class HttpService(Runtime runtime)
{
    const int MaxHeadBytes = 16384;
    const int IdleTimeoutMs = 20000;
    const int MaxRequestsPerConnection = 64;

    TcpListener? _listener;
    volatile bool _running;

    public void Start()
    {
        if (_running)
            return;
        var listener = new TcpListener(IPAddress.Any, runtime.Port);
        try
        {
            listener.Start();
        }
        catch (SocketException e)
        {
            throw new IOException($"cannot bind port {runtime.Port}: {e.SocketErrorCode}", e);
        }
        _listener = listener;
        _running = true;
        _ = Task.Run(AcceptLoopAsync);
    }

    public void Stop()
    {
        _running = false;
        try { _listener?.Stop(); }
        catch (SocketException) { /* already closed */ }
        _listener = null;
    }

    async Task AcceptLoopAsync()
    {
        while (_running && _listener is not null)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (e is SocketException or ObjectDisposedException)
            {
                return;
            }
            _ = Task.Run(() => ServeAsync(client));
        }
    }

    async Task ServeAsync(TcpClient client)
    {
        using (client)
        {
            client.NoDelay = true;
            client.ReceiveTimeout = IdleTimeoutMs;
            client.SendTimeout = IdleTimeoutMs;
            var remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "";
            try
            {
                var stream = client.GetStream();
                for (var served = 0; served < MaxRequestsPerConnection; served++)
                {
                    var head = await ReadHeadAsync(stream).ConfigureAwait(false);
                    if (head is null)
                        return;
                    var request = Request.Parse(head, remote);
                    if (request is null)
                    {
                        await WriteAsync(stream, new Response(400, "Bad Request",
                            "text/plain", Encoding.UTF8.GetBytes("bad request")),
                            headOnly: true, keepAlive: false).ConfigureAwait(false);
                        return;
                    }
                    var keepAlive = !request.ConnectionClose;
                    var response = Route(request);
                    await WriteAsync(stream, response, request.Method == "HEAD", keepAlive)
                        .ConfigureAwait(false);
                    if (!keepAlive)
                        return;
                }
            }
            catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
            {
                // Client vanished, timed out, or the socket was torn down.
            }
        }
    }

    Response Route(Request request)
    {
        if (request.Method == "HEAD")
            return Response.Redirect(runtime.Target);

        if (request.Method != "GET")
            return new Response(405, "Method Not Allowed", "text/plain",
                Encoding.UTF8.GetBytes("method not allowed"));

        var (path, query) = SplitTarget(request.Target);
        return path switch
        {
            "/favicon.ico" => new Response(204, "No Content", null, null),
            "/" => new Response(200, "OK", "text/html; charset=utf-8",
                Encoding.UTF8.GetBytes(Payload.Page)),
            _ => Capture(request, path, query),
        };
    }

    Response Capture(Request request, string path, string query)
    {
        try
        {
            Recorder.Capture(runtime, request.ClientIp(), path, Query.Parse(query),
                request.Header("user-agent"), request.Header("referer"));
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // A failed lookup must never cost the visitor their redirect.
        }
        return Response.Redirect(runtime.Target);
    }

    internal static (string Path, string Query) SplitTarget(string target)
    {
        var cut = target.IndexOf('?');
        var path = cut >= 0 ? target[..cut] : target;
        return (path.Length == 0 ? "/" : path, cut >= 0 ? target[(cut + 1)..] : "");
    }

    static async Task<string?> ReadHeadAsync(NetworkStream stream)
    {
        var buffer = new byte[MaxHeadBytes];
        var length = 0;
        while (length < buffer.Length)
        {
            int read;
            try
            {
                read = await stream
                    .ReadAsync(buffer.AsMemory(length, buffer.Length - length))
                    .ConfigureAwait(false);
            }
            catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
            {
                return null;
            }
            if (read <= 0)
                return null;
            length += read;
            var head = Encoding.ASCII.GetString(buffer, 0, length);
            var end = head.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (end >= 0)
                return head[..end];
        }
        return null;
    }

    async Task WriteAsync(NetworkStream stream, Response response, bool headOnly, bool keepAlive)
    {
        var body = response.Body ?? [];
        var head = new StringBuilder();
        head.Append("HTTP/1.1 ").Append(response.Status).Append(' ')
            .Append(response.Reason).Append("\r\n");
        if (response.ContentType is { } type)
            head.Append("Content-Type: ").Append(type).Append("\r\n");
        if (response.Location is { } location)
            head.Append("Location: ").Append(location).Append("\r\n");
        head.Append("Cache-Control: no-store\r\n");
        head.Append("Content-Length: ").Append(body.Length).Append("\r\n");
        head.Append("Connection: ").Append(keepAlive ? "keep-alive" : "close").Append("\r\n\r\n");

        await stream.WriteAsync(Encoding.ASCII.GetBytes(head.ToString())).ConfigureAwait(false);
        if (body.Length > 0 && !headOnly)
            await stream.WriteAsync(body).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }
}

/// <summary>Immutable response template produced by <see cref="HttpService.Route"/>.</summary>
public sealed record Response(int Status, string Reason, string? ContentType, byte[]? Body, string? Location = null)
{
    public static Response Redirect(string target) => new(302, "Found", null, null, target);
}

/// <summary>One parsed HTTP request head.</summary>
public sealed class Request
{
    public string Method { get; private init; } = "";
    public string Target { get; private init; } = "";
    public string RemoteIp { get; private init; } = "";
    public bool ConnectionClose { get; private set; }

    readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public string Header(string name) => _headers.TryGetValue(name, out var value) ? value : "";

    /// <summary>Visitor address, preferring the headers a proxy tunnel sets.</summary>
    public string ClientIp()
    {
        var candidate = Header("cf-connecting-ip");
        if (candidate.Length == 0)
        {
            var forwarded = Header("x-forwarded-for");
            candidate = forwarded.Length == 0 ? RemoteIp : forwarded.Split(',')[0];
        }
        candidate = candidate.Trim();
        const string Mapped = "::ffff:";
        return candidate.StartsWith(Mapped, StringComparison.OrdinalIgnoreCase)
            ? candidate[Mapped.Length..]
            : candidate;
    }

    public static Request? Parse(string head, string remoteIp)
    {
        var lines = head.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return null;
        var parts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        var request = new Request
        {
            Method = parts[0],
            Target = parts[1],
            RemoteIp = remoteIp,
        };
        for (var i = 1; i < lines.Length; i++)
        {
            var colon = lines[i].IndexOf(':');
            if (colon <= 0)
                continue;
            request._headers[lines[i][..colon].Trim()] = lines[i][(colon + 1)..].Trim();
        }
        if (parts.Length >= 3 && parts[2].Equals("HTTP/1.0", StringComparison.OrdinalIgnoreCase))
            request.ConnectionClose = true;
        if (request.Header("connection").Contains("close", StringComparison.OrdinalIgnoreCase))
            request.ConnectionClose = true;
        return request;
    }
}