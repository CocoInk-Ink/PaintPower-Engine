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
    public async Task DownloadProject(string savePath, int id)
    {
        if (!isConnected) return;

        string url = MakeUrl($"{id}");
        await Net.DownloadFileAsync(url, savePath);
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
    public async Task<List<ProjectInfo>> ListUserProjects()
    {
        if (!isConnected) return new();

        string url = MakeUrl(Routes.userProjectsRoute());
        string? response = await Net.PerformGetRequest(url);

        if (response == null) return new();

        try
        {
            return JsonSerializer.Deserialize<List<ProjectInfo>>(response) ?? new();
        }
        catch
        {
            Log.QuickLog("Failed to parse project list JSON.");
            return new();
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
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            return json?["id"];
        }
        catch
        {
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
        InitServer();
    }
}
