"""Settings screen: theme, animation and rendering options."""

from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


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