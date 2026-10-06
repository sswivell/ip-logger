"""Platform detection, terminal control and dependency bootstrap."""

import os
import platform
import subprocess
import sys
import urllib.request
from contextlib import contextmanager

IS_WIN = platform.system().lower() == "windows"
IS_POSIX = not IS_WIN
TM = bool(
    os.environ.get("TERMUX_VERSION")
    or "com.termux" in os.environ.get("PREFIX", "")
)
TRUE_COLOR = TM or os.environ.get(
    "COLORTERM", ""
).lower() in ("truecolor", "24bit")

if IS_WIN:
    try:
        import msvcrt

        HT = 1
    except ImportError:
        HT = 0
else:
    try:
        import termios
        import tty

        HT = 1
    except ImportError:
        HT = 0


def sh(cmd, quiet=True):
    """Run a command, optionally swallowing its output."""
    kw = {"stdout": subprocess.DEVNULL,
          "stderr": subprocess.DEVNULL} if quiet else {}
    try:
        p = subprocess.Popen(cmd, shell=isinstance(cmd, str), **kw)
        p.communicate()
        return p.returncode == 0
    except Exception:
        return False


def have_pip():
    return sh([sys.executable, "-m", "pip", "--version"])


def ensure_pip():
    if have_pip():
        return True
    print("[*] pip missing, installing...")
    if TM:
        sh(["pkg", "install", "-y", "python-pip"])
    elif IS_WIN:
        sh([sys.executable, "-m", "ensurepip"])
        sh([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])
    else:
        try:
            with urllib.request.urlopen(
                "https://bootstrap.pypa.io/get-pip.py", timeout=15
            ) as r:
                data = r.read()
            with open("get-pip.py", "wb") as fp:
                fp.write(data)
            sh([sys.executable, "get-pip.py"])
            os.remove("get-pip.py")
        except Exception as e:
            print("[!] pip bootstrap failed:", e)
            return False
    return have_pip()


def enable_ansi():
    """Turn on VT100 escape handling for the Windows console."""
    if not IS_WIN:
        return
    try:
        import ctypes

        k = ctypes.windll.kernel32
        mode = ctypes.c_ulong()
        k.GetConsoleMode(k.GetStdHandle(-11), ctypes.byref(mode))
        mode.value |= 0x0004
        k.SetConsoleMode(k.GetStdHandle(-11), mode)
    except Exception as e:
        print("[!] ansi enable failed:", e)


def check_tty():
    """Refuse to start unless we own a real terminal."""
    if not sys.stdin.isatty():
        print("[!] stdin is not a tty.")
        print("[!] run this in a real terminal:")
        if IS_WIN:
            print("    powershell - run:")
            print("        python swivel.py")
        else:
            print("    termux       - open termux, run:")
            print("                   python swivel.py")
            print("    ssh          - should work by default")
            print("    tmux/screen  - should work by default")
        print()
        return False
    return sys.stdout.isatty()


@contextmanager
def raw_terminal():
    """Put the tty in cbreak mode for the duration of the block."""
    fd = old = None
    if IS_POSIX:
        fd = sys.stdin.fileno()
        import termios
        import tty
        old = termios.tcgetattr(fd)
        tty.setcbreak(fd)
    try:
        yield fd
    finally:
        if fd is not None:
            import termios
            termios.tcsetattr(fd, termios.TCSADRAIN, old)


def enter_alt_screen():
    sys.stdout.write("\x1b[?1049h\x1b[?25l")
    sys.stdout.flush()


def leave_alt_screen():
    sys.stdout.write("\x1b[?25h\x1b[?1049l\x1b[0m")
    sys.stdout.flush()