using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Swivel.Core;
using Swivel.Ui;
using Swivel.Ui.Screens;

namespace Swivel.Tests;

/// <summary>Headless checks for the ported behaviour. Run: dotnet run --project tests</summary>
public static class Checks
{
    static int _passed;
    static readonly List<string> Failures = [];

    public static async Task<int> Main()
    {
        Query();
        PayloadRows();
        ClassifyAgent();
        ClassifyNetwork();
        ClassifyScanner();
        RequestParsing();
        SplitTarget();
        ThemePalettes();
        SimDeterministic();
        HitStoreCounters();
        FingerprintScoring();
        await HttpBehaviour();
        await ListenerEndToEnd();
        ScreenRendering();

        Console.WriteLine();
        if (Failures.Count == 0)
        {
            Console.WriteLine($"all {_passed} checks passed");
            return 0;
        }
        Console.WriteLine($"{_passed} passed, {Failures.Count} FAILED:");
        foreach (var failure in Failures)
            Console.WriteLine("  - " + failure);
        return 1;
    }

    static void Query()
    {
        var parsed = Query.Parse("a=1&b=hello%20world&c&a=2");
        Ok("query: plain value", parsed.Value("a") == "1");
        Ok("query: repeated key takes last", parsed.Value("a") == "2");
        Ok("query: percent decoding", parsed.Value("b") == "hello world");
        Ok("query: valueless key", parsed.Value("c") == "");
        Ok("query: empty input", Query.Parse("").Count == 0);
    }

    static void PayloadRows()
    {
        const string fp = """
            {"cv":"data:image/png;base64,iVBOR","gpu":"ANGLE (NVIDIA)","cams":2,"wd":1}
            """;
        var rows = Payload.Rows(JsonDocument.Parse(fp).RootElement)
            .ToDictionary(r => r.Label, r => r.Value);
        Ok("payload: canvas row", rows.ContainsKey("canvas"));
        Ok("payload: gpu row", rows.ContainsKey("webgl gpu"));
        Ok("payload: camera count", rows.GetValueOrDefault("cams") == "2");
        Ok("payload: webdriver flag", rows.GetValueOrDefault("webdriver") == "yes");
        Ok("payload: empty object yields no rows",
            !Payload.Rows(JsonDocument.Parse("{}").RootElement).Any());
        Ok("payload: page is html and self-posts",
            Payload.Page.Contains("<canvas", StringComparison.Ordinal)
            && Payload.Page.Contains("fetch('/r", StringComparison.Ordinal));
    }

