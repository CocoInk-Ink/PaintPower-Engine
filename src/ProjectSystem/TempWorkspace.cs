using System;
using System.IO;

namespace PaintPower.ProjectSystem;

public class TempWorkspace
{
    public string Root { get; }
    public string ItemsDir => Path.Combine(Root, "items");

    public string ActiveRoot { get; private set; }

    public void SetActiveRoot(string path)
    {
        ActiveRoot = path;
    }

    public TempWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "PaintPower_" + Guid.NewGuid());

        // Delete the directory if it already exists because TempWorkspace might be reused for multiple projects
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);

        Directory.CreateDirectory(Root);

        Directory.CreateDirectory(ItemsDir);
        ActiveRoot = ItemsDir; // default
    }

    public string MapToTemp(string projectRelativePath)
    {
        // If the path is already inside ActiveRoot, return it directly
        if (projectRelativePath.StartsWith(ActiveRoot, StringComparison.OrdinalIgnoreCase))
            return projectRelativePath;

        // If the path is absolute, return it
        if (Path.IsPathRooted(projectRelativePath))
            return projectRelativePath;

        return Path.Combine(ActiveRoot, projectRelativePath.Replace("/", "\\"));
    }

    public void ImportFile(string sourcePath, string relativePath)
    {
        string dest = MapToTemp(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(sourcePath, dest, overwrite: true);
    }

    public void SaveFile(string relativePath, string content)
    {
        string dest = MapToTemp(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, content);
    }

    public void SaveBinary(string relativePath, byte[] data)
    {
        string dest = MapToTemp(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllBytes(dest, data);
    }

    public byte[] LoadBinary(string relativePath)
    {
        string path = MapToTemp(relativePath);
        return File.Exists(path) ? File.ReadAllBytes(path) : Array.Empty<byte>();
    }

    public string LoadText(string relativePath)
    {
        string path = MapToTemp(relativePath);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    public void Delete(string relativePath)
    {
        string path = MapToTemp(relativePath);
        if (File.Exists(path))
            File.Delete(path);
    }

    public void Cleanup()
    {
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}