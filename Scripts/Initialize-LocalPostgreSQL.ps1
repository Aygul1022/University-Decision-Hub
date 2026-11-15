# Run interactively in Administrator PowerShell. No passwords are stored here.
# This local recovery setup preserves the old cluster and does not run EF migrations.
$ErrorActionPreference = 'Stop'
$taskIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$taskPrincipal = [Security.Principal.WindowsPrincipal]::new($taskIdentity)
if (-not $taskPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Open PowerShell with Run as administrator, then run this script again.'
}

$serviceName = 'postgresql-x64-18'
$pgBin = 'C:\Program Files\PostgreSQL\18\bin'
$newData = 'C:\PostgreSQL\18\data'
$service = Get-CimInstance Win32_Service -Filter "Name='$serviceName'"
if (-not $service -or $service.State -ne 'Stopped') {
    throw 'Expected the existing PostgreSQL 18 service to be stopped. No setup performed.'
}
if ($service.StartName -notmatch '^(NT AUTHORITY\\NetworkService|NT AUTHORITY\\NETWORK SERVICE)$') {
    throw 'Unexpected service account. Review permissions before continuing.'
}
if (Test-Path -LiteralPath $newData) {
    throw "Target already exists: $newData. It will not be overwritten. Ask for a review before retrying."
}
foreach ($exe in @('initdb.exe', 'pg_ctl.exe', 'createdb.exe', 'psql.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $pgBin $exe))) { throw "Missing $exe" }
}
if (Get-NetTCPConnection -LocalPort 5432 -State Listen -ErrorAction SilentlyContinue) {
    throw 'Port 5432 is already in use. No setup performed.'
}

$oldServicePath = $service.PathName
New-Item -ItemType Directory -Path $newData -Force | Out-Null
# Limit the new cluster to its owner, administrators, SYSTEM and the service account.
& icacls.exe $newData /inheritance:r /grant:r "*$($taskIdentity.User.Value):(OI)(CI)F" '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-18:(OI)(CI)F' '*S-1-5-20:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Failed to secure the new data directory.' }

Write-Host 'Choose a new postgres password when prompted. Keep it private; you will enter it again below.'
& "$pgBin\initdb.exe" -D $newData -U postgres --encoding=UTF8 --locale=C --auth=scram-sha-256 -W
if ($LASTEXITCODE -ne 0) { throw 'Initialization failed. The old service configuration is unchanged; do not delete any files.' }
@"

# UniversityDecisionHub local development cluster
listen_addresses = 'localhost'
port = 5432
logging_collector = on
log_directory = 'log'
"@ | Add-Content -LiteralPath "$newData\postgresql.conf" -Encoding ASCII

$newServicePath = '"' + $pgBin + '\pg_ctl.exe" runservice -N "' + $serviceName + '" -D "' + $newData + '" -w'
$changed = Invoke-CimMethod -InputObject $service -MethodName Change -Arguments @{ PathName = $newServicePath }
if ($changed.ReturnValue -ne 0) { throw "Service configuration failed: $($changed.ReturnValue)" }
try {
    Start-Service -Name $serviceName
    (Get-Service -Name $serviceName).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
} catch {
    $current = Get-CimInstance Win32_Service -Filter "Name='$serviceName'"
    $rollback = Invoke-CimMethod -InputObject $current -MethodName Change -Arguments @{ PathName = $oldServicePath }
    Write-Warning "Startup failed. Service path restoration result: $($rollback.ReturnValue). Both data directories are preserved."
    throw
}

Write-Host 'Enter the same new password to create the empty UniversityDecisionHub database.'
& "$pgBin\createdb.exe" -h 127.0.0.1 -p 5432 -U postgres -W UniversityDecisionHub
if ($LASTEXITCODE -ne 0) { throw 'Service is running, but database creation failed. Do not rerun initialization; report this output.' }
Write-Host 'Enter the same password once more to verify the connection and empty database.'
& "$pgBin\psql.exe" -X -h 127.0.0.1 -p 5432 -U postgres -W -d UniversityDecisionHub -v ON_ERROR_STOP=1 -c "SELECT current_database(), current_user; SELECT count(*) AS public_tables FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';"
if ($LASTEXITCODE -ne 0) { throw 'Database was created, but connection verification failed. Report the error; do not rerun initialization.' }
Write-Host 'Setup completed. Expected public_tables: 0. No EF migrations were applied.'
Write-Host 'In pgAdmin use 127.0.0.1, port 5432, user postgres and your new password.'
