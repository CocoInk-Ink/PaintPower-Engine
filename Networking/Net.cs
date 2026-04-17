/*
    Actual networking
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PaintPower;

class Net
{
    // Shared HttpClient instance (recommended for performance)
    private static readonly HttpClient client = new HttpClient();

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
            Console.WriteLine("GET Response:");
            Console.WriteLine(responseBody);
            return responseBody;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"GET request error: {e.Message}");
            return null;
        }
    }

    // POST request method
    public static async Task PerformPostRequest<T>(string url, T data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("POST Response:");
            Console.WriteLine(responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"POST request error: {e.Message}");
        }
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
}
