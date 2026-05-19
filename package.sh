#!/bin/bash
set -e

# Cross‑platform packaging script for PaintPower
# Creates installers for Windows (MSIX), macOS (DMG), Linux (AppImage)

TARGETS=(
  "win-x86"
  "win-x64"
  "win-arm64"
  "linux-x64"
  "linux-arm64"
  "linux-musl-x64"
  "linux-musl-arm64"
  "osx-x64"
  "osx-arm64"
)

mkdir -p dist

for RID in "${TARGETS[@]}"; do
  echo "Packaging $RID..."

  PUBLISH_DIR="publish/$RID"
  OUTPUT_DIR="dist/$RID"
  mkdir -p "$OUTPUT_DIR"

  if [[ "$RID" == win-* ]]; then
    echo "  -> Preparing MSIX staging folder..."
    cp -r "$PUBLISH_DIR"/* "$OUTPUT_DIR"
    # Note: MSIX creation normally uses MakeAppx.exe or VS packaging project
  elif [[ "$RID" == osx-* ]]; then
    echo "  -> Creating DMG..."
    APP_NAME="PaintPower.app"
    APP_PATH="$OUTPUT_DIR/$APP_NAME"

    mkdir -p "$APP_PATH/Contents/MacOS"
    mkdir -p "$APP_PATH/Contents/Resources"

    cp -r "$PUBLISH_DIR"/* "$APP_PATH/Contents/MacOS"

    DMG_NAME="PaintPower-$RID.dmg"
    hdiutil create -volname "PaintPower" -srcfolder "$APP_PATH" -ov -format UDZO "dist/$DMG_NAME"
  elif [[ "$RID" == linux-* ]]; then
    echo "  -> Creating AppImage..."
    APPDIR="$OUTPUT_DIR/AppDir"
    mkdir -p "$APPDIR"

    cp -r "$PUBLISH_DIR"/* "$APPDIR"

    # AppRun
    echo -e "#!/bin/bash\nexec ./PaintPower" > "$APPDIR/AppRun"
    chmod +x "$APPDIR/AppRun"

    # Desktop entry
    echo "[Desktop Entry]
Type=Application
Name=PaintPower
Exec=PaintPower
Icon=paintpower
Categories=Graphics;" > "$APPDIR/paintpower.desktop"

    # Build AppImage (requires appimagetool)
    APPIMAGE_NAME="PaintPower-$RID.AppImage"
    appimagetool "$APPDIR" "dist/$APPIMAGE_NAME"
  fi
done

echo "All installers created in /dist"
