"""swivel - terminal IP logger with browser fingerprinting."""

from .bootstrap import (HT, IS_POSIX, IS_WIN, TM, check_tty, enable_ansi,
                        enter_alt_screen, leave_alt_screen, raw_terminal)
from .theme import set_brightness, set_theme

__version__ = "1.3"
__all__ = ["HT", "IS_POSIX", "IS_WIN", "TM", "check_tty", "enable_ansi",
           "enter_alt_screen", "leave_alt_screen", "raw_terminal",
           "set_brightness", "set_theme", "main"]


def main(argv=None):
    """Lazy re-export of the entry point to avoid an import cycle."""
    from .app import main as _main
    return _main(argv)