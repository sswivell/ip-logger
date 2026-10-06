"""Payload screen: view the fingerprinting payload HTML/JS."""

from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, lr, pad
from ...core import payload


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def payload_screen(app, ctx):
    """View the fingerprinting payload."""
    out = heading(ctx, " PAYLOAD ")
    
    rows = [""]
    rows.append(fg(*T.VI) + B + " LANDING PAGE HTML " + R)
    rows.append(fg(*T.GREY) + "─" * min(ctx.w - 4, 60) + R)
    
    # Show first 500 chars of HTML
    html = payload.PAGE[:500]
    for line in html.split("\n")[:15]:
        rows.append(ctx.m + "  " + fg(*T.GREY) + line[:ctx.inn - 4] + R)
    rows.append(ctx.m + "  " + fg(*T.GREY) + "..." + R)
    rows.append("")
    
    rows.append(fg(*T.VI) + B + " FINGERPRINT FIELDS " + R)
    rows.append(fg(*T.GREY) + "─" * min(ctx.w - 4, 60) + R)
    
    for label, key, fmt in payload.FIELDS[:20]:
        rows.append(ctx.m + "  " + fg(*T.VI) + pad(label[:16], 16) + R
                   + fg(*T.WHITE) + key + R)
    
    rows.append(ctx.m + "  " + fg(*T.GREY) + f"... and {len(payload.FIELDS) - 20} more fields" + R)
    rows.append("")
    
    rows += ["", fg(*T.GREY) + "this is the payload served at / (redirects to /r?ab=nojs after 4s)" + R]
    out += [ctx.m + x for x in W.panel("PAYLOAD DETAILS", rows, ctx.cw, T.VI)]
    return out