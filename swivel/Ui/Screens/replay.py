"""Replay screen: traffic session playback timeline."""

from datetime import datetime
from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr, pad
from ...core import server, sessions


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def replay(app, ctx):
    """Traffic replay timeline visualization."""
    out = heading(ctx, " TRAFFIC REPLAY ")
    
    # Get current session data from live hits
    with server.LOCK:
        hits = list(server.HITS)
    
    replay_sessions = sessions.get_replay_data(hits)
    
    if not replay_sessions:
        replay_sessions = [{
            "time": datetime.now().strftime("%H:%M"),
            "hits": 0,
            "risk": 0,
            "duration": "0m",
            "tags": ""
        }]
    
    rows = [""]
    for i, s in enumerate(replay_sessions):
        bar_width = ctx.inn - 20
        filled = max(1, int((s["hits"] / max(1, max(x["hits"] for x in replay_sessions))) * bar_width)) if s["hits"] > 0 else 0
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
    
    # Show saved sessions list
    saved = sessions.list_sessions()
    if saved:
        rows.append(fg(*T.VI) + B + " SAVED SESSIONS " + R)
        rows.append("")
        for sv in saved[:5]:
            rows.append(ctx.m + fg(*T.GREY) + f"  {sv['file']}  ({sv['hit_count']} hits)" + R)
        rows.append("")
    
    rows += ["", fg(*T.GREY) + "←/→ seek   space play/pause   r restart   s save" + R]
    out += [ctx.m + x for x in W.panel("SESSIONS", rows, ctx.cw, T.PK)]
    return out