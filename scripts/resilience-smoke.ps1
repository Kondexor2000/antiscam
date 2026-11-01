param(
    [string]$BlogApiUrl = 'http://127.0.0.1:5000',
    [string]$PythonApiUrl = 'http://127.0.0.1:8000',
    [string]$ReportPath = 'artifacts/resilience-report.md'
)

$ErrorActionPreference = 'Stop'
if (-not [string]::IsNullOrWhiteSpace($env:BLOG_API_URL)) {
    $BlogApiUrl = $env:BLOG_API_URL
} elseif (-not [string]::IsNullOrWhiteSpace($env:BLOG_API_PORT)) {
    $BlogApiUrl = "http://127.0.0.1:$($env:BLOG_API_PORT)"
}
if (-not [string]::IsNullOrWhiteSpace($env:PYTHON_API_URL)) {
    $PythonApiUrl = $env:PYTHON_API_URL
} elseif (-not [string]::IsNullOrWhiteSpace($env:PYTHON_API_PORT)) {
    $PythonApiUrl = "http://127.0.0.1:$($env:PYTHON_API_PORT)"
}
if ($env:ANTISCAM_ALLOW_RESILIENCE_TESTS -ne 'YES') {
    throw 'Set ANTISCAM_ALLOW_RESILIENCE_TESTS=YES after confirming this is an isolated Compose test stack.'
}
if ([string]::IsNullOrWhiteSpace($env:MSSQL_SA_PASSWORD)) { throw 'Set MSSQL_SA_PASSWORD for the isolated Compose stack.' }
if ([string]::IsNullOrWhiteSpace($env:ANTISCAM_CONTROL_PLANE_KEY)) { throw 'Set ANTISCAM_CONTROL_PLANE_KEY (32+ bytes).' }

$reportDirectory = Split-Path -Parent $ReportPath
if ($reportDirectory) { New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null }
Set-Content -Path $ReportPath -Encoding utf8 -Value @(
    '# Raport testów odporności',
    '',
    "Rozpoczęto: $([DateTime]::UtcNow.ToString('o'))",
    '',
    '| Scenariusz | Wynik |',
    '|---|---|'
)

function Add-Result([string]$Scenario, [string]$Result) {
    Add-Content -Path $ReportPath -Encoding utf8 -Value "| $Scenario | $Result |"
}

function Get-HttpStatus([string]$Url) {
    try {
        return [int](Invoke-WebRequest -Uri $Url -TimeoutSec 5 -SkipHttpErrorCheck).StatusCode
    } catch {
        return 0
    }
}

function Wait-Http([string]$Url, [int]$ExpectedStatus) {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $status = Get-HttpStatus $Url
        if ($status -eq $ExpectedStatus) { return }
        Start-Sleep -Seconds 2
    }
    throw "Timed out waiting for $Url to return $ExpectedStatus (last status: $status)."
}

function Invoke-Compose([string[]]$Arguments) {
    & docker compose @Arguments
    if ($LASTEXITCODE -ne 0) { throw "docker compose $($Arguments -join ' ') failed with exit code $LASTEXITCODE." }
}

function Invoke-Sql([string]$Query, [string]$Database = 'master') {
    Invoke-Compose @('exec', '-T', 'sqlserver', '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', 'sa', '-P', $env:MSSQL_SA_PASSWORD, '-C', '-b', '-d', $Database, '-Q', $Query)
}

function Get-SqlScalar([string]$Query) {
    $output = & docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $env:MSSQL_SA_PASSWORD -C -b -d AntiScamBlog -h -1 -W -Q $Query
    if ($LASTEXITCODE -ne 0) { throw "SQL scalar query failed with exit code $LASTEXITCODE." }
    return ($output | Out-String).Trim()
}

