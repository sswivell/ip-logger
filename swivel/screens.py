"""The six dashboard screens."""

from . import geo, payload, server, theme as T, widgets as W
from .theme import B, R, fg, gradient, lr, mix, pad

RISK_HI = 60
RISK_MID = 25


def risk_colour(score):
    if score >= RISK_HI:
        return T.SHADES[4]
    return T.SHADES[2] if score >= RISK_MID else T.GN


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def setup(app, ctx):
    """Webhook and target entry plus server status."""
    out = heading(ctx, " SETUP ")
    rows = [""]
    for i, name in enumerate(("Webhook", "Target", "START")):
        focused = i == app.form.sel
        if i == 2:
            value = (B + fg(*T.GN) + "[ RUNNING ]" + R) if server.STARTED[0] \
                else (B + fg(*T.AM) + "[ ENTER ]" + R)
        else:
            text = app.form.values[i]
            shown = text if text else fg(*mix(T.GREY, (0, 0, 0), .6)) \
                + "(empty)"
            if focused and app.form.editing:
                shown += "_"
            value = fg(*T.WHITE) + shown + R
        row = W.highlight(lr(" %s %s" % (W.marker(focused),
                                         W.label(name, focused)),
                             value, ctx.inn), ctx.inn, T.VI) if focused \
            else lr(" %s %s" % (W.marker(focused), W.label(name, focused)),
                    value, ctx.inn)
        rows += [row, ""] if i < 2 else [row]
    rows += ["", fg(*T.GREY) + "w/s move   enter edit   esc cancel" + R]
    out += [ctx.m + x for x in W.panel("INPUTS", rows, ctx.cw, T.VI)]
    out.append("")

    status = []
    if server.PUBLIC[0]:
        status.append(fg(*T.GN) + "public " + B + fg(*T.WHITE)
                      + server.PUBLIC[0][:50] + R)
    elif server.STARTED[0]:
        status.append(fg(*T.AM) + "waiting for URL..." + R)
    else:
        status.append(fg(*T.GREY) + "not started" + R)
    status.append(W.kv("port", str(server.PORT[0]), T.GREY)
                  + "  " + W.kv("hits", str(server.STATS["tot"]), T.GREY))
    status.append(W.kv("webhook", (server.WEBHOOK[0] or "none")[:40], T.GREY))
    out += [ctx.m + x for x in W.panel("STATUS", status, ctx.cw, T.GN)]
    return out


def live(app, ctx):
    """Recent hits plus the traffic chart."""
    out = heading(ctx, " LIVE HITS ")
    with server.LOCK:
        hits = list(server.HITS[-8:])
    if not hits:
        out.append(ctx.m + fg(*T.GREY) + "waiting..." + R)
    for h in reversed(hits):
        c = risk_colour(h["risk"])
        line = fg(*mix(c, (0, 0, 0), .5)) + h["ts"] + R + "  "
        line += B + fg(*T.WHITE) + pad(h["ip"][:15], 15) + R + " "
        line += fg(*c) + pad(h["tags"][:10], 10) + R + " "
        line += fg(*T.GREY) + h["loc"][:30] + R
        out.append(ctx.m + line)
    out.append("")

    s = app.sim
    chart = [B + fg(*T.CY) + "hits" + R + "  " + fg(*T.WHITE)
             + str(server.STATS["tot"]) + R]
    chart.append(W.sparkline(s.cpu_hist, ctx.inn) if app.get("Sparklines")
                 else fg(*T.TRK) + "\u2500" * ctx.inn + R)
    chart.append(lr(fg(*T.GREY) + "lat " + fg(*T.WHITE) + "%dms" % int(s.latency),
                    fg(*T.GREY) + "up " + fg(*T.WHITE)
                    + "%ds" % int(ctx.nw - s.t0), ctx.inn))
    out += [ctx.m + x for x in W.panel("LOAD", chart, ctx.cw, T.CY)]
    return out


def stats(app, ctx):
    """Counters, top countries and top ISPs."""
    out = heading(ctx, " STATS ")
    s = server.STATS
    out += [ctx.m + x for x in W.panel("COUNTS", [
        W.stat_row("total", max(1, s["tot"]), T.CY),
        W.stat_row("clean", s["ok"], T.GN),
        W.stat_row("adblock", s["ab"], T.AM),
        W.stat_row("vpn/dc", s["vpn"], T.VI),
        W.stat_row("tor", s["tor"], T.PK),
        W.stat_row("scanner", s["bad"], T.SHADES[4]),
    ], ctx.cw, T.CY)]
    out.append("")

    top = sorted(server.CC.items(), key=lambda x: -x[1])[:8]
    peak = max(1, top[0][1]) if top else 1
    rows = []
    for cc, n in top or [("?", 0)]:
        rows.append(pad(fg(*T.WHITE) + geo.flag(cc) + " " + cc, 4 + len(cc) + 3)
                    + " " + W.bar(n / peak * 100, 12, T.CY)
                    + " " + B + fg(*T.WHITE) + str(n).rjust(4) + R)
    if not top:
        rows = [fg(*T.GREY) + "no data" + R]
    out += [ctx.m + x for x in W.panel("TOP COUNTRIES", rows, ctx.cw, T.GN)]
    out.append("")

    rows = []
    for isp, n in sorted(server.ISP.items(), key=lambda x: -x[1])[:6]:
        rows.append(pad(fg(*T.WHITE) + isp[:22], 22 + len(fg(*T.WHITE)) + len(R))
                    + " " + B + fg(*T.AM) + str(n).rjust(4) + R)
    out += [ctx.m + x for x in W.panel(
        "TOP ISPs", rows or [fg(*T.GREY) + "no data" + R], ctx.cw, T.AM)]
    return out


