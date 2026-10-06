"""Stats screen: counters, top countries and top ISPs."""

from ...core import server, geo
from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr, pad


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


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