param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Split-Path -Parent $PSScriptRoot)).Path
if (-not $OutputPath) {
    $OutputPath = Join-Path $projectRoot 'private-data/UniversityDecisionHub-DockerDemo-2026-11-19.zip'
}
$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $OutputPath) { throw 'Teslim ZIP dosyasi zaten var; yeni bir OutputPath belirtin.' }
$exactFiles = @('Dockerfile', '.dockerignore', '.env.example', 'compose.yaml',
    'Scripts/Initialize-DockerDemo.ps1', 'Scripts/Export-DockerDemo.ps1',
    'Doc/Docker_Demo_Kurulum.md', 'DataBase/Imports/catalog.ready.json')
$files = @()
foreach ($relative in $exactFiles) { $files += Get-Item -LiteralPath (Join-Path $projectRoot $relative) }
$files += Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Backend/UniversityDecisionHub.Api') -Recurse -File |
    Where-Object { $_.Extension -in @('.cs', '.csproj') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$files += Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Frontend') -Recurse -File |
    Where-Object { $_.Name -notmatch '^\.env' -and $_.Name -ne 'secrets.json' -and $_.FullName -notmatch '[\\/](bin|obj|node_modules|private-data|\.git)[\\/]' }
$files = @($files | Sort-Object FullName -Unique)
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Directory]::CreateDirectory((Split-Path -Parent $OutputPath)) | Out-Null
$stream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::CreateNew)
$zip = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName,
            ('UniversityDecisionHub-DockerDemo/' + $relative),
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose(); $stream.Dispose() }
$hash = (Get-FileHash -LiteralPath $OutputPath -Algorithm SHA256).Hash
[System.IO.File]::WriteAllText(($OutputPath + '.sha256'), $hash + [Environment]::NewLine)
Write-Host ('Teslim paketi hazir: ' + $OutputPath)
Write-Host ('SHA256: ' + $hash)
