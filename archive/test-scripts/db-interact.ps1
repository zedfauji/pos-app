# Database Interaction Script for Billiard POS
# This script provides easy database access for the payment flow audit

param(
    [Parameter(Mandatory=$false)]
    [string]$Query = "",
    [switch]$ListTables,
    [switch]$PaymentStats,
    [switch]$PaymentFlow,
    [switch]$Help
)

$DatabaseHost = "34.51.82.201"
$DatabaseUser = "posapp"
$DatabasePassword = "Campus_66"
$DatabaseName = "postgres"

function Execute-Query {
    param([string]$sql)
    Write-Host "Executing: $sql" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Gray
    
    docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "$sql"
}

function Show-PaymentStats {
    Write-Host "Payment Statistics" -ForegroundColor Magenta
    Write-Host "==================" -ForegroundColor Magenta
    
    Execute-Query "SELECT 
        COUNT(*) as total_payments,
        SUM(amount_paid) as total_amount,
        AVG(amount_paid) as avg_amount,
        payment_method,
        COUNT(*) as method_count
    FROM pay.payments 
    GROUP BY payment_method 
    ORDER BY method_count DESC;"
}

function Show-PaymentFlow {
    Write-Host "Payment Flow Analysis" -ForegroundColor Magenta
    Write-Host "=====================" -ForegroundColor Magenta
    
    Execute-Query "SELECT 
        bl.billing_id,
        bl.total_due,
        bl.total_paid,
        bl.total_discount,
        bl.total_tip,
        bl.status,
        COUNT(p.payment_id) as payment_count
    FROM pay.bill_ledger bl
    LEFT JOIN pay.payments p ON bl.billing_id = p.billing_id
    GROUP BY bl.billing_id, bl.total_due, bl.total_paid, bl.total_discount, bl.total_tip, bl.status
    ORDER BY bl.created_at DESC
    LIMIT 10;"
}

function Show-Tables {
    Write-Host "Database Tables" -ForegroundColor Magenta
    Write-Host "===============" -ForegroundColor Magenta
    
    Execute-Query "\dt pay.*"
    Execute-Query "\dt inventory.*"
    Execute-Query "\dt tables.*"
}

function Show-Help {
    Write-Host "Database Interaction Script for Billiard POS" -ForegroundColor Magenta
    Write-Host "=============================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\db-interact.ps1 -Query 'SELECT * FROM pay.payments LIMIT 5;'" -ForegroundColor White
    Write-Host "  .\db-interact.ps1 -ListTables" -ForegroundColor White
    Write-Host "  .\db-interact.ps1 -PaymentStats" -ForegroundColor White
    Write-Host "  .\db-interact.ps1 -PaymentFlow" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\db-interact.ps1 -Query \"\\d pay.payments\"" -ForegroundColor White
    Write-Host "  .\db-interact.ps1 -Query \"SELECT COUNT(*) FROM pay.payments\"" -ForegroundColor White
    Write-Host "  .\db-interact.ps1 -Query \"SELECT * FROM pay.bill_ledger WHERE status = 'unpaid'\"" -ForegroundColor White
}

# Main script logic
if ($Help) {
    Show-Help
}
elseif ($ListTables) {
    Show-Tables
}
elseif ($PaymentStats) {
    Show-PaymentStats
}
elseif ($PaymentFlow) {
    Show-PaymentFlow
}
elseif ($Query -ne "") {
    Execute-Query -sql $Query
}
else {
    Show-Help
}
