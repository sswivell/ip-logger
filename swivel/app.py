"""Application state, splash screen and the frame loop."""

import shutil
import sys
import threading
import time

from . import keys, screens, server, theme, widgets as W
from .bootstrap import (HT, check_tty, enable_ansi, enter_alt_screen,
                         leave_alt_screen, raw_terminal)
from .sim import Sim
from .theme import (B, GREY, R, THEMES, WHITE, fg, gradient,
                    lr, ramp, set_brightness, set_theme, vlen)

TABS = ("SETUP", "LIVE", "STATS", "FP", "SETTINGS", "ABOUT")
MAX_COL = 100
GAP = 2

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


class Form:
    """Setup-screen input buffer state."""

    def __init__(self, values=("", "")):
        self.values = list(values)
        self.sel = 0
        self.editing = False


class App:
    """Everything the renderer needs: options, selection, traffic series."""

    def __init__(self, form=None):
        self.sim = Sim()
        self.tab = 0
        self.sel = 0
        self.form = form or Form()
        self.options = [
            {"name": "Theme", "type": "choice",
             "options": list(THEMES), "value": 0},
            {"name": "Animations", "type": "bool", "value": True},
            {"name": "Sparklines", "type": "bool", "value": True},
            {"name": "Compact", "type": "bool", "value": True},
            {"name": "Brightness", "type": "int", "value": 100,
             "min": 30, "max": 150, "step": 5},
            {"name": "Refresh Hz", "type": "int", "value": 20,
             "min": 5, "max": 60, "step": 5},
        ]
        self.apply()

    def get(self, name):
        """Read one option by display name; choices resolve to their label."""
        for o in self.options:
            if o["name"] != name:
                continue
            if o["type"] == "choice":
                return o["options"][o["value"]]
            return o["value"]
        return None

    def apply(self):
        set_brightness(self.get("Brightness"))
        set_theme(self.get("Theme"))

    def adjust(self, direction):
        """Nudge the highlighted option left or right."""
        o = self.options[self.sel]
        if o["type"] == "bool":
            o["value"] = not o["value"]
        elif o["type"] == "choice":
            o["value"] = (o["value"] + direction) % len(o["options"])
        else:
            o["value"] = max(o["min"],
                             min(o["max"], o["value"] + direction * o["step"]))
        self.apply()


