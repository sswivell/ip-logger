"""swivel - terminal IP logger with browser fingerprinting."""

# Import from utils at module level
from .utils.bootstrap import (HT, IS_POSIX, IS_WIN, TM, check_tty, enable_ansi,
                              enter_alt_screen, leave_alt_screen, raw_terminal)

__version__ = "1.3"
__all__ = ["HT", "IS_POSIX", "IS_WIN", "TM", "check_tty", "enable_ansi",
           "enter_alt_screen", "leave_alt_screen", "raw_terminal",
           "set_brightness", "set_theme", "main"]


# Lazy imports for theme functions to avoid circular imports
def set_brightness(v):
    from .ui.theme import set_brightness as _fn
    return _fn(v)


def set_theme(name):
    from .ui.theme import set_theme as _fn
    return _fn(name)


def main(argv=None):
    """Lazy re-export of the entry point to avoid an import cycle."""
    from .ui.app import main as _main
    return _main(argv)