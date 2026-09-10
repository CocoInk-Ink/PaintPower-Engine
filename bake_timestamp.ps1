Set-Location .\Toolbox

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$content = @"
namespace Toolbox.Time
{
    public static class BuildInfo
    {
        public const string Timestamp = "$timestamp";
    }
}
"@

Set-Content -Path "BuildInfo.g.cs" -Value $content
