/*
    File containing routes for the interweb, if you want to create your own server,
    then use these routes or modify them for your server.
*/

namespace PaintPower.Networking;

public class Routes
{
    // Server check routes
    public static string serverCheck()
    {
        return "api/servercheck/";
    }

    public static string checkActiveServer()
    {
        return serverCheck();
    }

    // Create a new project without uploading it.
    public static string createNew()
    {
        return "api/projects/create";
    }

    // Upload project routes
    public static string uploadNew() {
        return "api/projects/new/upload/paintfile/";
    }

    public static string uploadUpdate(string id)
    {
        return $"api/projects/{id}/upload/paintfile/";
    }

    // Download project routes

    public static string downloadProject(string id)
    {
        return $"api/projects/{id}/download";
    }

    // Mystuff routes
    public static string userProjectsRoute(string username = "")
    {
        if (username != string.Empty)
        {
            return $"api/list/projects/{username}";
        }
        return "api/mystuff/projects";
    }

    // For testing a custom server.
    public static string testServerListProjects()
    {
        return "api/listProjects/";
    }
}
