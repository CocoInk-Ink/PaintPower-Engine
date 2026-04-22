/*
    Actual networking
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PaintPower;
using PaintPower.Logging;

public class Net
{
    private static readonly HttpClientHandler handler = new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
        AllowAutoRedirect = true
    };

    // Shared HttpClient instance (recommended for performance)
    private static readonly HttpClient client = new HttpClient(handler);

    private static async Task<string?> getCSRF_Token()
    {
        return PaintPower_Engine.App.server.CurrentDomain?.CSRF_Token;
    }

    // GET request method
    public static async Task<string?> PerformGetRequest(string url)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode(); // Throws if not 2xx

            string responseBody = await response.Content.ReadAsStringAsync();
            Log.QuickLog("GET Response:");
            Log.QuickLog(responseBody);
            return responseBody;
        }
        catch (HttpRequestException e)
        {
            Log.QuickLog($"GET request error: {e.Message}");
            return null;
        }
    }

    // POST request method
    public static async Task<object?> PerformPostRequest<T>(string url, T data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Log.QuickLog($"Body: {content}");

            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Log.QuickLog("POST Response:");
            Log.QuickLog(responseBody);

            return responseBody;
        }
        catch (HttpRequestException e)
        {
            Log.QuickLog($"POST request error: {e.Message}");
        }
        return null;
    }

    public static async Task DownloadFileAsync(string url, string destinationPath)
    {
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(destinationPath);

            byte[] buffer = new byte[81920]; // 80 KB chunks
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download error: {ex.Message}");
        }
    }

    public static async Task UploadFileAsync(string url, string filePath, string projectTitle)
    {
        Debug.WriteLine(url);

        try
        {
            using var form = new MultipartFormDataContent();
            await using var fileStream = File.OpenRead(filePath);

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            // If project is not saved on disk, then get a random directory, save in in this random directory.
            // Save the project, upload it, then put it back to normal.

            if (filePath == string.Empty)
            {
                string tempFolder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
                string tempFile = Path.Join(tempFolder, Guid.NewGuid().ToString() + ".temp.xPaint");
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }
                await PaintPower_Engine.App._project.SaveToDisk(tempFile);
                filePath = tempFile;
            }

            // File field (multer expects "file")
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            // Add project title
            form.Add(new StringContent(projectTitle, Encoding.UTF8), "title");

            using var response = await client.PostAsync(url, form);
            response.EnsureSuccessStatusCode();

            Debug.WriteLine("Upload complete.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Upload error: {ex.Message}");
        }
    }

    public static async Task<bool> Login(string username, string password)
    {
        try
        {
            var data = new Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };

            var content = new FormUrlEncodedContent(data);

            var response = await client.PostAsync(
                PaintPower_Engine.App.server.makeUrl("login"),
                content
            );

            Log.QuickLog($"Login status: {response.GetHashCode()}");

            // If login fails, server returns 400 with text
            if (!response.IsSuccessStatusCode)
                return false;

            // If login succeeds, server redirects to "/"
            return true;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Login error: {ex.Message}");
            return false;
        }
    }
}
