# Swivel IP Logger

Terminal-based IP logger with real-time traffic visualization, browser fingerprinting, and Discord webhook notifications.

## Quick Start

```bash
git clone https://github.com/sswivell/ip-logger.git
cd ip-logger
pip install -e .
swivel
```

Or run directly:
```bash
python swivel.py
```

## Usage

```bash
# With Discord webhook and target URL
swivel "https://discord.com/api/webhooks/..." "https://example.com"

# Keyboard shortcuts
Tab/]    Next tab          W/S     Navigate
[        Prev tab          A/D     Adjust settings
1-6      Jump to tab       Enter   Edit/activate
R        Reset sim         C       Clear hits
Q        Quit
```

## Tabs

1. **SETUP** - Webhook + target URL
2. **LIVE** - Real-time hits + traffic chart
3. **STATS** - Counters, top countries, ISPs
4. **FP** - Browser fingerprints
5. **SETTINGS** - Theme, brightness, refresh rate
6. **ABOUT** - Help & key bindings