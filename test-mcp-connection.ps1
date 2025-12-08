# Test Postgres MCP Pro Connection
# This script helps verify the MCP server configuration

Write-Host "Testing Postgres MCP Pro Configuration..." -ForegroundColor Green

# Check if postgres-mcp is installed
Write-Host "`n1. Checking if postgres-mcp is installed..." -ForegroundColor Yellow
try {
    $version = & postgres-mcp --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ postgres-mcp is installed: $version" -ForegroundColor Green
    } else {
        Write-Host "✗ postgres-mcp not found in PATH" -ForegroundColor Red
        Write-Host "Make sure to activate the virtual environment:" -ForegroundColor Yellow
        Write-Host "  cd postgres-mcp" -ForegroundColor Cyan
        Write-Host "  .venv\Scripts\activate" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ postgres-mcp not found" -ForegroundColor Red
}

# Check MCP configuration file
Write-Host "`n2. Checking MCP configuration..." -ForegroundColor Yellow
$mcpConfigPath = ".cursor\mcp.json"
if (Test-Path $mcpConfigPath) {
    Write-Host "✓ MCP configuration file exists" -ForegroundColor Green
    $config = Get-Content $mcpConfigPath | ConvertFrom-Json
    if ($config.mcpServers."postgres-mcp".env.DATABASE_URI -like "*YOUR_*") {
        Write-Host "⚠ Configuration needs to be updated with actual Cloud SQL details" -ForegroundColor Yellow
    } else {
        Write-Host "✓ Configuration appears to be set up" -ForegroundColor Green
    }
} else {
    Write-Host "✗ MCP configuration file not found" -ForegroundColor Red
}

# Instructions for next steps
Write-Host "`n3. Next Steps:" -ForegroundColor Yellow
Write-Host "1. Update .cursor\mcp.json with your Cloud SQL connection details" -ForegroundColor Cyan
Write-Host "2. Restart Cursor to load the MCP server" -ForegroundColor Cyan
Write-Host "3. Test queries in Cursor chat" -ForegroundColor Cyan

Write-Host "`nExample queries to try in Cursor:" -ForegroundColor Green
Write-Host "- 'Check the health of my database'" -ForegroundColor Cyan
Write-Host "- 'List all tables in the database'" -ForegroundColor Cyan
Write-Host "- 'Show me the schema of the users table'" -ForegroundColor Cyan



