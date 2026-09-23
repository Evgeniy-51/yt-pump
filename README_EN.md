# YtPump

[Русский](README.md) | **English**

A portable Windows GUI for [yt-dlp](https://github.com/yt-dlp/yt-dlp). Paste a link, pick quality, audio track and container, download. No installer, no Python, no FFmpeg on `PATH`.

<p align="center">
  <img src="docs/yt-pump-main.jpg" width="780" alt="YtPump main window: link, quality, audio track, output folder and the finished file">
</p>

> You are responsible for complying with copyright law and the terms of YouTube and any other site. YtPump does not bypass age gates, region locks, or login walls.

---

## Contents

- [Features](#features)
- [System requirements](#system-requirements)
- [Install and run](#install-and-run)
- [Usage](#usage)
- [Browser session (cookies)](#browser-session-cookies)
- [Proxy](#proxy)
- [Settings and portable mode](#settings-and-portable-mode)
- [Security and privacy](#security-and-privacy)
- [Troubleshooting](#troubleshooting)
- [Building from source](#building-from-source)
- [Architecture](#architecture)
- [Licenses](#licenses)

---

## Features

- **Automatic link probing.** Once a URL is pasted, the app asks yt-dlp for available resolutions, audio tracks and subtitles.
- **Quality selection** — "Best" or a specific frame height (`1080p`, `720p`, …).
- **Audio track selection** — original or dubbed, when a video has several audio tracks.
- **Three output modes:**
  | Mode | What it does |
  |---|---|
  | Best (MKV) | Best video and audio streams, merged without re-encoding |
  | Compatible MP4 | Prefers H.264 + AAC so the file plays on any player/device |
  | Audio only (M4A) | Audio only, AAC. If YouTube already serves AAC — no re-encoding |
- **Subtitles** as a separate `.srt` file next to the video (manual and auto-generated), with language selection.
- **File name** is editable before downloading; `[video id]` is always appended.
- **Proxy** support: HTTP / HTTPS / SOCKS5, multiple saved profiles, built-in connection test.
- **Automatic browser session reuse** (Firefox, Edge, etc.) when YouTube asks to confirm you're not a bot.
- **History** of the last 10 links.
- **Progress, speed, ETA**, cancel button, a log with full yt-dlp output.
- **Russian and English UI**; the default follows the OS language.
- **Portable**: everything lives in one folder, settings are stored next to the app.

**Not supported:** playlists (a playlist link downloads a single video only), username/password sign-in, yt-dlp auto-update.

---

## System requirements

- Windows 10 / 11, x64.
- No .NET installation needed — the release build is self-contained.
- ~320 MB of disk space (app + yt-dlp, FFmpeg, Deno); the archive is ~180 MB.
- For videos where YouTube requires confirmation — an installed **Firefox** or **Edge**.

---

## Install and run

1. Download `YtPump-<version>-win-x64.zip` from the Releases page.
2. Extract it to any **writable** folder (e.g. `D:\Apps\YtPump`). Don't extract to `C:\Program Files` — in portable mode the app writes settings next to itself.
3. Run `YtPump.exe`.

Archive layout:

```text
YtPump\
├─ YtPump.exe            # the app (single-file, self-contained)
├─ portable.flag         # enables portable mode (see below)
├─ README.md / README_EN.md
├─ LICENSE
└─ tools\
   ├─ yt-dlp.exe         # downloader
   ├─ ffmpeg.exe         # stream merging (LGPL shared build)
   ├─ ffprobe.exe
   ├─ deno.exe           # JS runtime for the current YouTube extractor
   ├─ *.dll              # FFmpeg libraries
   └─ LICENSE-*.txt
```

YtPump looks for tools **only** in the `tools\` folder next to `YtPump.exe` — the system `PATH` is ignored. If any file is missing, a banner lists what's missing and downloading is blocked.

On first launch Windows SmartScreen may warn about an unknown publisher: "More info" → "Run anyway".

---

## Usage

1. **Link.** Paste a URL into the field or click "Paste link from clipboard". Probing starts automatically (~0.4 s after the field changes).
2. **Options.** Choose quality, audio track and container.
   - **MKV** keeps the original streams without re-encoding — maximum quality.
   - **MP4** is more convenient for playback on most players and devices.
   - **Audio only** saves the sound without the video track.
3. **Folder.** Choose the output folder using the button next to the path field. Default: "Videos", falling back to "Downloads", then "Documents".
4. **File name.** Pre-filled from the video title, editable. Final name: `<name> [<id>].<ext>`. Characters forbidden on Windows are stripped automatically; length is capped at 180 characters.
5. **Subtitles.** If the video has subtitles, tick the checkbox and pick a language — an `.srt` file appears next to the video. Subtitles are not embedded into the container.
6. **Download.** Percentage, speed and remaining time are shown during download. When done, "Open folder" highlights the file in Explorer.

Cancelling leaves temporary `*.part` files in the folder — delete them manually if needed. Closing the window mid-download asks for confirmation; yt-dlp/FFmpeg processes are guaranteed to terminate together with the app.

The "Help" button in the window opens a short guide.

---

## Browser session (cookies)

YtPump does not talk to YouTube itself — yt-dlp does. YouTube sometimes flags such requests as automated and responds with "Sign in to confirm you're not a bot". In that case a regular browser session is required — **not a login and password, but the site's cookies**.

**What to do:**

1. Open the same video in **Firefox** or **Edge** as a regular web page.
2. Refresh the page to make sure the session is active.
3. Paste the link again or restart the download in YtPump.

You don't need to close the browser. YtPump (via yt-dlp `--cookies-from-browser`) reads only YouTube cookies and never receives logins, passwords, or browsing history.

**Use Firefox or Edge.** Current Chrome versions encrypt cookies (App-Bound Encryption) so that third-party programs, including yt-dlp, usually cannot decrypt them. Opening the video in Chrome therefore usually doesn't help — even if you're signed in to a Google account.

This issue is **not related** to your internet connection or proxy.

<details>
<summary>How it works under the hood</summary>

- While probing a link, YtPump iterates over installed browsers in this order: `firefox → edge → brave → vivaldi → opera → chrome → chromium → whale`. A browser is detected by the presence of its profile directory in `%AppData%` / `%LocalAppData%`.
- If yt-dlp returns an auth error (`Sign in to confirm`, `HTTP 401/403`) or fails to decrypt cookies (`DPAPI`), the next browser is tried.
- The browser that succeeded is remembered and tried first next time; it is also used for downloading.
- If no browser is found, requests are made without cookies.

</details>

---

## Proxy

Toggle it with the switch on the left of the window header; the active profile name is shown next to it. The gear icon opens settings.

- Types: **HTTP**, **HTTPS**, **SOCKS5** (default).
- SOCKS5 is always passed as `socks5h://` — DNS queries are routed through the proxy too.
- Host and port (1–65535) are required; username and password are optional. IPv6 addresses are supported.
- Up to 10 saved profiles: create, duplicate, delete, pick the active one.
- **Test** runs in two stages: a TCP connect to the proxy (5 s timeout), then a real metadata request for a test video via yt-dlp (45 s timeout).
- If the proxy is enabled but the profile is incomplete, the settings window opens automatically.

---

## Settings and portable mode

| Mode | Condition | Settings location |
|---|---|---|
| Portable | `portable.flag` exists next to `YtPump.exe` | `<app folder>\data\settings.json` |
| Regular | no `portable.flag` | `%AppData%\YtPump\settings.json` |

- If the portable folder isn't writable, the app shows a warning and does **not** silently fall back to `%AppData%`.
- A corrupted `settings.json` is saved as `settings.json.bak`, and defaults are loaded.
- Writes are atomic (via a temp file), encoding is UTF-8 without BOM.
- Stored: UI language, last folder, container, preferred quality, subtitle language, proxy profiles, last successful cookie browser, link history, window size and position.

The settings path and mode are printed to the log at startup.

---

## Security and privacy

- The proxy password is stored in `settings.json` only in encrypted form — **Windows DPAPI** (`CurrentUser` scope). It never appears in plain text in the file or the log.
- Credentials in URLs are masked in every log line (`socks5h://***:***@host:port`).
- The app does not use Google sign-in and stores no site passwords.
- No telemetry; the only network requests are made by yt-dlp to the target site (and to the proxy, if enabled).

> ⚠️ DPAPI ties the password to the Windows user account. If you move a portable folder to another PC or another user, proxy passwords won't decrypt — re-enter them. All other settings carry over.

---

## Troubleshooting

| Message / symptom | Cause and fix |
|---|---|
| "Tools package is incomplete" / "Missing: …" | One of `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`, `deno.exe` is missing from `tools\`. Restore it from the archive. |
| "YouTube treats this connection as a bot" | See [Browser session](#browser-session-cookies): open the video in Firefox/Edge and retry. |
| "Firefox or Edge was not found" | No browser profile detected. Install Firefox or Edge and open the video there. |
| "Could not reach the site" | Network, DNS, proxy or SSL issue. Check your connection; try a proxy. |
| "Could not connect through the proxy" | Wrong host/port/credentials or the proxy is down. Click "Test" in proxy settings. |
| "Deno not found" | `tools\deno.exe` is missing or yt-dlp failed to solve YouTube's JS challenge. Update yt-dlp and Deno. |
| "FFmpeg is missing or failed to merge streams" | `ffmpeg.exe` / its DLLs are missing, or merging failed. Check `tools\`. |
| "Video is unavailable, removed, or restricted" | The video is removed, private, blocked for copyright or in your region. |
| Everything suddenly stopped downloading | Most likely YouTube changed its site. **Update `tools\yt-dlp.exe`** (see below). |
| Warning about the `data` folder | The portable copy sits in a protected location (`Program Files`, etc.). Move the folder. |

Details are always in the **log** at the bottom of the window: the full yt-dlp command (secrets masked) and its output.

### Updating yt-dlp

There is no auto-update. YouTube changes its site regularly, so if downloads start failing en masse, grab a fresh `yt-dlp.exe` from [yt-dlp releases](https://github.com/yt-dlp/yt-dlp/releases/latest) and replace the file in `tools\`. `deno.exe` and FFmpeg are updated the same way.

---

## Building from source

### Requirements

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PowerShell 5.1+ (for scripts)

### Steps

```powershell
git clone git@github.com:Evgeniy-51/yt-pump.git YtPump
cd YtPump

# Build and test
dotnet build YtPump.sln
dotnet test  YtPump.sln

# Download yt-dlp, Deno and FFmpeg (LGPL shared) into tools\
.\fetch-tools.ps1

# Run the debug build (tools\ is copied into bin automatically)
.\src\YtPump.App\bin\Debug\net8.0-windows\YtPump.exe

# Release package: dist\win-x64\ and dist\YtPump-<version>-win-x64.zip
.\pack.ps1
```

If script execution is blocked by policy: `powershell -ExecutionPolicy Bypass -File .\fetch-tools.ps1`.

### What the scripts do

**`fetch-tools.ps1`** — downloads the latest versions into `tools\`:
- `yt-dlp.exe` — from `yt-dlp/yt-dlp` releases;
- `deno.exe` — from `denoland/deno` releases (`x86_64-pc-windows-msvc`);
- `ffmpeg.exe`, `ffprobe.exe` and DLLs — **LGPL shared** build from `BtbN/FFmpeg-Builds`;
- writes `LICENSE-yt-dlp.txt`, `LICENSE-deno.txt`, copies the FFmpeg license.

The script does not verify checksums — check them manually if needed.

**`pack.ps1`** — runs `dotnet publish` with the `win-x64` profile (self-contained, single-file, compressed, no debug symbols), then adds `portable.flag`, `README.md`, `README_EN.md`, `LICENSE` and the `tools\` folder, and zips everything into `dist\YtPump-<version>-win-x64.zip`.

### What's excluded from git

`bin/`, `obj/`, `dist/`, `data/`, `*.zip`, `*.pdb`, and the `tools/*.exe` / `tools/*.dll` binaries — they're fetched by `fetch-tools.ps1`. Only `tools/README.txt` and license texts are tracked.

---

## Architecture

```text
YtPump.sln
└─ src\
   ├─ YtPump.Core\         # UI-free logic (net8.0-windows)
   ├─ YtPump.App\          # WPF app, MVVM (CommunityToolkit.Mvvm)
   └─ YtPump.Core.Tests\   # xUnit tests for Core
```

**YtPump.Core**

| Namespace | Purpose |
|---|---|
| `YtDlp` | Argument building (`CommandBuilder`), format selectors (`FormatSelector`), metadata JSON parsing (`MetadataParser`) and progress parsing (`ProgressParser`), error classification (`ErrorClassifier`), process launch (`YtDlpProcess`) inside a Job Object with `KILL_ON_JOB_CLOSE` |
| `Cookies` | Detecting installed browsers and their try order |
| `Proxy` | Proxy URL building, two-stage proxy test |
| `Settings` | `settings.json`, portable mode, proxy profiles, DPAPI, link history |
| `Tools` | Locating `tools\` next to the executable (via `Environment.ProcessPath`, not `AppContext.BaseDirectory` — matters for single-file) |
| `Validation` | URL and folder checks, file name sanitization |

**YtPump.App** — windows (`MainWindow`, `ProxySettingsWindow`, `HelpWindow`), view models, `.resx` localization (`Strings.resx` — Russian, the neutral language; `Strings.en.resx` — English), theme in `Themes\AppTheme.xaml`. Help text (the "Help" button) is embedded into the assembly as a resource from `src\YtPump.App\Help\*.md`.

<details>
<summary>Arguments passed to yt-dlp</summary>

Common to all calls:

```text
--ffmpeg-location <tools> --js-runtimes deno:<tools>\deno.exe
--windows-filenames --no-playlist --encoding utf-8 --newline
```

Probe: `--skip-download -J` (+ `--proxy`, `--cookies-from-browser`).

Download:

```text
-f <selector> --merge-output-format mkv|mp4       # or --extract-audio --audio-format m4a
--write-subs --write-auto-subs --sub-langs <lang>.* --convert-subs srt   # when subtitles are on
-P <folder> -o "<name> [%(id)s].%(ext)s" --force-overwrites
--progress-template ... --print after_move:YTPFILE:%(filepath)s
```

Format selectors (example for 1080p and a Russian audio track):

| Container | Selector |
|---|---|
| MKV | `bv*[height<=1080]+ba[language=ru]/bv*[height<=1080]+ba/b` |
| MP4 | `bv*[vcodec^=avc][height<=1080]+ba[acodec^=mp4a][language=ru]/…/b[ext=mp4]/b` |
| Audio | `ba[language=ru][ext=m4a]/ba[language=ru]/ba` |

</details>

---

## Licenses

YtPump source code is licensed under the [MIT License](LICENSE).

Third-party components shipped in `tools\`:

| Component | License |
|---|---|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Unlicense |
| [FFmpeg](https://ffmpeg.org/) ([BtbN builds](https://github.com/BtbN/FFmpeg-Builds)) | LGPL 2.1+ (the LGPL build without GPL components is used) |
| [Deno](https://github.com/denoland/deno) | MIT |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MIT |

License texts are in `tools\LICENSE-*.txt`. If you redistribute your own build, use the **LGPL** variant of FFmpeg.
