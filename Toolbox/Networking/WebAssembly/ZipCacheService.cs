// ZipCacheService.cs

// Use for in the browser only

#if BROWSER

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Toolbox.Networking.WebAssembly;

// <summary>
// A service that downloads a ZIP file from a URL, extracts it, and caches the files in the browser's storage.
// Used for caching assets in the WebAssembly version of the VM.
// </summary>

public class ZipCacheService
{
    private readonly IStorageProvider _storage;
    private readonly HttpClient _http;

    public ZipCacheService(TopLevel topLevel)
    {
        _storage = topLevel.StorageProvider ?? throw new ArgumentNullException(nameof(topLevel.StorageProvider));
        _http = new HttpClient();
    }

    // ------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------

    public async Task InitializeAsync(string zipUrl, string version)
    {
        string? cachedVersion = await ReadTextSafeAsync("zip_version.txt");

        if (cachedVersion == version)
        {
            // Already cached, nothing to do
            return;
        }

        // Download ZIP
        byte[] zipBytes = await DownloadZipAsync(zipUrl);

        // Extract + cache
        await ExtractAndCacheAsync(zipBytes);

        // Save version
        await _storage.SaveTextAsync("zip_version.txt", version);
    }

    public async Task<string?> LoadTextFileAsync(string fileName)
    {
        if (!await _storage.FileExistsAsync(fileName))
            return null;

        using var stream = await _storage.OpenFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<Stream?> LoadBinaryFileAsync(string fileName)
    {
        if (!await _storage.FileExistsAsync(fileName))
            return null;

        return await _storage.OpenFileAsync(fileName);
    }

    // ------------------------------------------------------------
    // INTERNAL HELPERS
    // ------------------------------------------------------------

    private async Task<byte[]> DownloadZipAsync(string url)
    {
        return await _http.GetByteArrayAsync(url);
    }

    private async Task ExtractAndCacheAsync(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

        foreach (var entry in zip.Entries)
        {
            using var entryStream = entry.Open();
            using var msEntry = new MemoryStream();
            await entryStream.CopyToAsync(msEntry);
            msEntry.Position = 0;

            await _storage.SaveFileAsync(entry.FullName, msEntry);
        }
    }

    private async Task<string?> ReadTextSafeAsync(string fileName)
    {
        if (!await _storage.FileExistsAsync(fileName))
            return null;

        using var stream = await _storage.OpenFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

#endif