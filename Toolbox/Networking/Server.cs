using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Toolbox.Logging;

namespace Toolbox.Networking;

public class Server
{
    private static readonly List<Domain> AllowedDomainsList = new();
    private bool isConnected = false;

    public Domain CurrentDomain = new Domain("www.cocoink.ink/f/PaintPower");
    public string Username { get; private set; } = "";
    public bool IsConnected => isConnected;

    // ------------------------------------------------------------
    // Domain Security
    // ------------------------------------------------------------
    public void AllowDomain(Domain domain) => AllowedDomainsList.Add(domain);
    public bool IsDomainAllowed(Domain domain) => AllowedDomainsList.Contains(domain);
    public void ClearAllowedDomains() => AllowedDomainsList.Clear();
    public void RemoveDomain(Domain domain) => AllowedDomainsList.Remove(domain);

    public void CloseAllConnections()
    {
        AllowedDomainsList.Clear();
        isConnected = false;
    }

    public string MakeUrl(string addon = "")
    {
        string url = $"{URLifyer.URLify(CurrentDomain)}{addon}";
        Log.QuickLog($"Url made: {url}");
        return url;
    }

    public void LoadDefaultDomains()
    {
        AllowedDomainsList.Clear();

        Domain[] defaults =
        {
            new("xpaint.cocoink.ink"),
            new("paint.cocoink.ink"),
            new("127.0.0.1:5500/f/xPaint"),
            new("127.0.0.1:5000/f/xPaint"),
            new("127.0.0.1:3000/f/xPaint"),
            new("0.0.0.0:5500/f/xPaint"),
            new("127.0.0.1:8000"),
            new("localhost:5500"),
            new("localhost:5000"),
            new("localhost:8000"),
            new("localhost:3000"),
            new("github.com"),
            new("paint-website.onrender.com"),
            new("paintpower.cocoink.ink"),
            new("www.cocoink.ink"),
            new("www.cocoink.ink/f/xPaint"),
            new("www.cocoink.ink/f/Paint"),
            new("www.cocoink.ink/f/PaintPower"),
            new("negro.org"),
            new("example.com")
        };

        foreach (var d in defaults)
            AllowDomain(d);

#if DEBUG
        SetActiveDomain(defaults[2]); // 127.0.0.1:5500/f/xPaint
#else
        SetActiveDomain(defaults[15]); // www.cocoink.ink/f/xPaint
#endif
    }

    public void SetActiveDomain(Domain domain)
    {
        CurrentDomain = domain;
    }

    // ------------------------------------------------------------
    // Networking
    // ------------------------------------------------------------
    public async Task InitServer()
    {
        LoadDefaultDomains();
        isConnected = await CheckConnection();
    }

    public async Task<bool> CheckConnection()
    {
        if (!IsDomainAllowed(CurrentDomain))
            throw new UnauthorizedAccessException("Domain not allowed");

        try
        {
            bool ok = await Net.PerformGetRequest(MakeUrl(Routes.checkActiveServer())) == "Ok.";
            isConnected = ok;
            return ok;
        }
        catch
        {
            isConnected = false;
            return false;
        }
    }

    public async Task<string?> GetFromServer(string url)
    {
        if (!isConnected) return null;
        if (!IsDomainAllowed(CurrentDomain)) throw new UnauthorizedAccessException("Domain not allowed");

        return await Net.PerformGetRequest(url);
    }

    // ------------------------------------------------------------
    // Project Download
    // ------------------------------------------------------------
    public async Task<bool> DownloadProjectSource(string savePath, int id)
    {
        return await DownloadLatestProjectSource(savePath, id.ToString());
    }

    public async Task<bool> DownloadLatestProjectSource(string savePath, string projectId)
    {
        if (!isConnected) return false;
        return await Net.DownloadFileAsync(
            MakeUrl(Routes.downloadLatestSource(projectId)),
            savePath
        );
    }

    public async Task<bool> DownloadProjectVersionSource(
        string savePath,
        string projectId,
        string versionId)
    {
        if (!isConnected) return false;
        return await Net.DownloadFileAsync(
            MakeUrl(Routes.downloadVersionSource(projectId, versionId)),
            savePath
        );
    }

