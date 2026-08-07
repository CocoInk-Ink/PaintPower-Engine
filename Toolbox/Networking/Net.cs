using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Toolbox.Logging;

public static class Net
{
    private static readonly HttpClientHandler handler = new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
        AllowAutoRedirect = true
    };

    private static readonly HttpClient client = new HttpClient(handler);

    // ------------------------------------------------------------
    // GET
    // ------------------------------------------------------------
    public static async Task<string?> PerformGetRequest(string url)
    {
        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();
            Log.QuickLog("GET Response:");
            Log.QuickLog(body);

            return body;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"GET request error: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------
    // POST (JSON)
    // ------------------------------------------------------------
    public static async Task<string?> PerformPostRequest<T>(string url, T data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Log.QuickLog($"POST Body: {json}");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();
            Log.QuickLog("POST Response:");
            Log.QuickLog(body);

            return body;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"POST request error: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------
    // File Download
    // ------------------------------------------------------------
    public static async Task<bool> DownloadFileAsync(string url, string destinationPath)
    {
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(destinationPath);

            byte[] buffer = new byte[81920];
            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                await fileStream.WriteAsync(buffer, 0, read);

            return true;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Download error: {ex.Message}");
            return false;
        }
    }

    // ------------------------------------------------------------
    // File Upload
    // ------------------------------------------------------------
    public static async Task<bool> UploadFileAsync(string url, string filePath, string projectTitle)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            await using var fileStream = File.OpenRead(filePath);

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            form.Add(fileContent, "file", Path.GetFileName(filePath));
            form.Add(new StringContent(projectTitle, Encoding.UTF8), "title");

            var response = await client.PostAsync(url, form);
            response.EnsureSuccessStatusCode();

            Debug.WriteLine("Upload complete.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Upload error: {ex.Message}");
            return false;
        }
    }

    // ------------------------------------------------------------
    // Login
    // ------------------------------------------------------------
    public static async Task<bool> Login(string url, string username, string password)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password }
            };

            var content = new FormUrlEncodedContent(data);
            var response = await client.PostAsync(url, content);

            Log.QuickLog($"Login status: {response.StatusCode}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Login error: {ex.Message}");
            return false;
        }
    }
}
