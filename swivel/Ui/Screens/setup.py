"""Setup screen: webhook and target entry plus server status."""

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