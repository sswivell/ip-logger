"""Splash screen animation."""

import shutil
import sys
import time

from .theme import fg, gradient, ramp, vlen
from . import theme

LOGO = (
    "⠀⠀⠀⠀⠀⠀⠐⢶⣶⣶⣤⣄⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠈⢿⣿⣿⣿⣿⣿⣦⣄⠀⠀⠀⠀⠀⠀",
    "⠀⢀⣠⣶⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣄⠀⠀⠀⠀",
    "⠰⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣆⠀⠀⠀",
    "⠀⠀⠀⠈⠙⣿⣿⣿⣿⣿⣿⡿⠛⠛⢿⣿⣿⣿⣿⣄⣀⣀",
    "⠀⠀⠀⣠⣾⣿⣿⣿⣿⣿⣿⠁⢠⡄⠈⣿⣿⣿⣿⣿⣿⠟",
    "⠀⠀⣰⣿⣿⣿⣿⣿⣿⣿⣿⣷⡋⠀⠀⣿⣿⡿⢻⠟⠁⠀",
    "⠀⢠⡿⠟⠋⠉⠉⠀⠀⠉⠛⢿⣿⣷⣶⣿⣻⠁⠁⠀⠀⠀",
    "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠉⠉⠙⠃⠀⠀⠀⠀",
)
LOGO_W = max(len(r) for r in LOGO)
SPLASH_TIME = 2.2


def splash(duration=SPLASH_TIME):
    """Animated boot sequence on the alternate screen."""
    start = time.time()
    while (elapsed := time.time() - start) < duration:
        p = min(1.0, elapsed / duration)
        cols = max(40, shutil.get_terminal_size((80, 40))[0])
        margin = " " * max(0, (cols - LOGO_W) // 2)
        frame = ["\x1b[H"]
        for row in LOGO:
            span = max(1, len(row) - 1)
            frame.append(margin + "".join(
                fg(*ramp(((i / span) + elapsed * .6) % 1.0, theme.BRAND)) + ch
                for i, ch in enumerate(row)) + "\x1b[K")
        frame.append("")
        title = "I P L O G G E R"
        frame.append(" " * max(0, (cols - len(title)) // 2)
                     + gradient(title, theme.BRAND, elapsed * .3))
        frame.append("")
        bar_width = min(44, cols - 24)
        filled = int(p * bar_width)
        bar = (fg(*theme.AM) + "\u2588" * filled + fg(*theme.TRK)
               + "\u2591" * (bar_width - filled) + "\x1b[0m")
        spin = "|/-\\"[int(elapsed * 14) % 4]
        line = "%s%s%s  %sbooting...%s  [%s] %s%s%s%%%s" % (
            fg(*ramp((elapsed * .8) % 1.0, theme.BRAND)), spin, "\x1b[0m",
            fg(*theme.WHITE), "\x1b[0m", bar, "\x1b[1m", fg(*theme.AM), int(p * 100), "\x1b[0m")
        frame.append(" " * max(0, (cols - vlen(line)) // 2) + line)
        sys.stdout.write("\n".join(frame) + "\x1b[J")
        sys.stdout.flush()
        time.sleep(1 / 30)