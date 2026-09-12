param(
    [Parameter()]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version = '1.2.0'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$releaseDirectory = Join-Path $artifactRoot "EveJFTool-v$Version-win-x64"
$publishDirectory = Join-Path $releaseDirectory 'publish'
$archivePath = Join-Path $artifactRoot "EveJFTool-v$Version-win-x64.zip"
$checksumPath = "$archivePath.sha256"

if (-not $artifactRoot.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The artifact directory resolved outside the repository.'
}

if (Test-Path -LiteralPath $releaseDirectory) {
    Remove-Item -LiteralPath $releaseDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

if (Test-Path -LiteralPath $checksumPath) {
    Remove-Item -LiteralPath $checksumPath -Force
}

dotnet publish (Join-Path $repositoryRoot 'src\EveJFTool.App\EveJFTool.App.csproj') `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumPath -Value "$hash  $(Split-Path $archivePath -Leaf)" -Encoding ascii

Write-Output $archivePath
Write-Output $checksumPath
