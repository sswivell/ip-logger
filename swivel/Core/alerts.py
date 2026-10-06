"""Alert system for high-risk hits."""

from ..core import server
from ..ui import theme as T
from ..ui.theme import fg, B, R


alerts = []
MAX_ALERTS = 50


def check_alert(hit):
    """Evaluate a hit for alert conditions."""
    risk = hit.get("risk", 0)
    tags = hit.get("tags", "")
    is_scan = hit.get("bad", False)
    
    if risk >= 80 or is_scan:
        level = "CRITICAL"
        color = T.SHADES[4]
    elif risk >= 60 or "TOR" in tags or "VPN" in tags:
        level = "HIGH"
        color = T.VI
    elif risk >= 40 or "PX" in tags or "HOST" in tags:
        level = "MEDIUM"
        color = T.AM
    else:
        return None
    
    alert = {
        "time": hit["ts"],
        "level": level,
        "ip": hit["ip"],
        "risk": risk,
        "tags": tags,
        "loc": hit["loc"],
        "color": color,
    }
    alerts.append(alert)
    if len(alerts) > MAX_ALERTS:
        alerts.pop(0)
    return alert


def get_alerts():
    """Return all alerts, newest first."""
    return list(reversed(alerts))


def clear_alerts():
    alerts.clear()