def fingerprint(app, ctx):
    """Browser fingerprint detail for the three most recent hits."""
    out = heading(ctx, " FINGERPRINTS ")
    with server.LOCK:
        hits = list(server.HITS[-6:])
    if not any(h.get("fp") for h in hits):
        return out + [ctx.m + fg(*T.GREY) + "no fingerprints yet" + R,
                      ctx.m + fg(*T.GREY) + "hit the link first" + R]
    shown = 0
    for h in reversed(hits):
        if not h.get("fp"):
            continue
        head = fg(*T.VI) + h["ts"] + R + "  " + B + fg(*T.WHITE) + h["ip"] + R
        head += "  " + fg(*T.GREY) + h["loc"][:30] + R
        out.append(ctx.m + head)
        out.append(ctx.m + fg(*T.GREY) + "\u2500" * min(ctx.w, 60) + R)
        for k, v in payload.rows(h["fp"])[:14]:
            out.append(ctx.m + "  " + fg(*T.VI) + pad(k[:14], 14) + R
                       + fg(*T.WHITE) + str(v)[:40] + R)
        out.append("")
        shown += 1
        if shown >= 3:
            break
    return out


def settings(app, ctx):
    """Theme, animation and rendering options."""
    out = heading(ctx, " SETTINGS ")
    rows = [""]
    for i, opt in enumerate(app.options):
        focused = i == app.sel
        if opt["type"] == "bool":
            value = (B + fg(*T.GN) + "[====O]" + R + " " + B + fg(*T.GN) + "ON "
                     + R) if opt["value"] else \
                (fg(*T.GREY) + "[O====]" + R + " " + fg(*T.GREY) + "OFF" + R)
        elif opt["type"] == "choice":
            value = (fg(*T.VI) + "<" + R + " " + B + fg(*T.WHITE)
                     + opt["options"][opt["value"]].center(10) + R
                     + " " + fg(*T.VI) + ">" + R)
        else:
            value = W.slider(opt["value"], opt["min"], opt["max"])
        name = W.label(opt["name"], focused)
        line = lr(" %s %s" % (W.marker(focused), name), value, ctx.w)
        rows.append(W.highlight(line, ctx.w, T.VI) if focused else line)
    rows += ["", fg(*T.GREY) + "w/s move   a/d adjust   enter toggle" + R]
    return out + [ctx.m + x for x in W.panel("SETTINGS", rows, ctx.w, T.VI)]


def about(app, ctx):
    """Usage notes and the key map."""
    out = heading(ctx, " ABOUT ")
    info = ["",
            B + fg(*T.WHITE) + "IPLOGGER x SWIVEL" + R, "",
            fg(*T.GREY) + "paste webhook in setup" + R,
            fg(*T.GREY) + "set target url" + R,
            fg(*T.GREY) + "press start" + R,
            fg(*T.GREY) + "hits stream into LIVE" + R,
            fg(*T.GREY) + "see FP for browser prints" + R, "",
            fg(*T.GREY) + "canvas / gpu / audio fp" + R,
            fg(*T.GREY) + "font enum / webgl vendor" + R,
            fg(*T.GREY) + "battery + webrtc local IP" + R,
            fg(*T.GREY) + "cams / mics / speakers" + R, ""]

    binds = [""]
    for k, desc in (("w/s", "move"), ("enter", "edit/activate"),
                    ("esc", "cancel"), ("tab", "next tab"),
                    ("1-6", "jump"), ("r", "reset sim"),
                    ("c", "clear hits"), ("q", "quit")):
        binds.append(fg(*T.VI) + pad(k, 7) + R + "  " + fg(*T.GREY) + desc + R)
    binds.append("")
    left = W.panel("HELP", info, ctx.cw, T.AM)
    right = W.panel("KEYS", binds, ctx.cw, T.GN)
    return out + [ctx.m + a + " " * ctx.gap + b
                  for a, b in zip(left, right)]


SCREENS = (setup, live, stats, fingerprint, settings, about)