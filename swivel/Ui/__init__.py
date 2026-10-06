"""UI package: screens, widgets, theme, splash."""

from .theme import (
    THEMES, WHITE, GREY, TRK, ALERT, CY, VI, PK, GN, AM,
    SHADES, BRAND, B, R, E,
    fg, bg, mix, ramp, gradient, vlen, pad, lr, set_theme, set_brightness,
)
from .widgets import (
    bar, slider, sparkline, marker, label, highlight, panel, rule, tabs, stat_row, kv,
)
from .splash import splash
from .app import App, Form, Context, render, handle, loop, main as run_app

__all__ = [
    "THEMES", "WHITE", "GREY", "TRK", "ALERT", "CY", "VI", "PK", "GN", "AM",
    "SHADES", "BRAND", "B", "R", "E",
    "fg", "bg", "mix", "ramp", "gradient", "vlen", "pad", "lr", "set_theme", "set_brightness",
    "bar", "slider", "sparkline", "marker", "label", "highlight", "panel", "rule", "tabs", "stat_row", "kv",
    "splash",
    "App", "Form", "Context", "render", "handle", "loop", "run_app",
]