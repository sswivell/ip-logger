"""IP geolocation, Tor exit list and traffic classification."""

import json
import re
import urllib.request

CACHE = {}
TOR = set()

VPN_KEYS = ("vpn", "nord", "express", "surfshark", "mullvad",
            "proton", "ipvanish", "purevpn")
DC_KEYS = ("digitalocean", "linode", "vultr", "ovh", "hetzner", "amazon",
           "aws", "azure", "oracle", "m247", "contabo")
SCAN_UA = ("sqlmap", "nikto", "nmap", "masscan", "nuclei", "wpscan",
           "acunetix", "burp", "zgrab", "gobuster")
SCAN_PATH = (r"\.env", r"\.git", r"wp-login", r"phpmyadmin", r"\.\./",
              r"etc/passwd", r"eval\(", r"/admin")

BLANK = {
    "cc": "?", "cty": "?", "cit": "?", "rgn": "?", "isp": "?", "org": "?",
    "as": "?", "lat": None, "lon": None, "proxy": 0, "hosting": 0,
    "tor": False,
}

FIELDS = "66846719"


def load_tor():
    """Refresh the Tor bulk exit address list (best effort)."""
    global TOR
    url = "https://check.torproject.org/torbulkexitlist"
    try:
        with urllib.request.urlopen(url, timeout=6) as r:
            txt = r.read().decode()
        TOR = {ln.strip() for ln in txt.splitlines()
               if ln.strip() and not ln.startswith("#")}
    except Exception:
        TOR = set()


def geo(ip):
    """Look up an address via ip-api.com, memoised per process."""
    if ip in CACHE:
        return CACHE[ip]
    info = dict(BLANK, tor=ip in TOR)
    try:
        with urllib.request.urlopen(
            "http://ip-api.com/json/%s?fields=%s" % (ip, FIELDS), timeout=4
        ) as r:
            d = json.loads(r.read().decode())
        if d.get("status") == "success":
            for k in ("cc", "cty", "cit", "rgn", "isp", "org", "as",
                      "lat", "lon", "proxy", "hosting"):
                info[k] = d.get(k, BLANK[k])
    except Exception:
        pass
    CACHE[ip] = info
    return info


def flag(cc):
    """Render a 2-letter country code as a regional-indicator flag emoji."""
    if not cc or len(cc) != 2:
        return ""
    try:
        return "".join(chr(0x1F1E6 + ord(c.upper()) - 65) for c in cc)
    except Exception:
        return ""


def classify(isp, org, asn):
    """Tag an address as VPN and/or datacenter, returning tags and score."""
    blob = (isp + " " + org + " " + asn).lower()
    tags, reasons, score = [], [], 0
    for kind, keys, penalty in (("VPN", VPN_KEYS, 45), ("DC", DC_KEYS, 25)):
        for k in keys:
            if k in blob:
                tags.append(kind)
                reasons.append(kind.lower() + ":" + k)
                score += penalty
                break
    return tags, reasons, min(score, 100)


def scanner(ua, path):
    """Flag known scanners and probes against sensitive path patterns."""
    u = (ua or "").lower()
    for k in SCAN_UA:
        if k in u:
            return True, "ua:" + k
    for p in SCAN_PATH:
        if re.search(p, path.lower()):
            return True, "path:" + p
    return False, ""


def parse_ua(ua):
    """Best-effort OS and browser family from a User-Agent string."""
    u = (ua or "").lower()
    os_ = next((n for n, k in (("Android", "android"),
                               ("iOS", "iphone"), ("iOS", "ipad"),
                               ("Win", "windows"), ("Mac", "mac"),
                               ("Lin", "linux")) if k in u), "?")
    br = next((n for n, k in (("Edge", "edg/"), ("Chrome", "chrome"),
                              ("FF", "firefox"), ("Safari", "safari"),
                              ("curl", "curl"), ("py", "python"))
              if k in u), "?")
    return os_, br


def lookup(ip):
    """Convenience: full geo + classification."""
    info = geo(ip)
    tags, reasons, score = classify(info["isp"], info["org"], info["as"])
    return info, tags, reasons, score