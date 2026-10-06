"""Colour palettes, text measurement and drawing primitives."""

import re

from .bootstrap import TRUE_COLOR

E = "\x1b["
R = E + "0m"
B = E + "1m"
BRIGHT = [1.0]

WHITE = (255, 248, 235)
GREY = (110, 130, 170)
TRK = (28, 36, 64)
ALERT = (255, 90, 90)

THEMES = {
    "BLUE": ((30, 90, 200), (60, 140, 240), (100, 180, 255),
             (150, 210, 255), (200, 230, 255)),
    "SWIVEL": ((100, 180, 255), (140, 90, 230), (230, 40, 60),
               (0, 220, 140), (255, 200, 40)),
    "EMERALD": ((0, 220, 140), (0, 180, 220), (100, 180, 255),
                (180, 255, 200), (255, 255, 220)),
    "NIGHTS": ((80, 40, 180), (180, 90, 220), (255, 200, 40),
               (100, 180, 255), (230, 40, 60)),
    "AMBER": ((255, 200, 100), (230, 150, 60), (255, 140, 50),
              (255, 210, 120), (255, 180, 60)),
}

CY = VI = PK = GN = AM = None
SHADES = []
BRAND = []


def set_theme(name):
    """Activate a named palette, deriving its shades and brand ramp.

    The derived lists are mutated in place so importers holding a reference
    always observe the active theme.
    """
    global CY, VI, PK, GN, AM
    CY, VI, PK, GN, AM = THEMES[name]
    SHADES[:] = [mix(CY, (0, 0, 0), .55), CY, AM, PK, ALERT]
    BRAND[:] = [CY, VI, AM, WHITE]


def set_brightness(v):
    BRIGHT[0] = v / 100.


def _scale(r, g, b):
    return [min(255, int(x * BRIGHT[0])) for x in (r, g, b)]


def fg(r, g, b):
    """Foreground 24-bit colour, falling back to the 256-colour cube."""
    r, g, b = _scale(r, g, b)
    if TRUE_COLOR:
        return "%s38;2;%d;%d;%dm" % (E, r, g, b)
    i = 16 + 36 * round(r / 51) + 6 * round(g / 51) + round(b / 51)
    return "%s38;5;%dm" % (E, i)


def bg(r, g, b):
    """Background counterpart of fg()."""
    r, g, b = _scale(r, g, b)
    if TRUE_COLOR:
        return "%s48;2;%d;%d;%dm" % (E, r, g, b)
    i = 16 + 36 * round(r / 51) + 6 * round(g / 51) + round(b / 51)
    return "%s48;5;%dm" % (E, i)


f = fg


def mix(a, b, t):
    """Linear blend between two RGB tuples."""
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def ramp(t, stops):
    """Sample a colour ramp at t in [0, 1]."""
    t = max(0.0, min(1.0, t))
    x = t * (len(stops) - 1)
    i = min(int(x), len(stops) - 2)
    return mix(stops[i], stops[i + 1], x - i)


ANSI = re.compile("\x1b\\[[0-9;]*m")


def vlen(s):
    """Visible width of a string, ignoring escape sequences."""
    return len(ANSI.sub("", s))


def pad(s, w):
    return s + " " * max(0, w - vlen(s))


def lr(left, right, w):
    """Left/right justified row with a computed gutter."""
    return left + " " * max(1, w - vlen(left) - vlen(right)) + right


def gradient(text, stops, phase=0.0):
    """Paint each non-space character along a colour ramp."""
    n = max(1, len(text) - 1)
    out = []
    for i, ch in enumerate(text):
        out.append(ch if ch == " " else
                   fg(*ramp(((i / n) + phase) % 1.0, stops)) + ch)
    return "".join(out) + R