namespace PaintPower.Templates;
public class Template
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ThumbnailPath { get; set; }

    public Template(string name, string description, string thumbnailPath)
    {
        Name = name;
        Description = description;
        ThumbnailPath = thumbnailPath;
    }
}