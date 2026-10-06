"""Settings persistence to JSON config file."""

import json
import os
from ..utils.bootstrap import IS_WIN

if IS_WIN:
    CONFIG_DIR = os.path.join(os.environ.get("APPDATA", ""), "swivel")
else:
    CONFIG_DIR = os.path.join(os.path.expanduser("~"), ".config", "swivel")

CONFIG_FILE = os.path.join(CONFIG_DIR, "config.json")

DEFAULTS = {
    "theme": "SWIVEL",
    "animations": True,
    "sparklines": True,
    "compact": True,
    "brightness": 100,
    "refresh_hz": 20,
    "webhook": "",
    "target": "",
}


def ensure_config_dir():
    os.makedirs(CONFIG_DIR, exist_ok=True)


def load():
    """Load settings from config file, return defaults if not found."""
    ensure_config_dir()
    if not os.path.exists(CONFIG_FILE):
        return DEFAULTS.copy()
    try:
        with open(CONFIG_FILE, "r", encoding="utf-8") as f:
            data = json.load(f)
        return {**DEFAULTS, **data}
    except Exception:
        return DEFAULTS.copy()


def save(settings):
    """Save settings to config file."""
    ensure_config_dir()
    try:
        with open(CONFIG_FILE, "w", encoding="utf-8") as f:
            json.dump(settings, f, indent=2)
        return True
    except Exception:
        return False


def get(key, default=None):
    """Get a single setting value."""
    data = load()
    return data.get(key, default)


def set(key, value):
    """Set a single setting value and save."""
    data = load()
    data[key] = value
    return save(data)