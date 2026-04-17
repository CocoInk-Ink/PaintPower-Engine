using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using PaintPower.ProjectSystem;
using PaintPower.Logging;

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

    public void closeAllConnections() {
        AllowedDomainsList.Clear();
    }

    // Make a valid url.
    public string makeUrl(string addon = "")
    {
        return $"{URLifyer.URLify(CurrentDomain)}/{addon}";
    }

    // Create, register, and add default domains.
    public void loadDefaultDomains() {
        
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

    public async Task<bool> checkConnection() {
        var domain = CurrentDomain;
        if (domain == null) throw new ArgumentNullException(nameof(domain));

        if (!IsDomainAllowed(domain)) throw new UnauthorizedAccessException("Domain not allowed");

        try {
            return await Net.PerformGetRequest(makeUrl(Routes.checkActiveServer())) == "Ok.";
        }
        catch
        {
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
    public async Task DownloadProject(string savePath)
    {
        string url = URLifyer.URLify(CurrentDomain);
        await Net.DownloadFileAsync(url, savePath);
    }

    /* Save the project and load it into the editor. */
    public async Task DownloadProjectAndLoad(string savePath)
    {
        try {
            await DownloadProject(savePath);
        } catch(Exception e)
        {
            Log.QuickLog(e.Message);
        }

        PaintPower_Engine.App.OpenProjectFile(savePath);
    }

    public async Task UploadProject(PaintProject project)
    {
        #pragma warning disable
        PaintPower_Engine.App.RunSavingAnimation();

        try
        {
            await Net.UploadFileAsync(
                 $"{URLifyer.URLify(CurrentDomain)}api/upload/projects/1/",
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
    public async Task ListUserProjects()
    {
        string url =
        #if DEBUG
        makeUrl(Routes.testServerListProjects());
        #else
        makeUrl(Routes.userProjectsRoute());
        #endif
        GetFromServer(url); // Do nothing with the data for now...
    }

    public Server() {
        InitServer();
    }
}