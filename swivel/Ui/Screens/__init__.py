"""Dashboard screens."""

from .setup import setup
from .live import live
from .stats import stats
from .fingerprint import fingerprint
from .settings import settings
from .about import about
from .replay import replay

SCREENS = (setup, live, stats, fingerprint, settings, about, replay)

__all__ = ["SCREENS", "setup", "live", "stats", "fingerprint", "settings", "about", "replay"]