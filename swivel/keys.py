"""Raw keystroke polling and inline text editing."""

import os
import select
import sys
import time

from .bootstrap import IS_WIN

if IS_WIN:
    import msvcrt

ESC = "\x1b"
BACKSPACE = ("\x7f", "\x08")
SEQ = ("\r", "\n", " ")
ESC_TIMEOUT = .08


def poll(fd=None):
    """Return one keypress, or None if nothing is pending.

    Escape sequences are swallowed so they never reach the key handlers.
    """
    if IS_WIN:
        try:
            if not msvcrt.kbhit():
                return None
            ch = msvcrt.getwch()
            if ch in ("\x00", "\xe0"):
                msvcrt.getwch()
                return None
            return ch
        except Exception:
            return None
    if not select.select([sys.stdin], [], [], 0)[0]:
        return None
    ch = os.read(fd, 1).decode("utf-8", "replace")
    if ch != ESC:
        return ch
    seq, deadline = ch, time.time() + ESC_TIMEOUT
    while time.time() < deadline:
        if select.select([sys.stdin], [], [], ESC_TIMEOUT)[0]:
            seq += os.read(fd, 1).decode("utf-8", "replace")
            deadline = time.time() + ESC_TIMEOUT
        else:
            break
    return ESC


def edit(ch, text):
    """Apply one character to the buffer being edited."""
    if ch in BACKSPACE:
        return text[:-1]
    if ch in ("\r", "\n"):
        return text
    if len(ch) == 1 and ord(ch) >= 32:
        return text + ch
    return text


def done(ch):
    return ch in ("\r", "\n")