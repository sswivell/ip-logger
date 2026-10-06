"""Dashboard screens."""

from .setup import setup
from .live import live
from .stats import stats
from .fingerprint import fingerprint
from .settings import settings
from .about import about
from .replay import replay
from .alerts import alerts_screen

SCREENS = (setup, live, stats, fingerprint, replay, alerts_screen, settings, about)

__all__ = ["SCREENS", "setup", "live", "stats", "fingerprint", "settings", "about", "replay", "alerts_screen"]