#!/bin/bash

PROJECT="PaintPower.csproj"
CONFIG="Release"

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

for RID in "${TARGETS[@]}"; do
  echo "Publishing for $RID..."
  dotnet publish "$PROJECT" -c "$CONFIG" -r "$RID" --self-contained false -o "publish/$RID"
done

echo "Done! Builds are in the /publish folder."
