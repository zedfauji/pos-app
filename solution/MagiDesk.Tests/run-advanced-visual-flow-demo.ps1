# Script to run Advanced Visual Flow Demo Test

Write-Host "🎬 RUNNING ADVANCED VISUAL FLOW DEMO"
Write-Host "===================================="
Write-Host ""
Write-Host "👀 WATCH: This will launch MagiDesk and navigate through ALL pages!"
Write-Host "🎯 You'll see: Dashboard → Tables → Orders → Payments → Settings"
Write-Host "🚀 Perfect for demonstrating the complete user journey!"
Write-Host ""

# Step 1: Kill any existing MagiDesk processes to ensure a clean start
Write-Host "🔪 Killing any existing MagiDesk processes..."
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host ""

# Step 2: Run the advanced visual flow demo test
Write-Host "🎬 Running Advanced Visual Flow Demo Test..."
Write-Host "⏱️  This demo will take approximately 3-4 minutes to complete"
Write-Host "👀 You'll see the application navigate through different pages!"
Write-Host "📱 Watch the application window as it moves between sections!"
Write-Host ""

$testResult = dotnet test MagiDesk.Tests --filter "AdvancedVisualFlowDemo_CompleteUserJourney_ShouldShowAllFlows" --logger "console;verbosity=detailed"
$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "✅ Advanced Visual Flow Demo completed!"
Write-Host "Exit Code: $exitCode"
Write-Host ""

# Step 3: Final cleanup
Write-Host "🧹 Final cleanup..."
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "🎉 Advanced Visual Flow Demo completed successfully!"
    Write-Host "✅ All page navigations were demonstrated successfully!"
    Write-Host "👀 You should have seen the app navigate through all sections!"
} else {
    Write-Host "⚠️  Advanced Visual Flow Demo completed with some issues"
    Write-Host "📋 Check the output above for details"
}

Write-Host ""
Write-Host "🎬 Advanced Visual Flow Demo execution completed!"

# Exit with the test result exit code
exit $exitCode
