"""Replay screen: traffic session playback timeline."""

from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr, pad


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def replay(app, ctx):
    """Traffic replay timeline visualization."""
    out = heading(ctx, " TRAFFIC REPLAY ")
    
    # Mock recorded session data - in real app this would come from saved sessions
    sessions = [
        {"time": "23:41", "hits": 3, "risk": 15, "duration": "6m"},
        {"time": "23:47", "hits": 1, "risk": 65, "duration": "1m"},
        {"time": "23:52", "hits": 5, "risk": 8, "duration": "4m"},
        {"time": "00:01", "hits": 2, "risk": 42, "duration": "2m"},
        {"time": "00:08", "hits": 0, "risk": 0, "duration": "0m"},
    ]
    
    rows = [""]
    for i, s in enumerate(sessions):
        # Timeline bar
        bar_width = ctx.inn - 20
        filled = max(1, int((s["hits"] / 5) * bar_width)) if s["hits"] > 0 else 0
        risk_color = T.SHADES[4] if s["risk"] >= 60 else T.SHADES[2] if s["risk"] >= 25 else T.GN
        
        timeline = fg(*T.GREY) + "━" * max(0, bar_width - filled) + R
        if filled > 0:
            timeline = fg(*risk_color) + "━" * (filled - 1) + "●" + fg(*T.GREY) + "━" * max(0, bar_width - filled) + R
        
        marker_pos = " " * 12 + " " * filled + fg(*T.AM) + "↑" + R
        
        row = (fg(*T.WHITE) + B + s["time"] + R + "  " + timeline + "  "
               + fg(*risk_color) + f"R:{s['risk']:3d}" + R + "  "
               + fg(*T.GREY) + s["duration"] + R)
        
        rows.append(ctx.m + row)
        if s["hits"] > 0:
            rows.append(ctx.m + marker_pos + fg(*T.GREY) + f"  Visitor #{i+1}" + R)
        rows.append("")
    
    rows += ["", fg(*T.GREY) + "←/→ seek   space play/pause   r restart   s save" + R]
    out += [ctx.m + x for x in W.panel("SESSIONS", rows, ctx.cw, T.PK)]
    return out