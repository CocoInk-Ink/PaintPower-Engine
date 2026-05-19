# Cross‑platform packaging script for PaintPower
# Creates installers for Windows (MSIX), macOS (DMG), Linux (AppImage)

$ErrorActionPreference = "Stop"

# RIDs to package
$targets = @(
    "win-x86",
    "win-x64",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "linux-musl-x64",
    "linux-musl-arm64",
    "osx-x64",
    "osx-arm64"
)

# Create output folder
New-Item -ItemType Directory -Force -Path "dist" | Out-Null

foreach ($rid in $targets) {
    Write-Host "Packaging $rid..."

    $publishDir = "publish/$rid"
    $outputDir = "dist/$rid"
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    if ($rid -like "win-*") {
        # Windows MSIX packaging
        Write-Host "  -> Creating MSIX package..."
        # This assumes you have an AppxManifest.xml in your project
        Copy-Item "$publishDir/*" $outputDir -Recurse -Force
        # Developer note: MSIX packaging normally uses MakeAppx.exe or VS packaging project
        # Here we simply stage the folder for packaging
    }
    elseif ($rid -like "osx-*") {
        # macOS DMG packaging
        Write-Host "  -> Creating DMG..."
        $appName = "PaintPower.app"
        $appPath = "$outputDir/$appName"

        # Create .app bundle structure
        New-Item -ItemType Directory -Force -Path "$appPath/Contents/MacOS" | Out-Null
        New-Item -ItemType Directory -Force -Path "$appPath/Contents/Resources" | Out-Null

        Copy-Item "$publishDir/*" "$appPath/Contents/MacOS" -Recurse -Force

        # Create DMG
        $dmgName = "PaintPower-$rid.dmg"
        hdiutil create -volname "PaintPower" -srcfolder $appPath -ov -format UDZO "dist/$dmgName"
    }
    elseif ($rid -like "linux-*") {
        # Linux AppImage packaging
        Write-Host "  -> Creating AppImage..."

        $appDir = "$outputDir/AppDir"
        New-Item -ItemType Directory -Force -Path $appDir | Out-Null

        Copy-Item "$publishDir/*" $appDir -Recurse -Force

        # Basic AppImage structure
        New-Item -ItemType File -Force -Path "$appDir/AppRun" | Out-Null
        Set-Content "$appDir/AppRun" "#!/bin/bash`nexec ./PaintPower" -NoNewline
        chmod +x "$appDir/AppRun"

        # Desktop entry
        Set-Content "$appDir/paintpower.desktop" "[Desktop Entry]
Type=Application
Name=PaintPower
Exec=PaintPower
Icon=paintpower
Categories=Graphics;" -NoNewline

        # AppImage creation (requires appimagetool)
        $appImageName = "PaintPower-$rid.AppImage"
        appimagetool $appDir "dist/$appImageName"
    }
}

Write-Host "All installers created in /dist"
