# Publish script for PaintPower (Windows, Linux, macOS)
# Builds x86, x64, and ARM where supported

$project = "PaintPower.csproj"
$configuration = "Release"

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

foreach ($rid in $targets) {
    Write-Host "Publishing for $rid..."
    dotnet publish $project -c $configuration -r $rid --self-contained false -o "publish/$rid"
}

Write-Host "Done! Builds are in the /publish folder."
