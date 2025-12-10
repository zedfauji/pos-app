# PostgreSQL Dump Verification Script
# Compares dump file with live database to ensure completeness

param(
    [string]$DumpFile,
    [string]$DbHost = "34.51.82.201",
    [string]$DbPort = "5432",
    [string]$DbUser = "posapp",
    [string]$DbName = "postgres"
)

$ErrorActionPreference = "Stop"

# Auto-detect latest dump if not specified
if (-not $DumpFile) {
    $latestDump = Get-ChildItem "dumps\postgres-dump-*.sql" -ErrorAction SilentlyContinue | 
                  Sort-Object LastWriteTime -Descending | 
                  Select-Object -First 1
    if ($latestDump) {
        $DumpFile = $latestDump.FullName
        Write-Host "Using latest dump: $($latestDump.Name)" -ForegroundColor Yellow
    } else {
        Write-Host "Error: No dump file found. Specify -DumpFile parameter." -ForegroundColor Red
        exit 1
    }
}

if (-not (Test-Path $DumpFile)) {
    Write-Host "Error: Dump file not found: $DumpFile" -ForegroundColor Red
    exit 1
}

Write-Host "=== PostgreSQL Dump Verification ===" -ForegroundColor Cyan
Write-Host "Dump file: $DumpFile" -ForegroundColor Yellow
Write-Host "File size: $([math]::Round((Get-Item $DumpFile).Length / 1MB, 2)) MB" -ForegroundColor Yellow
Write-Host ""

# Function to execute SQL query
function Invoke-PostgresQuery {
    param([string]$Query)
    $env:PGPASSWORD = "Campus_66"
    $result = psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -A -c $Query 2>&1
    return $result
}

# 1. Extract schemas from dump
Write-Host "1. Extracting schemas from dump..." -ForegroundColor Cyan
$dumpSchemas = @()
Select-String -Path $DumpFile -Pattern 'CREATE SCHEMA "([^"]+)"' | ForEach-Object {
    if ($_.Line -match 'CREATE SCHEMA "([^"]+)"') {
        $dumpSchemas += $matches[1]
    }
}
$dumpSchemas = $dumpSchemas | Sort-Object -Unique
Write-Host "   Found $($dumpSchemas.Count) schemas in dump:" -ForegroundColor Green
$dumpSchemas | ForEach-Object { Write-Host "     - $_" -ForegroundColor Gray }

# 2. Get schemas from live database
Write-Host "`n2. Querying live database for schemas..." -ForegroundColor Cyan
try {
    $query = "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast') ORDER BY schema_name;"
    $liveSchemas = (Invoke-PostgresQuery -Query $query) -split "`n" | Where-Object { $_.Trim() -ne "" } | Sort-Object
    Write-Host "   Found $($liveSchemas.Count) schemas in database:" -ForegroundColor Green
    $liveSchemas | ForEach-Object { Write-Host "     - $_" -ForegroundColor Gray }
} catch {
    Write-Host "   Warning: Could not query live database. Skipping live comparison." -ForegroundColor Yellow
    $liveSchemas = @()
}

# 3. Extract tables from dump
Write-Host "`n3. Extracting tables from dump..." -ForegroundColor Cyan
$dumpTables = @()
Select-String -Path $DumpFile -Pattern 'CREATE TABLE (?:[^"]+"\.)?([^"]+)"\.([^"]+)"' | ForEach-Object {
    if ($_.Line -match 'CREATE TABLE (?:[^"]+"\.)?([^"]+)"\.([^"]+)"') {
        $schema = $matches[1]
        $table = $matches[2]
        if ($schema -eq "public") {
            $dumpTables += "public.$table"
        } else {
            $dumpTables += "$schema.$table"
        }
    }
}
$dumpTables = $dumpTables | Sort-Object -Unique
Write-Host "   Found $($dumpTables.Count) tables in dump" -ForegroundColor Green

# 4. Get tables from live database
Write-Host "`n4. Querying live database for tables..." -ForegroundColor Cyan
$liveTables = @()
if ($liveSchemas.Count -gt 0) {
    try {
        foreach ($schema in $liveSchemas) {
            $query = "SELECT table_schema || '.' || table_name FROM information_schema.tables WHERE table_schema = '$schema' AND table_type = 'BASE TABLE' ORDER BY table_schema, table_name;"
            $schemaTables = (Invoke-PostgresQuery -Query $query) -split "`n" | Where-Object { $_.Trim() -ne "" }
            $liveTables += $schemaTables
        }
        $liveTables = $liveTables | Sort-Object
        Write-Host "   Found $($liveTables.Count) tables in database" -ForegroundColor Green
    } catch {
        Write-Host "   Warning: Could not query tables from live database." -ForegroundColor Yellow
    }
}

# 5. Compare schemas
Write-Host "`n=== SCHEMA COMPARISON ===" -ForegroundColor Cyan
if ($liveSchemas.Count -gt 0) {
    $missingSchemas = $liveSchemas | Where-Object { $dumpSchemas -notcontains $_ }
    $extraSchemas = $dumpSchemas | Where-Object { $liveSchemas -notcontains $_ }
    
    if ($missingSchemas.Count -eq 0 -and $extraSchemas.Count -eq 0) {
        Write-Host "✓ All schemas match!" -ForegroundColor Green
    } else {
        if ($missingSchemas.Count -gt 0) {
            Write-Host "✗ Missing schemas in dump:" -ForegroundColor Red
            $missingSchemas | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        }
        if ($extraSchemas.Count -gt 0) {
            Write-Host "⚠ Extra schemas in dump (may be expected):" -ForegroundColor Yellow
            $extraSchemas | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
        }
    }
}

