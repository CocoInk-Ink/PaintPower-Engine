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
            // Log.QuickLog(body);

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

    public static async Task<string?> PerformDeleteRequest(string url)
    {
        try
        {
            using var response = await client.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Log.QuickLog($"DELETE request error: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------
    // File Download
    // ------------------------------------------------------------
    public static async Task<bool> DownloadFileAsync(string url, string destinationPath)
    {
        string? temporaryPath = null;
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            string fullDestinationPath = Path.GetFullPath(destinationPath);
            string destinationDirectory = Path.GetDirectoryName(fullDestinationPath)!;
            temporaryPath = Path.Combine(
                destinationDirectory,
                $"{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.download"
            );
            Log.QuickLog($"Downloading file to temporary path: {temporaryPath}");
            await using var fileStream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None
            );

            byte[] buffer = new byte[81920];
            int read;

            Log.QuickLog($"Starting download from {url} to {fullDestinationPath}");

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                await fileStream.WriteAsync(buffer, 0, read);

            Log.QuickLog($"Download complete. Moving file to final destination: {fullDestinationPath}");

            await fileStream.FlushAsync();
            await fileStream.DisposeAsync();

            Log.QuickLog($"Moving temporary file to final destination: {fullDestinationPath}");

            Log.QuickLog($"Temp exists: {File.Exists(temporaryPath)}");
            Log.QuickLog($"Dest exists: {File.Exists(fullDestinationPath)}");

            // Move the temporary file to the final destination
            try
            {
                File.Move(temporaryPath, fullDestinationPath, true);
            }
            catch (IOException ex)
            {
                Log.QuickLog($"Move failed: {ex}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.QuickLog($"Download error: {ex.Message}");
            return false;
        }
        finally
        {
            if (temporaryPath != null && File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception ex)
                {
                    Log.QuickLog($"Download cleanup error: {ex.Message}");
                }
            }
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

    public static async Task<string?> UploadFilesAsync(
        string url,
        IReadOnlyDictionary<string, string> files,
        IReadOnlyDictionary<string, string>? fields = null)
    {
        var streams = new List<FileStream>();
        try
        {
            using var form = new MultipartFormDataContent();
            foreach (var (fieldName, filePath) in files)
            {
                var fileStream = File.OpenRead(filePath);
                streams.Add(fileStream);

                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, fieldName, Path.GetFileName(filePath));
            }

            if (fields != null)
            {
                foreach (var (fieldName, value) in fields)
                    form.Add(new StringContent(value, Encoding.UTF8), fieldName);
            }

            using var response = await client.PostAsync(url, form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Log.QuickLog($"File upload error: {ex.Message}");
            return null;
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
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
