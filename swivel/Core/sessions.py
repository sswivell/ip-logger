"""Session recording and playback for traffic replay."""

import json
import os
from datetime import datetime
from .config import CONFIG_DIR

SESSIONS_DIR = os.path.join(CONFIG_DIR, "sessions")


def ensure_sessions_dir():
    os.makedirs(SESSIONS_DIR, exist_ok=True)


def save_session(hits, metadata=None):
    """Save a session to file."""
    ensure_sessions_dir()
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"session_{ts}.json"
    path = os.path.join(SESSIONS_DIR, filename)
    
    data = {
        "timestamp": datetime.now().isoformat(),
        "metadata": metadata or {},
        "hits": hits,
    }
    try:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, default=str)
        return path
    except Exception:
        return None


def load_session(path):
    """Load a session from file."""
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return None


def list_sessions():
    """List all saved sessions, newest first."""
    ensure_sessions_dir()
    sessions = []
    for fname in sorted(os.listdir(SESSIONS_DIR), reverse=True):
        if fname.endswith(".json") and fname.startswith("session_"):
            path = os.path.join(SESSIONS_DIR, fname)
            data = load_session(path)
            if data:
                sessions.append({
                    "file": fname,
                    "path": path,
                    "timestamp": data.get("timestamp"),
                    "hit_count": len(data.get("hits", [])),
                    "metadata": data.get("metadata", {}),
                })
    return sessions


def delete_session(path):
    """Delete a session file."""
    try:
        os.remove(path)
        return True
    except Exception:
        return False


def get_replay_data(hits):
    """Convert hits to replay format for the timeline."""
    if not hits:
        return []
    
    # Group hits by minute
    from collections import defaultdict
    buckets = defaultdict(list)
    for h in hits:
        ts = h.get("ts", "")
        if len(ts) >= 5:
            minute = ts[:5]
            buckets[minute].append(h)
    
    replay_sessions = []
    for minute in sorted(buckets.keys()):
        bucket = buckets[minute]
        max_risk = max(h.get("risk", 0) for h in bucket)
        tags = set()
        for h in bucket:
            for t in h.get("tags", "").split():
                if t:
                    tags.add(t)
        
        replay_sessions.append({
            "time": minute,
            "hits": len(bucket),
            "risk": max_risk,
            "tags": " ".join(sorted(tags)),
            "duration": f"{len(bucket)}m",
        })
    
    return replay_sessions