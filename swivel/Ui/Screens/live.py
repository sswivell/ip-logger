"""Live screen: recent hits plus the traffic chart."""

from ...core import server
from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr, mix, pad

RISK_HI = 60
RISK_MID = 25


def risk_colour(score):
    if score >= RISK_HI:
        return T.SHADES[4]
    return T.SHADES[2] if score >= RISK_MID else T.GN


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


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