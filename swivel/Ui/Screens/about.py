"""About screen: usage notes and key map."""

from ...ui import theme as T, widgets as W
from ...ui.theme import B, R, fg, gradient, pad


def heading(ctx, text):
    return [ctx.m + gradient(text, T.BRAND, (ctx.nw * .15) % 1.0), ""]


def about(app, ctx):
    """Usage notes and the key map."""
    out = heading(ctx, " ABOUT ")
    info = ["",
            B + fg(*T.WHITE) + "IPLOGGER x SWIVEL" + R, "",
            fg(*T.GREY) + "paste webhook in setup" + R,
            fg(*T.GREY) + "set target url" + R,
            fg(*T.GREY) + "press start" + R,
            fg(*T.GREY) + "hits stream into LIVE" + R,
            fg(*T.GREY) + "see FP for browser prints" + R, "",
            fg(*T.GREY) + "canvas / gpu / audio fp" + R,
            fg(*T.GREY) + "font enum / webgl vendor" + R,
            fg(*T.GREY) + "battery + webrtc local IP" + R,
            fg(*T.GREY) + "cams / mics / speakers" + R, ""]

    binds = [""]
    for k, desc in (("w/s", "move"), ("enter", "edit/activate"),
                    ("esc", "cancel"), ("tab", "next tab"),
                    ("1-6", "jump"), ("r", "reset sim"),
                    ("c", "clear hits"), ("q", "quit")):
        binds.append(fg(*T.VI) + pad(k, 7) + R + "  " + fg(*T.GREY) + desc + R)
    binds.append("")
    left = W.panel("HELP", info, ctx.cw, T.AM)
    right = W.panel("KEYS", binds, ctx.cw, T.GN)
    return out + [ctx.m + a + " " * ctx.gap + b
                  for a, b in zip(left, right)]