$servicesMayNeedRecovery = $false
try {
    Wait-Http "$BlogApiUrl/api/health" 200
    Wait-Http "$PythonApiUrl/" 200
    $slug = "resilience-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())-$([Guid]::NewGuid().ToString('N').Substring(0,8))"
    $post = @{
        title = "Resilience check $slug"
        summary = 'Isolated resilience test'
        content = 'Neutral test content for backup and restore verification.'
        author = 'QA'
    } | ConvertTo-Json
    $created = Invoke-RestMethod -Uri "$BlogApiUrl/api/posts" -Method Post -ContentType 'application/json' -Body $post
    $slug = $created.slug
    $storedCount = Get-SqlScalar "SET NOCOUNT ON; SELECT COUNT_BIG(1) FROM dbo.Posts WHERE Slug=N'$slug'"
    Write-Host "Control post SQL verification: id='$($created.id)', slug='$slug', row count='$storedCount'"
    if ([int64]$storedCount -lt 1) {
        throw "Control post '$slug' was not persisted and is not present in the blog listing."
    }
    Add-Result 'Uruchomienie usług i zapis wpisu kontrolnego' "OK ($slug)"

    Invoke-Sql "BACKUP DATABASE [AntiScamBlog] TO DISK=N'/var/opt/mssql/data/antiscam-resilience.bak' WITH INIT"
    Add-Result 'Utworzenie backupu SQL Server' 'OK'

    Invoke-Compose @('stop', 'sqlserver')
    $servicesMayNeedRecovery = $true
    $status = 0
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $status = Get-HttpStatus "$BlogApiUrl/api/posts/$slug"
        if ($status -ge 500) { break }
        Start-Sleep -Seconds 2
    }
    if ($status -lt 500) { Add-Result 'Wyłączenie SQL Server' "FAIL (HTTP $status)"; throw 'SQL Server outage did not surface as an API error.' }
    Add-Result 'Wyłączenie SQL Server' "OK (API zwróciło $status)"

    Invoke-Compose @('start', 'sqlserver')
    $sqlReady = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try { Invoke-Sql 'SELECT 1' | Out-Null; $sqlReady = $true; break } catch { Start-Sleep -Seconds 2 }
    }
    if (-not $sqlReady) { throw 'Timed out waiting for SQL Server readiness.' }
    Invoke-Compose @('restart', 'blog-api')
    Wait-Http "$BlogApiUrl/api/health" 200
    Invoke-RestMethod -Uri "$BlogApiUrl/api/posts/$slug" | Out-Null
    Add-Result 'Restart SQL Server/API i zachowanie danych na wolumenie' 'OK'

    Invoke-Compose @('stop', 'python-api')
    $status = 0
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $status = Get-HttpStatus "$PythonApiUrl/"
        if ($status -eq 0) { break }
        Start-Sleep -Seconds 1
    }
    if ($status -ne 0) { Add-Result 'Wyłączenie silnika Python' "FAIL (HTTP $status)"; throw 'Python API remained reachable after stop.' }
    Invoke-Compose @('start', 'python-api')
    Wait-Http "$PythonApiUrl/" 200
    Add-Result 'Wyłączenie i restart silnika Python' 'OK'

    Invoke-Sql "DELETE FROM dbo.Posts WHERE Slug=N'$slug'" -Database 'AntiScamBlog'
    Invoke-Compose @('stop', 'blog-api')
    Invoke-Sql "ALTER DATABASE [AntiScamBlog] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [AntiScamBlog] FROM DISK=N'/var/opt/mssql/data/antiscam-resilience.bak' WITH REPLACE; ALTER DATABASE [AntiScamBlog] SET MULTI_USER"
    Invoke-Compose @('up', '-d', 'blog-api')
    Wait-Http "$BlogApiUrl/api/health" 200
    Invoke-RestMethod -Uri "$BlogApiUrl/api/posts/$slug" | Out-Null
    Add-Result 'Odtworzenie backupu i odzyskanie wpisu kontrolnego' 'OK'
    Add-Content -Path $ReportPath -Encoding utf8 -Value "`nZakończono: $([DateTime]::UtcNow.ToString('o'))"
    $servicesMayNeedRecovery = $false
} finally {
    if ($servicesMayNeedRecovery) {
        & docker compose start sqlserver python-api 2>$null | Out-Null
        & docker compose up -d blog-api 2>$null | Out-Null
    }
}
