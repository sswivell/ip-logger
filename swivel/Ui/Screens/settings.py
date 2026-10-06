"""Settings screen: theme, animation and rendering options."""

from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr
from ...core import config


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def theme_preview(ctx, name):
    """Show a preview of a theme's colors."""
    theme_data = T.THEMES.get(name)
    if not theme_data:
        return []
    
    rows = [fg(*T.VI) + B + f" {name} PREVIEW " + R]
    for i, color in enumerate(theme_data):
        block = fg(*color) + "████" + R + " " + fg(*T.GREY) + str(color) + R
        rows.append(ctx.m + "  " + block)
    return rows


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
    
    # Theme previews
    rows += ["", fg(*T.VI) + B + " THEME PREVIEWS " + R, ""]
    for tname in T.THEMES:
        is_current = tname == app.get("Theme")
        prefix = fg(*T.GN) + "► " + R if is_current else "  "
        rows.append(ctx.m + prefix + fg(*T.WHITE if is_current else T.GREY) + tname + R)
        rows.extend(theme_preview(ctx, tname))
        rows.append("")
    
    # Persisted settings
    rows += ["", fg(*T.VI) + B + " PERSISTED " + R, ""]
    cfg = config.load()
    rows.append(ctx.m + fg(*T.GREY) + f"  theme: {cfg.get('theme')}" + R)
    rows.append(ctx.m + fg(*T.GREY) + f"  webhook: {cfg.get('webhook')[:40] if cfg.get('webhook') else 'none'}" + R)
    rows.append(ctx.m + fg(*T.GREY) + f"  target: {cfg.get('target')[:40] if cfg.get('target') else 'none'}" + R)
    
    rows += ["", fg(*T.GREY) + "w/s move   a/d adjust   enter toggle   s save" + R]
    return out + [ctx.m + x for x in W.panel("SETTINGS", rows, ctx.w, T.VI)]