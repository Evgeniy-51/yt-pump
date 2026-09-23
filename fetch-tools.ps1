$ErrorActionPreference = 'Stop'
$tools = Join-Path $PSScriptRoot 'tools'
New-Item -ItemType Directory -Force -Path $tools | Out-Null
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("ytpump-tools-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

function Get-GithubFile([string]$Uri, [string]$OutFile) {
    Write-Host "Downloading $Uri"
    Invoke-WebRequest -Uri $Uri -OutFile $OutFile -UseBasicParsing
}

try {
    Get-GithubFile 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' (Join-Path $tools 'yt-dlp.exe')

    $denoZip = Join-Path $tmp 'deno.zip'
    Get-GithubFile 'https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip' $denoZip
    Expand-Archive -Path $denoZip -DestinationPath $tmp -Force
    Copy-Item (Join-Path $tmp 'deno.exe') (Join-Path $tools 'deno.exe') -Force

    $ffZip = Join-Path $tmp 'ffmpeg.zip'
    $ffUrl = 'https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl-shared.zip'
    Get-GithubFile $ffUrl $ffZip
    Expand-Archive -Path $ffZip -DestinationPath $tmp -Force
    $ffRoot = Get-ChildItem $tmp -Directory | Where-Object { $_.Name -like 'ffmpeg-*' } | Select-Object -First 1
    if (-not $ffRoot) {
        throw "FFmpeg archive layout unexpected"
    }
    Copy-Item (Join-Path $ffRoot.FullName 'bin\ffmpeg.exe') (Join-Path $tools 'ffmpeg.exe') -Force
    Copy-Item (Join-Path $ffRoot.FullName 'bin\ffprobe.exe') (Join-Path $tools 'ffprobe.exe') -Force
    Get-ChildItem (Join-Path $ffRoot.FullName 'bin\*.dll') -ErrorAction SilentlyContinue | Copy-Item -Destination $tools -Force
    $ffLicense = Get-ChildItem $ffRoot.FullName -Filter 'LICENSE*' -Recurse | Select-Object -First 1
    if ($ffLicense) {
        Copy-Item $ffLicense.FullName (Join-Path $tools 'LICENSE-FFmpeg.txt') -Force
    }

    @'
yt-dlp is Unlicense / public domain.
https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE
'@ | Set-Content -Path (Join-Path $tools 'LICENSE-yt-dlp.txt') -Encoding UTF8

    @'
Deno is MIT.
https://github.com/denoland/deno/blob/main/LICENSE.md
'@ | Set-Content -Path (Join-Path $tools 'LICENSE-deno.txt') -Encoding UTF8

    Write-Host "Tools installed in $tools"
    Get-ChildItem $tools | Select-Object Name, Length
}
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
