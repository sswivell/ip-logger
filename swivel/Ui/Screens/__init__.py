"""Dashboard screens."""

from .setup import setup
from .live import live
from .stats import stats
from .fingerprint import fingerprint
from .settings import settings
from .about import about

SCREENS = (setup, live, stats, fingerprint, settings, about)

__all__ = ["SCREENS", "setup", "live", "stats", "fingerprint", "settings", "about"]