class Context:
    """Per-frame layout metrics derived from the terminal size."""

    def __init__(self, nw):
        self.nw = nw
        cols, rows = shutil.get_terminal_size((80, 40))
        self.cols = max(40, cols)
        self.rows = rows
        self.w = min(self.cols - 2, MAX_COL)
        self.m = " " * max(0, (self.cols - self.w) // 2)
        self.gap = GAP
        self.cw = (self.w - self.gap) // 2
        self.inn = self.cw - 4


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
               + "\u2591" * (bar_width - filled) + R)
        spin = "|/-\\"[int(elapsed * 14) % 4]
        line = "%s%s%s  %sbooting...%s  [%s] %s%s%s%%%s" % (
            fg(*ramp((elapsed * .8) % 1.0, theme.BRAND)), spin, R,
            fg(*WHITE), R, bar, B, fg(*theme.AM), int(p * 100), R)
        frame.append(" " * max(0, (cols - vlen(line)) // 2) + line)
        sys.stdout.write("\n".join(frame) + "\x1b[J")
        sys.stdout.flush()
        time.sleep(1 / 30)


def handle(app, ch):
    """Dispatch one keypress to the active screen; False quits the app."""
    if app.form.editing:
        if ch == "\x1b" or ch in ("\r", "\n"):
            app.form.editing = False
        else:
            app.form.values[app.form.sel] = keys.edit(
                ch, app.form.values[app.form.sel])
        return True
    if ch in ("q", "\x03"):
        return False
    if ch == "\x1b":
        return True
    if ch in ("\t", "]"):
        app.tab, app.sel = (app.tab + 1) % len(TABS), 0
    elif ch == "[":
        app.tab, app.sel = (app.tab - 1) % len(TABS), 0
    elif ch in "123456":
        app.tab, app.sel = int(ch) - 1, 0
    elif ch == "r":
        app.sim = Sim()
    elif ch == "c":
        with server.LOCK:
            server.HITS.clear()
    else:
        _screen_keys(app, ch)
    return True


def _screen_keys(app, ch):
    if app.tab == 0:
        if ch == "w":
            app.form.sel = (app.form.sel - 1) % 3
        elif ch == "s":
            app.form.sel = (app.form.sel + 1) % 3
        elif ch in ("\r", "\n", " "):
            if app.form.sel == 2:
                server.start(*app.form.values)
            else:
                app.form.editing = True
    elif app.tab == 4:
        n = len(app.options)
        if ch == "w":
            app.sel = (app.sel - 1) % n
        elif ch == "s":
            app.sel = (app.sel + 1) % n
        elif ch == "a":
            app.adjust(-1)
        elif ch == "d":
            app.adjust(1)
        elif ch in ("\r", "\n", " "):
            app.adjust(1)


def render(app, ctx):
    """Compose the full frame as a list of lines."""
    out = [ctx.m + x for x in W.tabs(TABS, app.tab, ctx.w)]
    out += ["", ctx.m + lr(
        fg(*GREY) + "iplogger / " + TABS[app.tab].lower() + R,
        B + fg(*WHITE) + time.strftime("%H:%M:%S") + R, ctx.w), ""]
    out += screens.SCREENS[app.tab](app, ctx)
    out += ["", ctx.m + W.rule(ctx.w)]
    hints = (fg(*GREY) + "w/s nav" + R + "  " + fg(*WHITE) + "enter" + R
             + " " + fg(*GREY) + "edit" + R + "  " + fg(*WHITE) + "tab" + R
             + " " + fg(*GREY) + "next" + R + "  " + fg(*WHITE) + "q" + R
             + " " + fg(*GREY) + "quit" + R)
    out.append(ctx.m + lr(hints, fg(*GREY) + "swivel v1.3" + R, ctx.w))
    return out[:max(1, ctx.rows)]


def loop(app):
    """Render frames and pump input until the user quits."""
    with raw_terminal() as fd:
        last = time.time()
        while True:
            now = time.time()
            app.sim.step(now - last)
            last = now
            ctx = Context(now)
            body = "\n".join(line + "\x1b[K" for line in render(app, ctx))
            sys.stdout.write("\x1b[H" + body + "\x1b[J")
            sys.stdout.flush()
            ch = keys.poll(fd)
            if ch is not None and not handle(app, ch):
                break
            time.sleep(1 / max(1, app.get("Refresh Hz")))


def main(argv=None):
    """Entry point: bootstrap dependencies, then run the interface."""
    from . import geo, tunnel
    from .bootstrap import ensure_pip

    args = list(sys.argv[1:] if argv is None else argv)
    form = Form((args[1] if len(args) > 1 else "", args[0] if args else ""))
    enable_ansi()
    if not check_tty():
        return 1
    if not HT:
        print("[!] no tty control available")
        return 1

    ensure_pip()
    binary = tunnel.find()
    if binary:
        print("[+] cloudflared found: " + binary)
    else:
        print("[*] cloudflared not found, downloading...")
        binary = tunnel.ensure()
        print("[+] cloudflared ready: " + binary if binary
              else "[!] cloudflared unavailable - local only mode")
    print("[+] bootstrap complete\n")
    threading.Thread(target=geo.load_tor, daemon=True).start()

    app = App(form)
    enter_alt_screen()
    try:
        splash()
        loop(app)
    except KeyboardInterrupt:
        pass
    finally:
        server.stop()
        leave_alt_screen()
        for line in tunnel.ERRORS:
            print(line)
    return 0