param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$taskPublish = Join-Path $PSScriptRoot "artifacts\publish-$Version"
$taskPackages = Join-Path $PSScriptRoot "artifacts\packages-$Version"

Push-Location $PSScriptRoot
try {
    Write-Host 'Building NativeChat...'
    Push-Location NativeChatClient
    try {
        npm.cmd ci --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }
        npm.cmd run build
        if ($LASTEXITCODE -ne 0) { throw 'NativeChat build failed.' }
    }
    finally { Pop-Location }

    Write-Host 'Publishing Windows x64 app...'
    dotnet publish TransparentTwitchChatWPF\TransparentTwitchChatWPF.csproj -c Release -o $taskPublish -r win-x64 --self-contained true "-p:Version=$Version" "-p:InformationalVersion=$Version"
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

    Write-Host 'Checking window behavior and settings...'
    & .\verification\run.ps1

    Copy-Item -LiteralPath LICENSE -Destination (Join-Path $taskPublish 'LICENSE.txt')
    Copy-Item -LiteralPath CUSTOM_BUILD.md -Destination (Join-Path $taskPublish 'BUILD-NOTES.md')

    Write-Host 'Packaging installer and portable app...'
    dotnet tool run vpk -- pack --packId TransparentTwitchChatAlwaysOnTop --packVersion $Version --packTitle 'Twitch Chat - Always on top option' --packDir $taskPublish --mainExe TransparentTwitchChatWPF.exe --delta None -o $taskPackages
    if ($LASTEXITCODE -ne 0) { throw 'Velopack packaging failed.' }

    $taskAssets = Get-ChildItem -LiteralPath $taskPackages -File | Where-Object Name -ne 'SHA256SUMS.txt' | Sort-Object Name
    $taskAssets | ForEach-Object {
        $taskHash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$taskHash  $($_.Name)"
    } | Set-Content -LiteralPath (Join-Path $taskPackages 'SHA256SUMS.txt') -Encoding ascii

    Write-Host "Release $Version is ready in $taskPackages"
}
finally { Pop-Location }
