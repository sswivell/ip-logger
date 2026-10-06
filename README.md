# Swivel

A terminal-based IP logger with real-time traffic visualization, browser fingerprinting, and Discord webhook notifications.

## Features

- **Real-time dashboard** - Live traffic charts, hit logs, and statistics
- **Browser fingerprinting** - Canvas, WebGL, Audio, fonts, WebRTC, battery, media devices
- **Risk scoring** - VPN/datacenter detection, Tor exit nodes, scanner signatures
- **GeoIP lookup** - Country, city, ISP, ASN with flag emojis
- **Discord webhooks** - Rich embed notifications for every hit
- **Cloudflare tunnel** - Automatic public URL via cloudflared
- **Themes & customization** - Multiple color themes, brightness, refresh rate
- **Cross-platform** - Pure Python with ANSI terminal UI

## Screenshots

```
┌────────────────────────────────────────────────────────────────┐
│ SETUP  LIVE  STATS  FP  SETTINGS  ABOUT     14:32:15          │
│ iplogger / live                                                │
│ ████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ LIVE HITS                                                  │ │
│ │ 14:31:22  192.168.1.100  OK        US, CA, San Francisco  │ │
│ │ 14:31:45  10.0.0.50      VPN       NL, NH, Amsterdam      │ │
│ │ 14:32:01  203.0.113.10   TOR       ?, ?, ?                │ │
│ │ waiting...                                                 │ │
│ └────────────────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ LOAD                                                       │ │
│ │ hits  ████████████████████  42                             │ │
│ │ ▁▂▃▅▆▇█▇▆▅▃▂▁▁▂▃▅▆▇█▇▆▅▃▂▁                                │ │
│ │ lat 42ms      up 127s                                      │ │
│ └────────────────────────────────────────────────────────────┘ │
│ w/s nav  enter edit  tab next  q quit                    swivel v1.3 │
└────────────────────────────────────────────────────────────────┘
```

## Installation

### Prerequisites

- Python 3.10+
- Windows/Linux/macOS (terminal with ANSI support)

### Quick Start

```bash
git clone https://github.com/yourusername/swivel.git
cd swivel
pip install -e .
swivel
```

Or run directly:

```bash
python swivel.py
```

## Usage

```bash
# Run with default settings
swivel

# Run with webhook and target
swivel "https://discord.com/api/webhooks/..." "https://example.com"

# Help
swivel --help
```

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Tab` / `]` | Next tab |
| `[` | Previous tab |
| `1-6` | Jump to tab |
| `W` / `S` | Navigate up/down |
| `Enter` | Edit / Activate |
| `A` / `D` | Adjust setting left/right |
| `R` | Reset simulation |
| `C` | Clear hits |
| `Q` / `Ctrl+C` | Quit |

### Tabs

1. **SETUP** - Enter Discord webhook URL and target redirect URL
2. **LIVE** - Real-time hit stream with risk tags and traffic chart
3. **STATS** - Counters, top countries, top ISPs
4. **FP** - Browser fingerprints for recent hits
5. **SETTINGS** - Theme, animations, sparklines, brightness, refresh rate
6. **ABOUT** - Help and key bindings

## Architecture

```
swivel/
├── swivel.py           # Entry point
├── core/               # Business logic
│   ├── sim.py          # Traffic simulation
│   ├── server.py       # HTTP server + Discord webhooks
│   ├── payload.py      # Fingerprinting JS payload
│   └── geo.py          # GeoIP, Tor, classification
├── ui/                 # Terminal UI
│   ├── app.py          # Main app, event loop
│   ├── splash.py       # Boot animation
│   ├── theme.py        # Color themes, ANSI rendering
│   ├── widgets.py      # Reusable UI components
│   └── screens/        # Dashboard screens
│       ├── setup.py
│       ├── live.py
│       ├── stats.py
│       ├── fingerprint.py
│       ├── settings.py
│       └── about.py
└── utils/              # Utilities
    ├── bootstrap.py    # ANSI, cloudflared download
    ├── tunnel.py       # Cloudflare tunnel management
    └── keys.py         # Raw terminal input
```

## Configuration

Settings are adjusted in the **SETTINGS** tab and persist for the session:

- **Theme**: BLUE, SWIVEL, EMERALD, NIGHTS, AMBER
- **Animations**: Enable/disable UI animations
- **Sparklines**: Show/hide traffic sparklines
- **Compact**: Compact panel layout
- **Brightness**: 30-150%
- **Refresh Hz**: 5-60 FPS

## Discord Webhook Format

Each hit sends a rich embed with:

- IP, location, flag emoji
- Risk score (0-100) with color coding
- Tags (VPN, DC, TOR, HOST, PX, SCAN)
- OS/Browser, ISP, ASN
- User-Agent
- Browser fingerprint details (when available)
- Google Maps link (when coordinates available)

## Cloudflare Tunnel

On first run, swivel downloads `cloudflared` automatically and creates a public HTTPS tunnel to your local server. The public URL appears in the **SETUP** tab status panel.

To use a pre-installed cloudflared:

```bash
# Place cloudflared.exe (Windows) or cloudflared (Linux/macOS) in PATH
# or in ~/.swivel_cloudflared
```

## Development

```bash
# Install in development mode
pip install -e .

# Run tests
pytest tests/

# Type checking
mypy swivel/
```

## License

MIT License - see [LICENSE](LICENSE) for details.

## Disclaimer

This tool is for educational and authorized testing purposes only. Unauthorized tracking of individuals may violate privacy laws. Use responsibly and only on systems you own or have explicit permission to test.