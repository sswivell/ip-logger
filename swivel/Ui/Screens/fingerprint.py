"""Fingerprint screen: browser fingerprint detail for recent hits."""

from ...core import server, payload
from ...ui import theme as T
from ...ui.theme import B, R, fg, gradient, lr, pad


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def fingerprint(app, ctx):
    """Browser fingerprint detail for the three most recent hits."""
    out = heading(ctx, " FINGERPRINTS ")
    with server.LOCK:
        hits = list(server.HITS[-6:])
    if not any(h.get("fp") for h in hits):
        return out + [ctx.m + fg(*T.GREY) + "no fingerprints yet" + R,
                      ctx.m + fg(*T.GREY) + "hit the link first" + R]
    shown = 0
    for h in reversed(hits):
        if not h.get("fp"):
            continue
        head = fg(*T.VI) + h["ts"] + R + "  " + B + fg(*T.WHITE) + h["ip"] + R
        head += "  " + fg(*T.GREY) + h["loc"][:30] + R
        out.append(ctx.m + head)
        out.append(ctx.m + fg(*T.GREY) + "\u2500" * min(ctx.w, 60) + R)
        for k, v in payload.rows(h["fp"])[:14]:
            out.append(ctx.m + "  " + fg(*T.VI) + pad(k[:14], 14) + R
                       + fg(*T.WHITE) + str(v)[:40] + R)
        out.append("")
        shown += 1
        if shown >= 3:
            break
    return out