# 6. Compare tables
Write-Host "`n=== TABLE COMPARISON ===" -ForegroundColor Cyan
if ($liveTables.Count -gt 0) {
    $missingTables = $liveTables | Where-Object { $dumpTables -notcontains $_ }
    $extraTables = $dumpTables | Where-Object { $liveTables -notcontains $_ }
    
    if ($missingTables.Count -eq 0 -and $extraTables.Count -eq 0) {
        Write-Host "✓ All tables match!" -ForegroundColor Green
    } else {
        if ($missingTables.Count -gt 0) {
            Write-Host "✗ Missing tables in dump:" -ForegroundColor Red
            $missingTables | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        }
        if ($extraTables.Count -gt 0) {
            Write-Host "⚠ Extra tables in dump (may be expected):" -ForegroundColor Yellow
            $extraTables | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
        }
    }
}

# 7. Check for data in dump (COPY or INSERT statements)
Write-Host "`n=== DATA VERIFICATION ===" -ForegroundColor Cyan
$copyCount = (Select-String -Path $DumpFile -Pattern "COPY.*FROM stdin" | Measure-Object).Count
$insertCount = (Select-String -Path $DumpFile -Pattern "^INSERT INTO" | Measure-Object).Count

Write-Host "   Tables with data (COPY): $copyCount" -ForegroundColor Green
Write-Host "   Tables with data (INSERT): $insertCount" -ForegroundColor Green

# 8. Check for functions, triggers, indexes
Write-Host "`n=== OBJECT TYPE VERIFICATION ===" -ForegroundColor Cyan
$functionCount = (Select-String -Path $DumpFile -Pattern "CREATE FUNCTION" | Measure-Object).Count
$triggerCount = (Select-String -Path $DumpFile -Pattern "CREATE TRIGGER" | Measure-Object).Count
$indexCount = (Select-String -Path $DumpFile -Pattern "CREATE (?:UNIQUE )?INDEX" | Measure-Object).Count
$constraintCount = (Select-String -Path $DumpFile -Pattern "CONSTRAINT.*PRIMARY KEY|CONSTRAINT.*FOREIGN KEY" | Measure-Object).Count
$sequenceCount = (Select-String -Path $DumpFile -Pattern "CREATE SEQUENCE" | Measure-Object).Count

Write-Host "   Functions: $functionCount" -ForegroundColor Green
Write-Host "   Triggers: $triggerCount" -ForegroundColor Green
Write-Host "   Indexes: $indexCount" -ForegroundColor Green
Write-Host "   Constraints: $constraintCount" -ForegroundColor Green
Write-Host "   Sequences: $sequenceCount" -ForegroundColor Green

# 9. Sample row counts from live DB for verification
Write-Host "`n=== SAMPLE TABLE ROW COUNTS (Live DB) ===" -ForegroundColor Cyan
if ($liveTables.Count -gt 0) {
    $sampleTables = $liveTables | Select-Object -First 10
    foreach ($table in $sampleTables) {
        try {
            $query = "SELECT COUNT(*) FROM $table;"
            $count = (Invoke-PostgresQuery -Query $query).Trim()
            Write-Host "   $table : $count rows" -ForegroundColor Gray
        } catch {
            Write-Host "   $table : (error querying)" -ForegroundColor Yellow
        }
    }
    if ($liveTables.Count -gt 10) {
        Write-Host "   ... and $($liveTables.Count - 10) more tables" -ForegroundColor Gray
    }
}

# Summary
Write-Host "`n=== VERIFICATION SUMMARY ===" -ForegroundColor Cyan
Write-Host "Dump file: $(Split-Path $DumpFile -Leaf)" -ForegroundColor White
Write-Host "Schemas: $($dumpSchemas.Count)" -ForegroundColor White
Write-Host "Tables: $($dumpTables.Count)" -ForegroundColor White
Write-Host "Functions: $functionCount" -ForegroundColor White
Write-Host "Triggers: $triggerCount" -ForegroundColor White
Write-Host "Indexes: $indexCount" -ForegroundColor White

if ($liveSchemas.Count -gt 0 -and $liveTables.Count -gt 0) {
    $schemaMatch = ($missingSchemas.Count -eq 0)
    $tableMatch = ($missingTables.Count -eq 0)
    
    if ($schemaMatch -and $tableMatch) {
        Write-Host "`n✓ VERIFICATION PASSED: Dump appears complete!" -ForegroundColor Green
    } else {
        Write-Host "`n⚠ VERIFICATION WARNING: Some discrepancies found. Review above." -ForegroundColor Yellow
    }
} else {
    Write-Host "`n✓ DUMP FILE ANALYSIS COMPLETE" -ForegroundColor Green
    Write-Host "   (Live database comparison skipped - check manually if needed)" -ForegroundColor Gray
}

Write-Host ""

