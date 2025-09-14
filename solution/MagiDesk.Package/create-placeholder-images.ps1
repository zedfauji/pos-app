# Create placeholder images for MagiDesk MSIX package
# This script generates basic placeholder images for the installer

Write-Host "Creating placeholder images for MagiDesk installer..." -ForegroundColor Green

# Create a simple function to generate solid color images using .NET
Add-Type -AssemblyName System.Drawing

function Create-PlaceholderImage {
    param(
        [string]$FilePath,
        [int]$Width,
        [int]$Height,
        [string]$Text = "MagiDesk",
        [System.Drawing.Color]$BackgroundColor = [System.Drawing.Color]::FromArgb(0, 120, 215),
        [System.Drawing.Color]$TextColor = [System.Drawing.Color]::White
    )
    
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # Fill background
    $brush = New-Object System.Drawing.SolidBrush($BackgroundColor)
    $graphics.FillRectangle($brush, 0, 0, $Width, $Height)
    
    # Add text
    $font = New-Object System.Drawing.Font("Segoe UI", [math]::Min($Width/8, $Height/4), [System.Drawing.FontStyle]::Bold)
    $textBrush = New-Object System.Drawing.SolidBrush($TextColor)
    $textSize = $graphics.MeasureString($Text, $font)
    $x = ($Width - $textSize.Width) / 2
    $y = ($Height - $textSize.Height) / 2
    $graphics.DrawString($Text, $font, $textBrush, $x, $y)
    
    # Save image
    $bitmap.Save($FilePath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Cleanup
    $graphics.Dispose()
    $bitmap.Dispose()
    $brush.Dispose()
    $textBrush.Dispose()
    $font.Dispose()
    
    Write-Host "Created: $FilePath ($Width x $Height)" -ForegroundColor Gray
}

# Create all required images
$imagesPath = "Images"
if (-not (Test-Path $imagesPath)) {
    New-Item -ItemType Directory -Path $imagesPath -Force | Out-Null
}

# App icons and logos
Create-PlaceholderImage -FilePath "$imagesPath\Square44x44Logo.scale-200.png" -Width 88 -Height 88 -Text "MD"
Create-PlaceholderImage -FilePath "$imagesPath\Square44x44Logo.targetsize-24_altform-unplated.png" -Width 24 -Height 24 -Text "MD"
Create-PlaceholderImage -FilePath "$imagesPath\Square150x150Logo.scale-200.png" -Width 300 -Height 300 -Text "MagiDesk"
Create-PlaceholderImage -FilePath "$imagesPath\Wide310x150Logo.scale-200.png" -Width 620 -Height 300 -Text "MagiDesk POS"
Create-PlaceholderImage -FilePath "$imagesPath\SplashScreen.scale-200.png" -Width 1240 -Height 600 -Text "MagiDesk POS System"
Create-PlaceholderImage -FilePath "$imagesPath\LockScreenLogo.scale-200.png" -Width 48 -Height 48 -Text "MD"
Create-PlaceholderImage -FilePath "$imagesPath\StoreLogo.png" -Width 100 -Height 100 -Text "MD"

Write-Host "✅ All placeholder images created successfully!" -ForegroundColor Green