    static void ClassifyAgent()
    {
        var (os, browser) = Classify.Agent(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
        Ok("agent: windows", os.Contains("Windows", StringComparison.Ordinal));
        Ok("agent: chrome", browser.Contains("Chrome", StringComparison.Ordinal));
        var empty = Classify.Agent("");
        Ok("agent: empty falls back", empty.Os == "?" && empty.Browser == "?");
    }

    static void ClassifyNetwork()
    {
        var (tags, _, score) = Classify.Network("NordVPN", "NordVPN LLC", "AS212238");
        Ok("network: vpn tagged", tags.Contains("VPN"));
        Ok("network: vpn scored", score >= 40);
        var (dcTags, _, dcScore) = Classify.Network("Amazon.com, Inc.", "Amazon AWS", "AS16509");
        Ok("network: datacenter tagged", dcTags.Contains("DC"));
        Ok("network: datacenter scored", dcScore >= 30);
        var (none, _, noneScore) = Classify.Network("Comcast Cable", "Comcast", "AS7922");
        Ok("network: isp untagged", none.Count == 0);
        Ok("network: isp score low", noneScore < 25);
    }

    static void ClassifyScanner()
    {
        Ok("scanner: curl detected", Classify.IsScanner("curl/8.4.0", "/r", out _));
        Ok("scanner: sensitive path", Classify.IsScanner("Mozilla/5.0", "/.env", out var sig)
            && sig.StartsWith("path:", StringComparison.Ordinal));
        Ok("scanner: browser not flagged", !Classify.IsScanner(
            "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/120.0 Safari/537.36", "/r", out _));
    }

    static void RequestParsing()
    {
        const string head = "GET /r?fp=1 HTTP/1.1\r\nHost: x\r\n"
            + "cf-connecting-ip: 203.0.113.9\r\nuser-agent: probe\r\n\r\n";
        var request = Request.Parse(head, "10.0.0.1");
        Ok("request: method", request?.Method == "GET");
        Ok("request: target", request?.Target == "/r?fp=1");
        Ok("request: header lookup", request?.Header("USER-AGENT") == "probe");
        Ok("request: cloudflare ip wins", request?.ClientIp() == "203.0.113.9");
        Ok("request: garbage rejected", Request.Parse("", "1.1.1.1") is null);
        Ok("request: no target rejected", Request.Parse("GET", "1.1.1.1") is null);

        const string forwarded = "GET / HTTP/1.1\r\nx-forwarded-for: 198.51.100.7, 10.0.0.1\r\n\r\n";
        Ok("request: forwarded first hop", Request.Parse(forwarded, "10.0.0.1")?.ClientIp() == "198.51.100.7");
        const string mapped = "GET / HTTP/1.1\r\n\r\n";
        Ok("request: ipv4 mapped stripped",
            Request.Parse(mapped, "::ffff:127.0.0.1")?.ClientIp() == "127.0.0.1");
        Ok("request: http/1.0 closes",
            Request.Parse("GET / HTTP/1.0\r\n\r\n", "1.1.1.1")?.ConnectionClose == true);
    }

    static void SplitTarget()
    {
        var (path, query) = HttpService.SplitTarget("/r?a=1&b=2");
        Ok("route: path split", path == "/r");
        Ok("route: query split", query == "a=1&b=2");
        Ok("route: bare path", HttpService.SplitTarget("/x").Path == "/x");
        Ok("route: empty target", HttpService.SplitTarget("").Path == "/");
    }

    static void ThemePalettes()
    {
        foreach (var name in Theme.Palettes.Keys)
        {
            Theme.SetTheme(name);
            Ok($"theme: {name} has five shades", Theme.Shades.Length == 5);
            Ok($"theme: {name} brand set", Theme.Brand.Length > 0);
            Ok($"theme: {name} gradient has no escapes",
                !Theme.Gradient("abcdef", Theme.Brand, 0.25).Contains('\u001b'));
        }
        Theme.SetTheme("BLUE");
        Theme.SetBrightness(100);
        Ok("theme: brightness clamps low", SetBrightness(5) == 30);
        Ok("theme: brightness clamps high", SetBrightness(999) == 150);
        Theme.SetBrightness(100);
        return;

        static int SetBrightness(int value)
        {
            Theme.SetBrightness(value);
            return (int)Math.Round(Theme.Brightness * 100);
        }
    }

    static void SimDeterministic()
    {
        var a = new Sim(1234);
        var b = new Sim(1234);
        for (var i = 0; i < 30; i++)
        {
            a.Step();
            b.Step();
        }
        Ok("sim: seeded series match", a.Cpu.SequenceEqual(b.Cpu));
        Ok("sim: window size", a.Cpu.Count == Sim.Window);
        Ok("sim: values in range", a.Cpu.All(v => v is >= 0 and <= 100));
    }

    static void HitStoreCounters()
    {
        var store = new HitStore();
        Add(store, adBlock: true, scanner: false);
        Add(store, adBlock: false, scanner: true);
        Add(store, adBlock: false, scanner: false, tags: ["VPN"]);
        Ok("store: total counted", store.Total == 3);
        Ok("store: adblock counted independently", store.AdBlock == 1);
        Ok("store: scanner counted independently", store.Scanners == 1);
        Ok("store: clean excludes scanners", store.Clean == 2);
        Ok("store: networked counted", store.Networked == 1);
        Ok("store: tor counted", store.Tor == 0);
        Ok("store: countries ranked", store.TopCountries["DE"] == 1);
        Ok("store: isps ranked", store.TopIsps["isp"] == 2);
        Add(store, adBlock: false, scanner: false, tor: true);
        Ok("store: tor counted on flag", store.Tor == 1);

        static void Add(HitStore store, bool adBlock, bool scanner,
            List<string>? tags = null, bool tor = false)
        {
            var hit = NewHit(adBlock, scanner);
            store.Add(hit, "DE", "isp", tags ?? [], tor);
        }
    }

    static Hit NewHit(bool adBlock, bool scanner) => new()
    {
        Ip = "203.0.113.5",
        Location = "Berlin, DE",
        Flag = "🇩🇪",
        CountryCode = "DE",
        Isp = "isp",
        AutonomousSystem = "AS64496",
        UserAgent = "probe",
        Os = "Windows",
        Browser = "Chrome",
        Referer = "",
        Path = "/r",
        Timestamp = "00:00:00",
        Tags = "OK",
        Signature = "",
        Risk = 10,
        AdBlock = adBlock,
        Scanner = scanner,
        Fingerprint = Hit.NoFingerprint,
    };

    static void FingerprintScoring()
    {
        var clean = Capture("Mozilla/5.0 (Windows NT 10.0) Chrome/120.0 Safari/537.36", "");
        Ok("risk: clean visitor low", clean.Risk < 25);
        Ok("risk: clean visitor untagged", clean.Tags == "OK");

        var blocked = Capture("Mozilla/5.0 Chrome/120.0 Safari/537.36", "ab=1");
        Ok("risk: adblock detected", blocked.AdBlock);
        Ok("risk: adblock raises score", blocked.Risk > clean.Risk);

        var bot = Capture("curl/8.4.0", "");
        Ok("risk: scanner detected", bot.Scanner);
        Ok("risk: scanner floor applied", bot.Risk >= 80);
        Ok("risk: scanner not counted clean", !bot.Scanner == false);

        var webdriver = Capture("Mozilla/5.0 Chrome/120.0 Safari/537.36", "wd=1");
        Ok("risk: webdriver raises score", webdriver.Risk > clean.Risk);

        var cam = Capture("Mozilla/5.0 Chrome/120.0 Safari/537.36", "fp=" + Uri.EscapeDataString("{\"cams\":3}"));
        Ok("risk: camera payload parsed", cam.Fingerprint.ValueKind == JsonValueKind.Object);
        Ok("risk: camera raises score", cam.Risk > clean.Risk);

        var junk = Capture("Mozilla/5.0 Chrome/120.0 Safari/537.36", "fp=not-json");
        Ok("risk: bad fingerprint ignored", junk.Fingerprint.ValueKind == JsonValueKind.Undefined);
        Ok("risk: oversized fingerprint ignored", Capture("curl/8", "fp=" + new string('x', 9000))
            .Fingerprint.ValueKind == JsonValueKind.Undefined);

        static Hit Capture(string agent, string query)
        {
            var runtime = new Runtime();
            return Recorder.Capture(runtime, "203.0.113.5", "/r",
                Query.Parse(query), agent, "");
        }
    }

    static async Task HttpBehaviour()
    {
        using var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
        })
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        var port = FreePort();
        var runtime = new Runtime { Port = port, Target = "http://localhost:8080/dest" };
        new HttpService(runtime).Start();
        try
        {
            var baseUrl = $"http://127.0.0.1:{port}";

            var root = await client.GetAsync(baseUrl + "/");
            var body = await root.Content.ReadAsStringAsync();
            Ok("http: root serves payload", (int)root.StatusCode == 200
                && body.Length == Payload.Page.Length);
            Ok("http: root content type html",
                root.Content.Headers.ContentType?.MediaType == "text/html");

            using (var head = new HttpRequestMessage(HttpMethod.Head, baseUrl + "/"))
            {
                var response = await client.SendAsync(head);
                Ok("http: HEAD redirects", (int)response.StatusCode == 302);
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/r"))
            {
                request.Content = new StringContent("x");
                var response = await client.SendAsync(request);
                Ok("http: POST rejected", (int)response.StatusCode == 405);
            }

            var icon = await client.GetAsync(baseUrl + "/favicon.ico");
            Ok("http: favicon empty", (int)icon.StatusCode == 204);

            var hit = await client.GetAsync(baseUrl + "/r?ab=1&fp=%7B%22gpu%22%3A%22x%22%7D");
            Ok("http: capture redirects to target",
                hit.StatusCode == HttpStatusCode.Found
                && hit.Headers.Location?.ToString() == "http://localhost:8080/dest");

            await Task.Delay(400);
            Ok("http: hit recorded", runtime.Hits.Total == 1);
            var stored = runtime.Hits.Recent(10);
            Ok("http: adblock flag stored", stored[0].AdBlock);
            Ok("http: fingerprint stored", stored[0].Fingerprint.ValueKind == JsonValueKind.Object);
            Ok("http: client ip recorded", stored[0].Ip == "127.0.0.1");
            Ok("http: request path stored", stored[0].Path == "/r");

            var kept = await client.GetAsync(baseUrl + "/r?ab=1");
            Ok("http: keep-alive serves a second request", (int)kept.StatusCode == 302);
        }
        finally
        {
            new HttpService(runtime).Stop();
        }

        var closed = new Runtime { Port = port };
        await AssertThrows<IOException>(() => new HttpService(closed).Start(),
            "http: duplicate bind reports cleanly");
        void AssertThrows<T>(Action action, string name) where T : Exception
        {
            try
            {
                action();
                Ok(name, false);
            }
            catch (T)
            {
                Ok(name, true);
            }
        }
    }

