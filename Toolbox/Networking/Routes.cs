/*
    File containing routes for the interweb, if you want to create your own server,
    then use these routes or modify them for your server.
*/

namespace Toolbox.Networking;

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

    public static string normalOverwriteUpload(string id)
    {
        return $"api/upload/projects/{id}/";
    }

    // Upload project routes
    public static string uploadNew() {
        return "api/projects/new/upload/paintfile/";
    }

    public static string uploadUpdate(string id)
    {
        return $"api/projects/{id}/upload/paintfile/";
    }

    // Project version routes
    public static string createVersion(string projectId)
    {
        return $"api/projects/{projectId}/versions";
    }

    public static string listVersions(string projectId)
    {
        return createVersion(projectId);
    }

    public static string updateVersionSource(string projectId, string versionId)
    {
        return $"api/projects/{projectId}/versions/{versionId}/source";
    }

    public static string deleteVersion(string projectId, string versionId)
    {
        return $"api/projects/{projectId}/versions/{versionId}";
    }

    public static string updateVersionBuild(string projectId, string versionId)
    {
        return $"api/projects/{projectId}/versions/{versionId}/build";
    }

    public static string selectVersion(string projectId, string versionId)
    {
        return $"api/projects/{projectId}/versions/{versionId}/select";
    }

    // Download project routes

    public static string downloadProject(string id)
    {
        return $"api/projects/{id}/download";
    }

    public static string downloadLatestSource(string projectId)
    {
        return $"api/projects/{projectId}/file/latest/source";
    }

    public static string downloadVersionSource(string projectId, string versionId)
    {
        return $"api/projects/{projectId}/versions/{versionId}/source";
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
