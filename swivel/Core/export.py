"""Export session data to JSON/CSV for analysis."""

import json
import csv
import os
from datetime import datetime
from ..core import server


def export_json(path):
    """Export all hits to JSON file."""
    with server.LOCK:
        hits = list(server.HITS)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(hits, f, indent=2, default=str)
    return len(hits)


def export_csv(path):
    """Export hits to CSV file."""
    with server.LOCK:
        hits = list(server.HITS)
    if not hits:
        return 0
    fieldnames = ["ts", "ip", "loc", "cc", "isp", "risk", "tags", "os", "br", "path", "ref"]
    with open(path, "w", encoding="utf-8", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for h in hits:
            row = {k: h.get(k, "") for k in fieldnames}
            writer.writerow(row)
    return len(hits)


def export_session(path=None):
    """Auto-generate filename and export both formats."""
    if path is None:
        ts = datetime.now().strftime("%Y%m%d_%H%M%S")
        base = f"swivel_session_{ts}"
    else:
        base = os.path.splitext(path)[0]
    json_path = base + ".json"
    csv_path = base + ".csv"
    n = export_json(json_path)
    export_csv(csv_path)
    return json_path, csv_path, n