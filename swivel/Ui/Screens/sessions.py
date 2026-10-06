"""Sessions screen: manage saved traffic sessions."""

from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr, pad
from ...core import sessions


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def sessions_screen(app, ctx):
    """Saved sessions browser."""
    out = heading(ctx, " SESSIONS ")
    
    saved = sessions.list_sessions()
    
    rows = [""]
    if not saved:
        rows.append(ctx.m + fg(*T.GREY) + "no saved sessions" + R)
    else:
        for i, sv in enumerate(saved):
            selected = i == app.sel if hasattr(app, 'sel') else False
            marker = W.marker(selected)
            label_text = W.label(sv['file'][:30], selected)
            
            info = fg(*T.GREY) + f"  {sv['hit_count']} hits  " + R
            if sv.get('timestamp'):
                info += fg(*T.GREY) + sv['timestamp'][:19].replace('T', ' ') + R
            
            line = lr(f" {marker} {label_text}", info, ctx.inn)
            if selected:
                line = W.highlight(line, ctx.inn, T.VI)
            rows.append(ctx.m + line)
    
    rows += ["", fg(*T.GREY) + "w/s select   enter load   d delete   r refresh" + R]
    out += [ctx.m + x for x in W.panel("SAVED SESSIONS", rows, ctx.cw, T.PK)]
    return out