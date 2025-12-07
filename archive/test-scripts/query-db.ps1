# Database Query Helper Script
# Simple script to execute PostgreSQL queries against the billiard POS database

param(
    [Parameter(Mandatory=$true)]
    [string]$Query
)

$DatabaseUri = "postgresql://posapp:Campus_66@34.51.82.201:5432/postgres?sslmode=require"

Write-Host "Executing query..." -ForegroundColor Cyan
Write-Host "Query: $Query" -ForegroundColor Yellow
Write-Host "=" * 50 -ForegroundColor Gray

try {
    docker run --rm -e PGPASSWORD=Campus_66 postgres:14-alpine psql -h 34.51.82.201 -U posapp -d postgres -c "$Query"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Query executed successfully" -ForegroundColor Green
    } else {
        Write-Host "Query failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host "Error executing query: $($_.Exception.Message)" -ForegroundColor Red
}