    // ------------------------------------------------------------
    // Project Versions
    // ------------------------------------------------------------
    public async Task<string?> UploadNewVersion(
        string projectId,
        string sourcePath,
        string? buildPath = null,
        string? versionName = null,
        bool isShared = false)
    {
        if (!isConnected || !await IsLoggedIn()) return null;
        if (!File.Exists(sourcePath) ||
            !string.Equals(Path.GetExtension(sourcePath), ".xpaint", StringComparison.OrdinalIgnoreCase))
            return null;
        if (!string.IsNullOrWhiteSpace(buildPath) &&
            (!File.Exists(buildPath) ||
             !string.Equals(Path.GetExtension(buildPath), ".xpe", StringComparison.OrdinalIgnoreCase)))
            return null;

        var files = new Dictionary<string, string> { ["source"] = sourcePath };
        if (!string.IsNullOrWhiteSpace(buildPath))
            files["build"] = buildPath;

        var fields = new Dictionary<string, string> { ["isShared"] = isShared ? "true" : "false" };
        if (!string.IsNullOrWhiteSpace(versionName))
            fields["version"] = versionName;

        string? response = await Net.UploadFilesAsync(
            MakeUrl(Routes.createVersion(projectId)),
            files,
            fields
        );
        if (response == null) return null;

        try
        {
            using var json = JsonDocument.Parse(response);
            return json.RootElement
                .GetProperty("version")
                .GetProperty("id")
                .GetString();
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Failed to read created version ID: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateProjectVersionSource(
        string projectId,
        string versionId,
        string sourcePath)
    {
        if (!isConnected || !await IsLoggedIn()) return false;
        if (!File.Exists(sourcePath) ||
            !string.Equals(Path.GetExtension(sourcePath), ".xpaint", StringComparison.OrdinalIgnoreCase))
            return false;

        string? response = await Net.UploadFilesAsync(
            MakeUrl(Routes.updateVersionSource(projectId, versionId)),
            new Dictionary<string, string> { ["file"] = sourcePath }
        );
        return response != null;
    }

    public async Task<List<ProjectVersionInfo>?> ListProjectVersions(string projectId)
    {
        if (!isConnected || !await IsLoggedIn()) return null;

        string? response = await Net.PerformGetRequest(
            MakeUrl(Routes.listVersions(projectId))
        );
        if (response == null) return null;

        try
        {
            return JsonSerializer.Deserialize<ProjectVersionInfoList>(response)!.versions;
        }
        catch (JsonException ex)
        {
            Log.QuickLog($"Failed to parse project versions JSON: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteProjectVersion(string projectId, string versionId)
    {
        if (!isConnected || !await IsLoggedIn()) return false;

        string? response = await Net.PerformDeleteRequest(
            MakeUrl(Routes.deleteVersion(projectId, versionId))
        );
        return response != null;
    }

    public async Task<bool> UpdateProjectVersionBuild(
        string projectId,
        string versionId,
        string buildPath)
    {
        if (!isConnected || !await IsLoggedIn()) return false;
        if (!File.Exists(buildPath) ||
            !string.Equals(Path.GetExtension(buildPath), ".xpe", StringComparison.OrdinalIgnoreCase))
            return false;

        string? response = await Net.UploadFilesAsync(
            MakeUrl(Routes.updateVersionBuild(projectId, versionId)),
            new Dictionary<string, string> { ["file"] = buildPath }
        );
        return response != null;
    }

    public async Task<bool> SelectProjectVersion(string projectId, string versionId)
    {
        if (!isConnected || !await IsLoggedIn()) return false;
        string? response = await Net.PerformPostRequest(
            MakeUrl(Routes.selectVersion(projectId, versionId)),
            new { }
        );
        return response != null;
    }

    // ------------------------------------------------------------
    // Project Upload
    // ------------------------------------------------------------
    /*public async Task UploadProject(PaintProject project)
    {
        if (!isConnected) return;
        if (string.IsNullOrWhiteSpace(project.ProjectPath)) return;

        string url = MakeUrl(Routes.normalOverwriteUpload(project.Metadata.serverId));

        await Net.UploadFileAsync(
            url,
            project.ProjectPath,
            project.Metadata.name
        );
    }*/

    // ------------------------------------------------------------
    // User Projects
    // ------------------------------------------------------------
    public async Task<List<ProjectInfo>?> ListUserProjects()
    {
        if (!isConnected) return null;

        string url = MakeUrl(Routes.userProjectsRoute());
        string? response = await Net.PerformGetRequest(url);

        if (response == null) return null;

        try
        {
            return JsonSerializer.Deserialize<List<ProjectInfo>>(response);
        }
        catch (JsonException ex)
        {
            Log.QuickLog($"Failed to parse project list JSON: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------
    // Create New Server Project
    // ------------------------------------------------------------
    public async Task<string?> CreateNewServerProject(string? title)
    {
        if (!isConnected) return null;
        if (!await IsLoggedIn()) return null;

        if (string.IsNullOrWhiteSpace(title))
            title = "Untitled Project";

        if (title.Length > 100)
            title = title[..100];

        string url = MakeUrl(Routes.createNew());
        var payload = new { title };

        string? response = (string?)await Net.PerformPostRequest(url, payload);
        if (response == null) return null;

        try
        {
            using var json = JsonDocument.Parse(response);
            if (json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("id", out var id) &&
                id.ValueKind == JsonValueKind.String)
            {
                return id.GetString();
            }

            Log.QuickLog("Server project creation response did not contain a project ID.");
            return null;
        }
        catch (JsonException ex)
        {
            Log.QuickLog($"Failed to parse server project creation response: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------
    // Login / Logout
    // ------------------------------------------------------------
    // ------------------------------------------------------------
    // Login / Logout
    // ------------------------------------------------------------
    public async Task<bool> Login(string username, string password)
    {
        if (!isConnected) return false;

        await Logout();

        string loginUrl = MakeUrl("login");
        await Net.Login(loginUrl, username, password);

        if (await IsLoggedIn())
            Username = username;

        return await IsLoggedIn();
    }

    public async Task Logout()
    {
        if (!await IsLoggedIn()) return;

        await Net.PerformPostRequest(
            MakeUrl("logout"),
            new Dictionary<string, bool> { { "redirect", false } }
        );

        if (!await IsLoggedIn())
            Username = "";
    }

    public async Task<bool> IsLoggedIn()
    {
        if (!isConnected) return false;

        var response = await Net.PerformGetRequest(MakeUrl("api/whoami"));
        return response != null && !response.Contains("Not logged in");
    }

    public Server()
    {
        LoadDefaultDomains();
    }
}


class ProjectVersionInfoList
{
    public string status { get; set; } = "";
    public List<ProjectVersionInfo> versions { get; set; } = new();
};