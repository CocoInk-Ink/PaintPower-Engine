using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using PaintPower.ProjectSystem;
using PaintPower.Logging;
using System.Text.Json;

namespace PaintPower.Networking;

// Networking class for the PaintPower Engine.
// Mainly to be used for the 'Coco xPaint Project', but it will lie
// here in the engine because it's open source. So anyone can create their own server.
public class Server
{
    //*--- Domain security. ---*//
    private static List<Domain> AllowedDomainsList = new List<Domain>();
    private bool isConnected = false;
    public void AllowDomain(Domain domain) => AllowedDomainsList.Add(domain);
    public bool IsDomainAllowed(Domain domain) => AllowedDomainsList.Contains(domain);

    public void ClearAllowedDomains() => AllowedDomainsList.Clear();

    public void RemoveDomain(Domain domain) { AllowedDomainsList.Remove(domain); }

    public Domain CurrentDomain = new Domain("www.cocoink.ink/f/PaintPower");

    public void closeAllConnections()
    {
        AllowedDomainsList.Clear();
        PaintPower_Engine.App.SetNetworkStatus("Not connected");
    }

    // Make a valid url.
    public string makeUrl(string addon = "")
    {
        string url = $"{URLifyer.URLify(CurrentDomain)}{addon}";
        Log.QuickLog($"Url made: {url}");
        return url;
    }

    // Create, register, and add default domains.
    public void loadDefaultDomains()
    {

        // Clear old list
        AllowedDomainsList.Clear();

        // Create Coco links, Paint links, random links, and more!
        // Creating a custom server? Make a issue on GitHub and I'll add it here!
        Domain d1 = new Domain("xpaint.cocoink.ink");
        Domain d2 = new Domain("paint.cocoink.ink");
        Domain d3 = new Domain("127.0.0.1:5500/f/xPaint");
        Domain d4 = new Domain("127.0.0.1:5000/f/xPaint");
        Domain d5 = new Domain("127.0.0.1:3000/f/xPaint");
        Domain d12 = new Domain("0.0.0.0:5500/f/xPaint");
        Domain d6 = new Domain("127.0.0.1:8000");
        Domain d7 = new Domain("localhost:5500");
        Domain d8 = new Domain("localhost:5000");
        Domain d9 = new Domain("localhost:8000");
        Domain d10 = new Domain("localhost:3000");
        Domain d11 = new Domain("github.com");
        Domain d13 = new Domain("paint-website.onrender.com");
        Domain d14 = new Domain("paintpower.cocoink.ink");
        Domain d15 = new Domain("www.cocoink.ink");
        Domain d16 = new Domain("www.cocoink.ink/f/xPaint");
        Domain d17 = new Domain("www.cocoink.ink/f/Paint");
        Domain d18 = new Domain("www.cocoink.ink/f/PaintPower");
        Domain d19 = new Domain("negro.org");
        Domain d20 = new Domain("example.com");

        // Add to list
        AllowDomain(d1); AllowDomain(d2); AllowDomain(d3); AllowDomain(d4); AllowDomain(d5);
        AllowDomain(d6); AllowDomain(d7); AllowDomain(d8); AllowDomain(d9); AllowDomain(d10);
        AllowDomain(d11); AllowDomain(d12); AllowDomain(d13); AllowDomain(d14); AllowDomain(d15);
        AllowDomain(d16); AllowDomain(d17); AllowDomain(d18); AllowDomain(d19); AllowDomain(d20);

#if DEBUG
        setActiveDomain(d3);
#else
        setActiveDomain(d16);
#endif
    }

    public void setActiveDomain(Domain domain)
    {
        CurrentDomain = domain;
    }

    //*--- Networking ---*//
    public async Task InitServer()
    {
        loadDefaultDomains();
        isConnected = await checkConnection();
    }

    public async Task<bool> checkConnection()
    {
        var domain = CurrentDomain;
        if (domain == null) throw new ArgumentNullException(nameof(domain));

        if (!IsDomainAllowed(domain)) throw new UnauthorizedAccessException("Domain not allowed");

        try
        {
            bool c = await Net.PerformGetRequest(makeUrl(Routes.checkActiveServer())) == "Ok.";
            PaintPower_Engine.App.SetNetworkStatus(c ? "Connected" : "Not connected");
            return c;
        }
        catch
        {
            PaintPower_Engine.App.SetNetworkStatus("Not connected");
            return false;
        }
    }

    public async Task<object?> GetFromServer(string url)
    {
        var domain = CurrentDomain;
        if (domain == null) throw new ArgumentNullException(nameof(domain));
        if (!IsDomainAllowed(domain)) throw new UnauthorizedAccessException("Domain not allowed");
        return await Net.PerformGetRequest(url);
    }

    /* Download a project made by the user */
    public async Task DownloadProject(string savePath, int id)
    {
        string url = makeUrl($"{id}");
        await Net.DownloadFileAsync(url, savePath);
    }

    /* Save the project and load it into the editor. */
    public async Task DownloadProjectAndLoad(string savePath, int id)
    {
        try
        {
            await DownloadProject(savePath, id);
            PaintPower_Engine.App.OpenProjectFile(savePath);
        }
        catch (Exception e)
        {
            Log.QuickLog(e.Message);
        }
    }

    public async Task UploadProject(PaintProject project)
    {
#pragma warning disable
        PaintPower_Engine.App.RunSavingAnimation();

        try
        {
            await Net.UploadFileAsync(
                 makeUrl(Routes.normalOverwriteUpload(PaintPower_Engine.App._project.Metadata.serverId)),
                 project.ProjectPath,
                 project.Metadata.name // send project title
             );
        }
        finally
        {
            MainWindow.App._isSavingAnimationRunning = false;
        }
    }

    // If the user is signed in, then get a list of their projects from the server.
    public async Task<List<ProjectInfo>> ListUserProjects()
    {
        string url = makeUrl(Routes.userProjectsRoute());
        string? response = await Net.PerformGetRequest(url);

        if (response == null) return new List<ProjectInfo>();

        Log.QuickLog(response);

        List<ProjectInfo> list = new List<ProjectInfo>();

        try
        {
            list = JsonSerializer.Deserialize<List<ProjectInfo>>(response) ?? new List<ProjectInfo>();
        }
        catch { Log.QuickLog("Threw at JSON."); };

        return list;
    }


    public async Task<string?> CreateNewServerProject(string title)
    {
        string url = makeUrl(Routes.createNew());
        var payload = new { title = title };

        string? response = (string)await Net.PerformPostRequest(url, payload);
        if (response == null) return null;

        try {
        var json = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
        return json?["id"];
        } catch { return null; }
    }

    public string Username { get; set; }

    public async Task<bool> Login(string username, string password)
    {
        if (await IsLoggedIn()) await Logout();
        await Net.Login(username, password);
        if (await IsLoggedIn()) Username = username;
        return await IsLoggedIn();
    }

    public async Task Logout()
    {
        if (await IsLoggedIn()) await Net.PerformPostRequest(PaintPower_Engine.App.server.makeUrl("logout"), new Dictionary<string, bool> { { "redirect", false } });
        if (!await IsLoggedIn()) Username = "";
    }

    public async Task<bool> IsLoggedIn()
    {
        var response = await Net.PerformGetRequest(makeUrl("api/whoami"));
        return response != null && !response.Contains("Not logged in");
    }

    public Server()
    {
        InitServer();
    }
}