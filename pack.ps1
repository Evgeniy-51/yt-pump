$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

$project = Join-Path $root 'src\YtPump.App\YtPump.App.csproj'
$version = (Select-Xml -Path $project -XPath '//Version').Node.InnerText
if (-not $version) { throw "Version not found in $project" }

dotnet publish $project -c Release -p:PublishProfile=win-x64
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

$dist = Join-Path $root 'dist\win-x64'
$exe = Join-Path $dist 'YtPump.exe'
if (-not (Test-Path $exe)) {
    throw "publish did not produce $exe"
}
Copy-Item (Join-Path $root 'portable.flag') $dist -Force
Copy-Item (Join-Path $root 'README.md') $dist -Force
Copy-Item (Join-Path $root 'README_EN.md') $dist -Force
Copy-Item (Join-Path $root 'LICENSE') $dist -Force
$toolsDst = Join-Path $dist 'tools'
if (Test-Path $toolsDst) {
    Remove-Item $toolsDst -Recurse -Force
}
Copy-Item (Join-Path $root 'tools') $toolsDst -Recurse -Force

Get-ChildItem $dist -Recurse -Include *.pdb | Remove-Item -Force

$zip = Join-Path $root "dist\YtPump-$version-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip
Write-Host "Packed $zip"
