$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path -Parent $PSScriptRoot
$taskOutput = Join-Path $taskRepo 'artifacts\verification\current'

Push-Location $taskRepo
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Velopack tool restore failed.' }

    dotnet build verification\WindowBehaviorChecks.csproj -c Release -o $taskOutput -p:PublishSingleFile=false -p:SelfContained=true -p:SkipNativeChatPackaging=true -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Verification build failed.' }

    # Match the normal portable layout so all settings and web data stay here.
    $taskNugetCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget\packages' }
    $taskToolVersion = (Get-Content -LiteralPath .config\dotnet-tools.json -Raw | ConvertFrom-Json).tools.vpk.version
    Copy-Item -LiteralPath (Join-Path $taskNugetCache "vpk\$taskToolVersion\vendor\update.exe") -Destination (Join-Path (Split-Path -Parent $taskOutput) 'Update.exe')
    New-Item -ItemType File -Path (Join-Path (Split-Path -Parent $taskOutput) '.portable') -Force | Out-Null
    @'
<?xml version="1.0" encoding="utf-8"?>
<package><metadata><id>WindowBehaviorChecks</id><title>Window behavior checks</title><version>1.1.8</version><channel>win</channel><mainExe>WindowBehaviorChecks.exe</mainExe><os>win</os><rid>win</rid></metadata></package>
'@ | Set-Content -LiteralPath (Join-Path $taskOutput 'sq.version')

    & (Join-Path $taskOutput 'WindowBehaviorChecks.exe') | Tee-Object -FilePath (Join-Path $taskRepo 'artifacts\verification-results.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Window behavior checks failed.' }
}
finally {
    Pop-Location
}
