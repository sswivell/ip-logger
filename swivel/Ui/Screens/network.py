"""Network screen: connection monitoring and stats."""

from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr
from ...core import server


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def network(app, ctx):
    """Network monitoring and connection stats."""
    out = heading(ctx, " NETWORK ")
    
    rows = [""]
    
    # Server status
    if server.STARTED[0]:
        rows.append(fg(*T.GN) + B + " ● SERVER RUNNING" + R)
        rows.append(fg(*T.GREY) + f"  Port: {server.PORT[0]}" + R)
        if server.PUBLIC[0]:
            rows.append(fg(*T.GN) + f"  Public: {server.PUBLIC[0][:50]}" + R)
        else:
            rows.append(fg(*T.AM) + "  Public: waiting for tunnel..." + R)
        rows.append(fg(*T.GREY) + f"  Webhook: {server.WEBHOOK[0][:40] if server.WEBHOOK[0] else 'none'}" + R)
        rows.append(fg(*T.GREY) + f"  Target: {server.TARGET[0][:40]}" + R)
    else:
        rows.append(fg(*T.GREY) + B + " ○ SERVER STOPPED" + R)
    
    rows.append("")
    
    # Connection stats
    rows.append(fg(*T.VI) + B + " CONNECTION STATS " + R)
    rows.append("")
    
    with server.LOCK:
        total_hits = server.STATS["tot"]
        unique_ips = len(server.CC)
    
    rows.append(ctx.m + fg(*T.CY) + "  Total Hits:" + R + " " + fg(*T.WHITE) + B + str(total_hits) + R)
    rows.append(ctx.m + fg(*T.CY) + "  Unique IPs:" + R + " " + fg(*T.WHITE) + B + str(unique_ips) + R)
    rows.append(ctx.m + fg(*T.CY) + "  Countries:" + R + " " + fg(*T.WHITE) + B + str(len(server.CC)) + R)
    rows.append(ctx.m + fg(*T.CY) + "  ISPs:" + R + " " + fg(*T.WHITE) + B + str(len(server.ISP)) + R)
    
    rows.append("")
    
    # Top countries by hits
    if server.CC:
        rows.append(fg(*T.VI) + B + " TOP COUNTRIES " + R)
        rows.append("")
        top_cc = sorted(server.CC.items(), key=lambda x: -x[1])[:10]
        for cc, count in top_cc:
            from ...core import geo
            flag_emoji = geo.flag(cc)
            bar = W.bar(count / max(1, top_cc[0][1]) * 100, 20, T.CY)
            rows.append(ctx.m + fg(*T.WHITE) + f"  {flag_emoji} {cc}" + R + "  " + bar + "  " + fg(*T.WHITE) + str(count) + R)
    
    rows.append("")
    rows += ["", fg(*T.GREY) + "r refresh   c clear hits   tab next" + R]
    out += [ctx.m + x for x in W.panel("NETWORK MONITOR", rows, ctx.cw, T.CY)]
    return out