$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $projectRoot '.env'
if (Test-Path -LiteralPath $envFile) {
    Write-Host 'Mevcut .env korundu. Docker Compose var olan ayarlarla calisacak.'
    exit 0
}
function New-DemoSecret {
    $buffer = New-Object byte[] 48
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($buffer) } finally { $generator.Dispose() }
    return [Convert]::ToBase64String($buffer)
}
$privateText = @(
    ('UDH_POSTGRES_PASSWORD=' + (New-DemoSecret))
    ('UDH_JWT_KEY=' + (New-DemoSecret))
    'UDH_HTTP_PORT=5160'
) -join [Environment]::NewLine
[System.IO.File]::WriteAllText($envFile, $privateText + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
Write-Host 'Ozel .env olusturuldu. Bu dosyayi Git veya teslim ZIP dosyasina eklemeyin.'
