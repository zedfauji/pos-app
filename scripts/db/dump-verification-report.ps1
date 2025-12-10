# Generate a comprehensive dump verification report

param(
    [string]$DumpFile = "dumps\postgres-dump-20251209-210039.sql"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  POSTGRESQL DUMP VERIFICATION REPORT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$dumpFile = Get-Item $DumpFile -ErrorAction Stop

Write-Host "DUMP FILE INFORMATION" -ForegroundColor Yellow
Write-Host "  File: $($dumpFile.Name)" -ForegroundColor White
Write-Host "  Path: $($dumpFile.FullName)" -ForegroundColor White
Write-Host "  Size: $([math]::Round($dumpFile.Length / 1MB, 2)) MB" -ForegroundColor White
Write-Host "  Lines: $((Get-Content $dumpFile | Measure-Object -Line).Lines)" -ForegroundColor White
Write-Host "  Created: $($dumpFile.LastWriteTime)" -ForegroundColor White
Write-Host ""

$content = Get-Content $dumpFile

Write-Host "SCHEMAS VERIFIED" -ForegroundColor Green
$schemas = $content | Select-String -Pattern '^CREATE SCHEMA' | ForEach-Object { 
    if ($_ -match 'CREATE SCHEMA "?([^";]+)"?') { $matches[1] }
} | Sort-Object -Unique
$schemas | ForEach-Object { Write-Host "  ✓ $_" -ForegroundColor Green }
Write-Host "  Total: $($schemas.Count) schemas" -ForegroundColor White
Write-Host ""

Write-Host "TABLES VERIFIED" -ForegroundColor Green
$tables = $content | Select-String -Pattern '^CREATE TABLE' | ForEach-Object {
    if ($_ -match 'CREATE TABLE "?([^".]+)"?\."?([^"(.]+)"?') {
        "$($matches[1]).$($matches[2])"
    }
} | Sort-Object -Unique
$tables | ForEach-Object { Write-Host "  ✓ $_" -ForegroundColor Green }
Write-Host "  Total: $($tables.Count) tables" -ForegroundColor White
Write-Host ""

Write-Host "DATA VERIFICATION" -ForegroundColor Green
$copyStatements = $content | Select-String -Pattern '^COPY' | Measure-Object
Write-Host "  ✓ $($copyStatements.Count) tables contain data (COPY statements)" -ForegroundColor Green
Write-Host ""

Write-Host "DATABASE OBJECTS VERIFIED" -ForegroundColor Green
$functions = ($content | Select-String -Pattern '^CREATE FUNCTION' | Measure-Object).Count
$triggers = ($content | Select-String -Pattern '^CREATE TRIGGER' | Measure-Object).Count
$indexes = ($content | Select-String -Pattern '^CREATE (UNIQUE )?INDEX' | Measure-Object).Count
$sequences = ($content | Select-String -Pattern '^CREATE SEQUENCE' | Measure-Object).Count
$views = ($content | Select-String -Pattern '^CREATE VIEW' | Measure-Object).Count

Write-Host "  ✓ Functions: $functions" -ForegroundColor Green
Write-Host "  ✓ Triggers: $triggers" -ForegroundColor Green
Write-Host "  ✓ Indexes: $indexes" -ForegroundColor Green
Write-Host "  ✓ Sequences: $sequences" -ForegroundColor Green
Write-Host "  ✓ Views: $views" -ForegroundColor Green
Write-Host ""

Write-Host "CONSTRAINTS" -ForegroundColor Green
$pkConstraints = ($content | Select-String -Pattern 'PRIMARY KEY' | Measure-Object).Count
$fkConstraints = ($content | Select-String -Pattern 'FOREIGN KEY' | Measure-Object).Count
$uniqueConstraints = ($content | Select-String -Pattern 'UNIQUE[^(]' | Measure-Object).Count
Write-Host "  ✓ Primary Keys: $pkConstraints" -ForegroundColor Green
Write-Host "  ✓ Foreign Keys: $fkConstraints" -ForegroundColor Green
Write-Host "  ✓ Unique Constraints: $uniqueConstraints" -ForegroundColor Green
Write-Host ""

Write-Host "SCHEMA BREAKDOWN" -ForegroundColor Yellow
foreach ($schema in $schemas) {
    $schemaTables = $tables | Where-Object { $_ -like "$schema.*" }
    Write-Host "  $schema : $($schemaTables.Count) tables" -ForegroundColor White
}

# Expected POS system tables
Write-Host ""
Write-Host "EXPECTED POS SYSTEM TABLES" -ForegroundColor Yellow
$expectedSchemas = @('customers', 'inventory', 'menu', 'ord', 'pay', 'settings', 'users', 'public')
$expectedTableCounts = @{
    'customers' = 5
    'inventory' = 7
    'menu' = 8
    'ord' = 3
    'pay' = 4
    'settings' = 2
    'users' = 4
    'public' = 10  # tables, floors, sessions, bills, etc.
}

foreach ($schema in $expectedSchemas) {
    $expected = $expectedTableCounts[$schema]
    $actual = ($tables | Where-Object { $_ -like "$schema.*" }).Count
    if ($actual -eq $expected) {
        Write-Host "  ✓ $schema : $actual/$expected tables" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ $schema : $actual/$expected tables (expected $expected, found $actual)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "VERIFICATION STATUS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$allGood = $true
if ($tables.Count -lt 40) {
    Write-Host "⚠ WARNING: Expected at least 40 tables, found $($tables.Count)" -ForegroundColor Yellow
    $allGood = $false
}
if ($copyStatements.Count -lt 40) {
    Write-Host "⚠ WARNING: Expected data in at least 40 tables, found $($copyStatements.Count)" -ForegroundColor Yellow
    $allGood = $false
}

if ($allGood) {
    Write-Host "✓ DUMP VERIFICATION PASSED" -ForegroundColor Green
    Write-Host "  The dump file appears to contain all expected database objects and data." -ForegroundColor White
} else {
    Write-Host "⚠ VERIFICATION WARNING" -ForegroundColor Yellow
    Write-Host "  Please review the discrepancies above." -ForegroundColor White
}

Write-Host ""
Write-Host "To restore this dump:" -ForegroundColor Cyan
Write-Host "  psql -h 34.51.82.201 -p 5432 -U posapp -d postgres -f `"$($dumpFile.Name)`"" -ForegroundColor White
Write-Host ""

