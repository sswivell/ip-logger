"""cloudflared discovery, download and quick-tunnel management."""

import os
import platform
import shutil
import subprocess
import tarfile
import threading
import urllib.request
import zipfile

from .bootstrap import IS_WIN, TM, sh

NAME = "cloudflared.exe" if IS_WIN else "cloudflared"
CACHE = os.path.expanduser("~/.swivel_cloudflared")
BASE = "https://github.com/cloudflare/cloudflared/releases/latest/download/"
ERRORS = []


def note(msg):
    """Collect a message to print once the alt-screen is torn down."""
    ERRORS.append(msg)


def find():
    """Locate a cloudflared binary in PATH, cache, package dir or Termux."""
    p = shutil.which(NAME)
    if p:
        return p
    for c in (CACHE,
              os.path.join(os.path.dirname(os.path.abspath(__file__)),
                           NAME, "..", NAME),
              "/data/data/com.termux/files/usr/bin/" + NAME):
        if os.path.isfile(c):
            return os.path.normpath(c)
    return None


def asset():
    """Map the running platform onto a cloudflared release filename."""
    m, s = platform.machine().lower(), platform.system().lower()
    if s == "windows":
        if m in ("amd64", "x86_64"):
            return "cloudflared-windows-amd64.exe"
        if m in ("arm64", "aarch64"):
            return "cloudflared-windows-arm64.exe"
    elif s == "darwin":
        if m in ("arm64", "aarch64"):
            return "cloudflared-darwin-arm64.tgz"
        return "cloudflared-darwin-amd64.tgz"
    elif s == "linux":
        if m in ("aarch64", "arm64"):
            return "cloudflared-linux-arm64"
        if m.startswith("arm"):
            return "cloudflared-linux-arm"
        if m in ("x86_64", "amd64"):
            return "cloudflared-linux-amd64"
        if m in ("i386", "i686"):
            return "cloudflared-linux-386"
    return None


def _unpack(tmp, pkg):
    """Move a downloaded .tgz/.zip payload into the cache path."""
    home = os.path.dirname(CACHE)
    if pkg.endswith(".tgz"):
        with tarfile.open(tmp, "r:gz") as t:
            t.extractall(home)
        src = os.path.join(home, NAME)
        if os.path.isfile(src):
            shutil.move(src, CACHE)
        os.remove(tmp)
    elif pkg.endswith(".zip"):
        with zipfile.ZipFile(tmp) as z:
            z.extractall(home)
        os.remove(tmp)
    else:
        shutil.move(tmp, CACHE)
    try:
        os.chmod(CACHE, 0o755)
    except OSError:
        pass


def download():
    """Fetch the release build matching this machine into the cache."""
    pkg = asset()
    if not pkg:
        print("[!] unsupported platform for cloudflared")
        return None
    url, tmp = BASE + pkg, CACHE + ".download"
    print("[*] " + url)
    try:
        urllib.request.urlretrieve(url, tmp)
        _unpack(tmp, pkg)
    except Exception as e:
        print("[!] download failed:", e)
        try:
            os.remove(tmp)
        except OSError:
            pass
        return None
    print("[+] cloudflared saved to " + CACHE)
    return CACHE


def ensure():
    """Return a usable cloudflared path, installing one if required."""
    p = find()
    if p:
        return p
    if TM:
        print("[*] trying pkg install cloudflared...")
        if sh(["pkg", "install", "-y", "cloudflared"]):
            return shutil.which(NAME)
    return download()


def start(port, on_url):
    """Spawn a quick tunnel to localhost:port, reporting the URL via on_url."""
    args = [ensure(), "tunnel", "--url", "http://localhost:%d" % port,
            "--no-autoupdate", "--protocol", "http2"]
    if not args[0]:
        return None
    note("cf: " + args[0])
    try:
        proc = subprocess.Popen(
            args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            text=True, bufsize=1,
        )
    except Exception as e:
        note("cf spawn failed: " + str(e))
        return None

    def reader():
        for line in iter(proc.stdout.readline, ""):
            line = line.rstrip()
            if "trycloudflare.com" in line:
                for tok in line.split():
                    if tok.startswith("https://") and \
                            "trycloudflare.com" in tok:
                        on_url(tok.strip())
                        return
            elif "ERR" in line and "ping_group_range" not in line:
                note("cf: " + line)

    threading.Thread(target=reader, daemon=True).start()
    return proc