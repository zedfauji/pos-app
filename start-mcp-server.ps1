# PostgreSQL MCP Server Background Script
# This script starts the MCP server in the background and provides interaction methods

param(
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status,
    [string]$Query = ""
)

$ContainerName = "postgres-mcp-server"
$DatabaseUri = "postgresql://posapp:Campus_66@34.51.82.201:5432/postgres?sslmode=require"

function Start-MCPServer {
    Write-Host "Starting PostgreSQL MCP Server..." -ForegroundColor Green
    
    # Stop existing container if running
    docker stop $ContainerName 2>$null
    docker rm $ContainerName 2>$null
    
    # Start new container in background
    $containerId = docker run -d --name $ContainerName -e "DATABASE_URI=$DatabaseUri" crystaldba/postgres-mcp --access-mode=unrestricted
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "MCP Server started successfully with ID: $containerId" -ForegroundColor Green
        Write-Host "Container name: $ContainerName" -ForegroundColor Yellow
        Write-Host "Use 'docker logs $ContainerName' to view logs" -ForegroundColor Cyan
        Write-Host "Use 'docker exec $ContainerName --help' to see available commands" -ForegroundColor Cyan
    } else {
        Write-Host "Failed to start MCP Server" -ForegroundColor Red
    }
}

function Stop-MCPServer {
    Write-Host "Stopping PostgreSQL MCP Server..." -ForegroundColor Yellow
    docker stop $ContainerName
    docker rm $ContainerName
    Write-Host "MCP Server stopped" -ForegroundColor Green
}

function Get-MCPStatus {
    $status = docker ps -a --filter "name=$ContainerName" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    if ($status -match $ContainerName) {
        Write-Host "MCP Server Status:" -ForegroundColor Cyan
        Write-Host $status
    } else {
        Write-Host "MCP Server is not running" -ForegroundColor Red
    }
}

function Execute-Query {
    param([string]$sql)
    if ([string]::IsNullOrEmpty($sql)) {
        Write-Host "No query provided" -ForegroundColor Red
        return
    }
    
    Write-Host "Executing query: $sql" -ForegroundColor Cyan
    docker exec $ContainerName psql "$DatabaseUri" -c "$sql"
}

# Main script logic
if ($Start) {
    Start-MCPServer
}
elseif ($Stop) {
    Stop-MCPServer
}
elseif ($Status) {
    Get-MCPStatus
}
elseif ($Query -ne "") {
    Execute-Query -sql $Query
}
else {
    Write-Host "PostgreSQL MCP Server Management Script" -ForegroundColor Magenta
    Write-Host "=========================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\start-mcp-server.ps1 -Start                    # Start MCP server" -ForegroundColor White
    Write-Host "  .\start-mcp-server.ps1 -Stop                     # Stop MCP server" -ForegroundColor White
    Write-Host "  .\start-mcp-server.ps1 -Status                   # Check server status" -ForegroundColor White
    Write-Host "  .\start-mcp-server.ps1 -Query 'SELECT * FROM pay.payments LIMIT 5;'" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\start-mcp-server.ps1 -Query \"\\dt pay.*\"" -ForegroundColor White
    Write-Host "  .\start-mcp-server.ps1 -Query \"SELECT COUNT(*) FROM pay.payments\"" -ForegroundColor White
}