    static async Task ListenerEndToEnd()
    {
        var port = FreePort();
        var runtime = new Runtime { Port = port };
        var service = new HttpService(runtime);
        service.Start();
        try
        {
            var hit = Classify.IsScanner("curl/8.4.0", "/.env", out _);
            using var raw = new TcpClient();
            await raw.ConnectAsync(IPAddress.Loopback, port);
            var stream = raw.GetStream();
            var head = Encoding.ASCII.GetBytes(
                "GET /.env HTTP/1.1\r\nHost: localhost\r\nconnection: close\r\n\r\n");
            await stream.WriteAsync(head);
            await stream.FlushAsync();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            var response = await reader.ReadToEndAsync();
            Ok("http: scanner probe logged", hit);
            Ok("http: raw request answered", response.StartsWith("HTTP/1.1 302", StringComparison.Ordinal));
            Ok("http: close honoured", response.Contains("Connection: close", StringComparison.Ordinal));
        }
        finally
        {
            service.Stop();
        }
    }

    static void ScreenRendering()
    {
        var runtime = new Runtime { Port = 5099, Target = "http://localhost:8080/" };
        runtime.Hits.Add(NewHit(adBlock: true, scanner: false), "DE", "Example ISP", ["VPN"], tor: true);
        var app = new App(runtime, "https://discord.test/webhook", "http://localhost:8080/");

        for (var tab = 0; tab < app.TabNames.Length; tab++)
        {
            app.Tab = tab;
            var ctx = Context.Create(runtime, 12.5);
            List<string> lines;
            try
            {
                lines = Views.All[tab].Render(app, ctx);
            }
            catch (Exception e)
            {
                Ok($"screen: {app.TabNames[tab]} renders", false);
                Console.WriteLine("    " + e);
                continue;
            }
            Ok($"screen: {app.TabNames[tab]} renders", lines.Count > 0);
            Ok($"screen: {app.TabNames[tab]} fits width",
                lines.Where(l => Theme.VisibleLength(l) <= ctx.Cols + 1).Count() >= lines.Count / 2);
        }

        app.Tab = 0;
        var setup = Context.Create(runtime, 0);
        Ok("screen: setup shows webhook value",
            string.Join('\n', Views.All[0].Render(app, setup)).Contains("discord.test", StringComparison.Ordinal));

        app.Tab = 4;
        Ok("input: settings adjust clamps", AdjustBrightness(app, 99));
        Ok("input: setup start toggles listener state", ToggleStart(app));
        app.Tab = 0;
        Ok("input: quit key", app.Handle("q") == false);
        Ok("input: tab cycles", app.Handle("\t") && app.Tab == 1);
        Ok("input: digits jump", app.Handle("4") && app.Tab == 3);
        Ok("input: clear resets store", app.Handle("c") && runtime.Hits.Total == 0);

        static bool AdjustBrightness(App app, int steps)
        {
            app.Selected = 4;
            var low = app.Get("Brightness").Number;
            for (var i = 0; i < steps; i++)
                app.Adjust(-1);
            return app.Get("Brightness").Number < low;
        }

        static bool ToggleStart(App app)
        {
            app.Form.Selected = 2;
            var before = app.Runtime.Started;
            app.Handle("\r");
            return app.Runtime.Started != before;
        }
    }

    static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    static void Ok(string name, bool condition)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine("  ok  " + name);
        }
        else
        {
            Failures.Add(name);
            Console.WriteLine("FAIL  " + name);
        }
    }
}