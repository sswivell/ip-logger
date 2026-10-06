"""Alerts screen: security alerts for high-risk hits."""

from ...core import alerts, server
from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def alerts_screen(app, ctx):
    """Security alerts panel."""
    out = heading(ctx, " ALERTS ")
    
    alert_list = alerts.get_alerts()
    
    rows = [""]
    if not alert_list:
        rows.append(ctx.m + fg(*T.GREY) + "no alerts" + R)
    else:
        for a in alert_list[:10]:
            level_color = a["color"]
            level_text = a["level"]
            time_str = a["time"]
            ip_str = a["ip"]
            risk_str = str(a["risk"])
            tags_str = a["tags"]
            loc_str = a["loc"]
            
            row = (fg(*level_color) + B + f"[{level_text}]" + R + "  "
                   + fg(*T.WHITE) + time_str + R + "  "
                   + fg(*T.GREY) + ip_str + R + "  "
                   + fg(*level_color) + f"R:{risk_str}" + R + "  "
                   + fg(*T.GREY) + tags_str + R)
            rows.append(ctx.m + row)
            rows.append(ctx.m + "  " + fg(*T.GREY) + loc_str + R)
            rows.append("")
    
    rows += ["", fg(*T.GREY) + "c clear alerts   e export   space refresh" + R]
    out += [ctx.m + x for x in W.panel("SECURITY ALERTS", rows, ctx.cw, T.SHADES[4])]
    return out