# Script to run Visual Flow Demo Test

Write-Host "🎬 RUNNING VISUAL FLOW DEMO"
Write-Host "==========================="
Write-Host ""
Write-Host "👀 WATCH: This will launch the MagiDesk application and demonstrate all flows!"
Write-Host "🎯 Perfect for showing to users and validating functionality!"
Write-Host ""

# Step 1: Kill any existing MagiDesk processes to ensure a clean start
Write-Host "🔪 Killing any existing MagiDesk processes..."
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host ""

# Step 2: Run the visual flow demo test
Write-Host "🎬 Running Visual Flow Demo Test..."
Write-Host "⏱️  This demo will take approximately 2-3 minutes to complete"
Write-Host "👀 You'll see the application launch and navigate through all flows!"
Write-Host ""

$testResult = dotnet test MagiDesk.Tests --filter "VisualFlowDemo_CompleteUserJourney_ShouldShowAllFlows" --logger "console;verbosity=detailed"
$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "✅ Visual Flow Demo completed!"
Write-Host "Exit Code: $exitCode"
Write-Host ""

# Step 3: Final cleanup
Write-Host "🧹 Final cleanup..."
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "🎉 Visual Flow Demo completed successfully!"
    Write-Host "✅ All flows were demonstrated successfully!"
} else {
    Write-Host "⚠️  Visual Flow Demo completed with some issues"
    Write-Host "📋 Check the output above for details"
}

Write-Host ""
Write-Host "🎬 Visual Flow Demo execution completed!"

# Exit with the test result exit code
exit $exitCode



