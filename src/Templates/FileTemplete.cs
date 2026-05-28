namespace PaintPower.Templates;

public class FileTemplate
{
    public FileTemplate() {}
    public string? filetype;
    public string? name;
    public string? description;

    public string category = "General";

    public bool isCustom = false;

    public override string ToString()
    {
        return  isCustom ? "Your custom file type" : $"{name} ({filetype}): {description}";
    }
}