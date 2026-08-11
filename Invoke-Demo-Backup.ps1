[CmdletBinding()]
param(
    [int]$CSharpPort = 5000,
    [switch]$KeepArtifacts
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSCommandPath

# Niektore hosty PowerShell przekazuja jednoczesnie PATH i Path, czego .NET nie
# akceptuje przy Start-Process.
$processPath = [Environment]::GetEnvironmentVariable('Path', 'Process')
if (![string]::IsNullOrWhiteSpace($processPath)) {
    [Environment]::SetEnvironmentVariable('PATH', $null, 'Process')
    [Environment]::SetEnvironmentVariable('Path', $processPath, 'Process')
    $env:Path = $processPath
}

function Assert-Equal {
    param([object]$Actual, [object]$Expected, [string]$Message)
    if ($Actual -ne $Expected) { throw "$Message Otrzymano: $Actual; oczekiwano: $Expected." }
}

function Wait-ForApi {
    param([string]$Uri, [System.Diagnostics.Process]$Process)
    $deadline = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 500
        try {
            Invoke-RestMethod -Uri $Uri -ErrorAction Stop | Out-Null
            return
        } catch {
            if ($Process.HasExited) { throw 'API C# zakonczyl dzialanie przed uruchomieniem.' }
        }
    } while ((Get-Date) -lt $deadline)
    throw "API C# nie odpowiada pod adresem $Uri w ciagu 30 sekund."
}

$existing = Get-NetTCPConnection -LocalPort $CSharpPort -State Listen -ErrorAction SilentlyContinue
if ($null -ne $existing) { throw "Port $CSharpPort jest juz zajety. Zatrzymaj istniejacy serwer albo uzyj -CSharpPort." }
if ($null -eq (Get-Command curl.exe -ErrorAction SilentlyContinue)) { throw 'Brak curl.exe, wymaganego do ustawienia adresu klienta w demonstracji backupu.' }

$id = [guid]::NewGuid().ToString('N')
$databasePath = Join-Path $env:TEMP "antiscam-backup-demo-$id.sqlite"
$artifactDirectory = Join-Path $env:TEMP "antiscam-backup-demo-$id"
$bodyPath = Join-Path $env:TEMP "antiscam-backup-login-$id.json"
$logDirectory = Join-Path $env:TEMP "antiscam-backup-demo-logs-$id"
$process = $null
New-Item -ItemType Directory -Path $artifactDirectory, $logDirectory | Out-Null
Set-Content -LiteralPath $bodyPath -Value '{"userName":"backup-demo","password":"StrongPassword123!"}' -NoNewline

$previousDatabase = $env:ANTISCAM_BLOG_DB
$env:ANTISCAM_BLOG_DB = $databasePath
try {
    $keyPath = Join-Path $artifactDirectory 'backup.key'
    $process = Start-Process dotnet -ArgumentList @(
        'run', '--no-restore', '--project', 'src\AntiScam.Blog.Api\AntiScam.Blog.Api.csproj', '--',
        "--Network:HttpPort=$CSharpPort", '--Network:BindToLan=true',
        "--Backup:DirectoryPath=$artifactDirectory", "--Backup:KeyFilePath=$keyPath"
    ) -WorkingDirectory $repoRoot -PassThru -WindowStyle Hidden `
      -RedirectStandardOutput (Join-Path $logDirectory 'csharp.out.log') `
      -RedirectStandardError (Join-Path $logDirectory 'csharp.err.log')
    Wait-ForApi -Uri "http://127.0.0.1:$CSharpPort/api/health" -Process $process

    $registerUri = "http://127.0.0.1:$CSharpPort/api/auth/register"
    $loginUri = "http://127.0.0.1:$CSharpPort/api/auth/login"
    $headers = @('-H', 'Content-Type: application/json', '--data-binary', "@$bodyPath")

    $registration = & curl.exe --interface 127.0.0.2 -s -o NUL -w '%{http_code}' @headers $registerUri
    Assert-Equal $registration '201' 'Rejestracja konta demonstracyjnego nie powiodla sie.'

    $firstLogin = & curl.exe --interface 127.0.0.2 -s -o NUL -w '%{http_code}' @headers $loginUri
    Assert-Equal $firstLogin '200' 'Pierwsze logowanie nie powiodlo sie.'

    $backupPath = Join-Path $artifactDirectory 'backup.enc.json'
    $metadataPath = Join-Path $artifactDirectory 'backup_meta.json'
    if ((Test-Path $backupPath) -or (Test-Path $metadataPath)) {
        throw 'Backup zostal utworzony juz po pierwszym logowaniu z tego samego IP.'
    }

    $secondLogin = & curl.exe --interface 127.0.0.3 -s -o NUL -w '%{http_code}' @headers $loginUri
    Assert-Equal $secondLogin '200' 'Drugie logowanie nie powiodlo sie.'
    if (!(Test-Path $backupPath) -or !(Test-Path $metadataPath)) {
        throw 'Nie utworzono plikow backupu po logowaniu z innego IP.'
    }

    $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
    Assert-Equal $metadata.algorithm 'AES-GCM-256' 'Backup nie ma oczekiwanego algorytmu szyfrowania.'
    $payload = Get-Content -LiteralPath $backupPath -Raw | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($payload.cipherText) -or [string]::IsNullOrWhiteSpace($payload.tag)) {
        throw 'Plik backupu nie zawiera zaszyfrowanego ladunku AES-GCM.'
    }

    Write-Host 'Demo 9: backup po zmianie IP - OK' -ForegroundColor Green
    Write-Host 'Pierwsze IP: 127.0.0.2; drugie IP: 127.0.0.3; algorytm: AES-GCM-256.'
    if ($KeepArtifacts) { Write-Host "Pozostawiono artefakty: $artifactDirectory" }
} finally {
    $env:ANTISCAM_BLOG_DB = $previousDatabase
    if ($null -ne $process -and !$process.HasExited -and !$KeepArtifacts) { Stop-Process -Id $process.Id -Force }
    if (!$KeepArtifacts) {
        Remove-Item -LiteralPath $databasePath, $artifactDirectory, $bodyPath, $logDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}
