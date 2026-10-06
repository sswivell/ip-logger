"""Reusable terminal widgets: blocks, bars, sparklines and panels."""

from . import theme as T
from .theme import B, R, bg, fg, lr, mix, pad, ramp, vlen

FB = "█"      # full block
EB = "░"      # light shade
BL = "▁▂▃▄▅▆▇█"
HZ = "─"      # horizontal rule
VT = "│"      # vertical rule
TL, TR = "╭", "╮"
BL_, BR_ = "╰", "╯"

BULLET_ON = ">"
BULLET_OFF = "."


def bar(pct, width, colour):
    """Filled progress bar over a shaded track."""
    pct = max(0, min(100, pct))
    n = int(round(pct / 100 * width))
    return (fg(*colour) + FB * n + fg(*T.TRK) + EB * (width - n) + R)


def slider(value, lo, hi, width=12):
    """Labelled < value > slider used by the settings screen."""
    t = (value - lo) / max(1, hi - lo)
    filled = int(t * width)
    body = (fg(*ramp(t, T.SHADES)) + FB * filled
            + fg(*T.TRK) + EB * (width - filled) + R)
    return "< %s %s >" % (body, fg(*T.WHITE) + B + str(value).rjust(4) + R)


def sparkline(values, width, hi=100):
    """Braille-free block sparkline of recent values."""
    out = []
    for v in values[-width:]:
        t = max(0.0, min(1.0, v / hi))
        out.append(fg(*ramp(t, T.SHADES)) + BL[int(t * 7)])
    return "".join(out) + R


def marker(active):
    return (fg(*T.SHADES[1]) + B + BULLET_ON + R) if active else \
        fg(*mix(T.GREY, (0, 0, 0), .4)) + BULLET_OFF + R


def label(text, active):
    return (fg(*T.WHITE) + B + text + R) if active else fg(*T.GREY) + text + R


def highlight(line, width, colour):
    """Dim the row background for the focused entry."""
    back = bg(*mix(colour, (0, 0, 0), .72))
    return back + line.replace(R, R + back) + pad("", width - vlen(line)) + R


def panel(title, lines, width, accent):
    """Rounded box containing a title and pre-padded body lines."""
    dim = fg(*mix(accent, (0, 0, 0), .35))
    head = dim + TL + HZ + R + " " + B + fg(*accent) + title + R + " "
    out = [head + dim + HZ * max(1, width - len(title) - 5) + TR + R]
    for line in lines:
        out.append(dim + VT + R + " " + pad(line, width - 4) + " "
                   + dim + VT + R)
    out.append(dim + BL_ + HZ * (width - 2) + BR_ + R)
    return out


def rule(width, colour=None):
    c = colour or mix(T.GREY, (0, 0, 0), .55)
    return fg(*c) + HZ * width + R


def tabs(names, active, width):
    """Tab strip plus a gradient underline."""
    chips = []
    for i, name in enumerate(names):
        chip = "  %s  " % name
        if i == active:
            chips.append(bg(*T.SHADES[i % len(T.SHADES)]) + fg(15, 20, 40)
                         + B + chip + R)
        else:
            chips.append(fg(*T.GREY) + chip + R)
    strip = "".join(
        fg(*ramp(i / max(1, width - 1), T.BRAND)) + HZ for i in range(width))
    return ["".join(chips), strip + R]


def stat_row(name, value, colour):
    return fg(*colour) + name + "   " + B + fg(*T.WHITE) + str(value).rjust(6) + R


def kv(label_text, value_text, colour):
    return (fg(*colour) + B + label_text + R + "  "
            + fg(*T.WHITE) + value_text + R)