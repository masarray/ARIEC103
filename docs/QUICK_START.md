# ArIEC103 Quick Start

This guide is for a first relay communication check using the Windows desktop app.

## 1. Prepare the connection

- Use a known working USB-to-serial or RS-485 adapter.
- Confirm the relay communication port, baudrate, parity, stop bit, link address, and common address.
- Keep the relay wiring simple for the first test: one master, one relay, one serial segment.

## 2. Start the desktop app

For a portable release package, run:

```bat
Start-ArIEC103.bat
```

For source development:

```bash
dotnet run --project src/ArIEC103.Desktop
```

## 3. Open Setup

Set these values first:

| Field | Recommended first value |
|---|---|
| COM Port | Adapter COM port shown in Windows Device Manager |
| Baudrate | Relay configured value, commonly 9600 or 19200 |
| Parity | Relay configured value |
| Link Address | IEC-103 link address of the relay |
| Common Address | Relay common address used by the station/project |
| Timeout | Start conservative, then tune later |
| Reset FCB | Enabled for normal startup |
| General Interrogation | Enabled when a startup snapshot is needed |

## 4. Start the session

Click **Start** and watch these areas:

- **Operator Evidence** — readable session activity.
- **Line Monitor / Frame Trace** — raw TX/RX evidence.
- **Value Viewer** — latest decoded values.
- **Relay Event Log** — relay timestamped events.
- **Diagnostics** — communication and protocol issues.

## 5. First acceptance checks

A healthy first check normally shows:

- serial port opens without error;
- relay answers after link reset or first request;
- General Interrogation starts and finishes, when enabled;
- Class 2 polling continues at the configured interval;
- Class 1 is requested only when event data is pending;
- timeout/checksum/malformed counters stay low or zero.

## 6. Export evidence

Export Markdown evidence after the test. Review the report before sharing it outside the project team.

ArIEC103 intentionally avoids exposing full local mapping profile paths in exported public evidence by default.
