# YtPump — User Guide

## About the Program

YtPump does not open websites or interact with YouTube directly — downloading is handled by yt-dlp, video and audio streams are merged by FFmpeg, and YouTube's signature verification is handled by Deno. The program does not use Google sign-in and does not store passwords.

## Browser Session

YouTube sometimes detects requests from applications as automated and refuses to serve the video. In this case, the program needs an active session from a regular browser — not a login and password, but the site's cookies.

To resolve this:

1. Open the same video in Firefox or Edge as a regular web page.
2. Refresh the page to make sure the session is active.
3. Paste the link again or restart the download in YtPump.

You don't need to close the browser — YtPump reads only YouTube cookies and does not receive logins, passwords, or browsing history.

**Use Firefox or Edge.** Current versions of Chrome encrypt cookies so that third-party programs, including YtPump and yt-dlp, usually cannot decrypt them. Opening the video in Chrome therefore usually does not help — even if you are signed in to a Google account.

If Firefox or Edge is already installed, open the video there, refresh the page, and try the download again. This problem is not related to your internet connection or proxy: YouTube requires confirmation through a normal browser session.

## Workflow

1. **Link** — paste the video link; the program will automatically fetch the available video resolutions and audio tracks.
2. **Choose parameters**: quality, audio track, and container format.
   - MKV preserves the original streams without re-encoding.
   - MP4 is more convenient for playback on most players and devices.
   - "Audio only" mode saves the sound without the video track.
3. **Set the output folder** using the button next to the path field. The file name can be adjusted after a trial run.
4. **Subtitles**: if subtitles are available for the video, they can be saved as a separate `.srt` file. To do this, check the box and select a language.
5. **Proxy** is enabled in the top panel. A host and port must be specified — without them, the connection won't work. SOCKS5H is used by default, meaning DNS queries are also routed through the proxy.
6. **Playlists are not supported** — only a single video is downloaded from the link, even if the link points to a playlist.
