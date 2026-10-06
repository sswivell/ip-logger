"""Dashboard screens."""

from .setup import setup
from .live import live
from .stats import stats
from .fingerprint import fingerprint
from .settings import settings
from .about import about
from .replay import replay
from .alerts import alerts_screen
from .sessions import sessions_screen
from .payload import payload_screen

SCREENS = (setup, live, stats, fingerprint, replay, sessions_screen, payload_screen, alerts_screen, settings, about)

__all__ = ["SCREENS", "setup", "live", "stats", "fingerprint", "settings", "about", "replay", "alerts_screen", "sessions_screen", "payload_screen"]