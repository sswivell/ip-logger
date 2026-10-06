using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Swivel.Core;

/// <summary>Locates, downloads and drives the cloudflared quick tunnel.</summary>
public static class Tunnel
{
    const string ReleaseBase = "https://github.com/cloudflare/cloudflared/releases/latest/download/";
    static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".swivel_cloudflared");

    static readonly string ExecutableName =
        OperatingSystem.IsWindows() ? "cloudflared.exe" : "cloudflared";

    public static bool IsAvailable => Find() is not null;

    /// <summary>Print setup status and return the binary path, downloading if needed.</summary>
    public static string? Ensure(List<string>? notes = null)
    {
        notes ??= [];
        var found = Find();
        if (found is not null)
        {
            Console.WriteLine($"[+] cloudflared found: {found}");
            notes.Add("cloudflared: found at " + found);
            return found;
        }
        Console.WriteLine("[*] cloudflared not found, downloading...");
        var downloaded = Download(notes);
        notes.Add(downloaded is null
            ? "cloudflared: unavailable, local only mode"
            : "cloudflared: ready at " + downloaded);
        Console.WriteLine(downloaded is not null
            ? $"[+] cloudflared ready: {downloaded}"
            : "[!] cloudflared unavailable - local only mode");
        return downloaded;
    }

    /// <summary>Search PATH, the cache, the app directory and Termux's prefix.</summary>
    public static string? Find()
    {
        var name = OperatingSystem.IsWindows() ? ExecutableName : "cloudflared";
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "")
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim(), name);
            if (File.Exists(candidate))
                return candidate;
        }
        if (File.Exists(CachePath))
            return CachePath;
        var local = Path.Combine(AppContext.BaseDirectory, name);
        if (File.Exists(local))
            return local;
        var termux = "/data/data/com.termux/files/usr/bin/cloudflared";
        return File.Exists(termux) ? termux : null;
    }

    /// <summary>Release asset matching this OS and CPU architecture.</summary>
    public static string? Asset()
    {
        var arch = RuntimeInformation.OSArchitecture;
        var arm64 = arch == Architecture.Arm64;
        if (OperatingSystem.IsWindows())
            return arm64 ? "cloudflared-windows-arm64.exe" : "cloudflared-windows-amd64.exe";
        if (OperatingSystem.IsMacOS())
            return arm64 ? "cloudflared-darwin-arm64.tgz" : "cloudflared-darwin-amd64.tgz";
        if (OperatingSystem.IsLinux())
        {
            if (arm64)
                return "cloudflared-linux-arm64";
            if (arch == Architecture.Arm)
                return "cloudflared-linux-arm";
            if (arch is Architecture.X86 or Architecture.X64)
                return "cloudflared-linux-amd64";
        }
        return null;
    }

    static string? Download(List<string> notes)
    {
        var asset = Asset();
        if (asset is null)
        {
            notes.Add("cloudflared: unsupported platform");
            Console.WriteLine("[!] unsupported platform for cloudflared");
            return null;
        }
        var url = ReleaseBase + asset;
        var staging = CachePath + ".download";
        Console.WriteLine("[*] " + url);
        try
        {
            Net.DownloadAsync(url, staging, _ => { }).GetAwaiter().GetResult();
            Unpack(staging, asset);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(CachePath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute | UnixFileMode.GroupRead
                    | UnixFileMode.GroupExecute | UnixFileMode.OtherRead
                    | UnixFileMode.OtherExecute);
            }
        }
        catch (Exception e) when (e is HttpRequestException or IOException
                                      or UnauthorizedAccessException or InvalidDataException)
        {
            Console.WriteLine($"[!] cloudflared download failed: {e.Message}");
            notes.Add("cloudflared: " + e.Message);
            TryDelete(staging);
            return null;
        }
        Console.WriteLine("[+] cloudflared saved to " + CachePath);
        return CachePath;
    }

    static void Unpack(string staging, string asset)
    {
        if (asset.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
        {
            var work = Path.Combine(Path.GetTempPath(), "swivel-cf-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(work);
            try
            {
                ExtractTarGz(staging, work);
                foreach (var candidate in Directory.EnumerateFiles(work, "cloudflared*"))
                {
                    File.Move(candidate, CachePath, overwrite: true);
                    break;
                }
            }
            finally
            {
                Directory.Delete(work, recursive: true);
            }
            TryDelete(staging);
            return;
        }
        if (asset.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(staging, Path.GetDirectoryName(CachePath)!);
            TryDelete(staging);
            return;
        }
        File.Move(staging, CachePath, overwrite: true);
    }

    static void ExtractTarGz(string archive, string destination)
    {
        using var file = File.OpenRead(archive);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new TarReader(gzip);
        while (reader.GetNextEntry() is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile))
                continue;
            var target = Path.Combine(destination, Path.GetFileName(entry.Name));
            if (target.Length <= destination.Length)
                continue; // refuse traversal
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { /* leave the temp file behind */ }
        catch (UnauthorizedAccessException) { /* ditto */ }
    }

    /// <summary>Spawn the tunnel and report its public URL once it appears.</summary>
    public static Process? Start(int port, Action<string> onUrl, List<string> notes)
    {
        var binary = Ensure(notes);
        if (binary is null)
            return null;
        var info = new ProcessStartInfo(binary)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in new[]
                 {
                     "tunnel", "--url", $"http://localhost:{port}",
                     "--no-autoupdate", "--protocol", "http2",
                 })
        {
            info.ArgumentList.Add(arg);
        }
        Process process;
        try
        {
            process = Process.Start(info) ?? throw new IOException("cloudflared did not start");
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or IOException)
        {
            notes.Add("cloudflared: " + e.Message);
            return null;
        }
        notes.Add("cloudflared: " + binary);
        _ = Task.Run(() => ReadOutputAsync(process, onUrl, notes));
        return process;
    }

    static async Task ReadOutputAsync(Process process, Action<string> onUrl, List<string> notes)
    {
        try
        {
            while (await process.StandardOutput.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                if (ExtractUrl(line) is { } url)
                {
                    onUrl(url);
                    return;
                }
                if (line.Contains("ERR", StringComparison.Ordinal)
                    && !line.Contains("ping_group_range", StringComparison.Ordinal))
                {
                    notes.Add("cloudflared: " + line);
                }
            }
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException)
        {
            // Tunnel exited; the caller keeps local-only mode.
        }
    }

    internal static string? ExtractUrl(string line)
    {
        foreach (var token in line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith("https://", StringComparison.Ordinal)
                && token.Contains("trycloudflare.com", StringComparison.Ordinal))
            {
                return token.Trim();
            }
        }
        return null;
    }
}