#pragma warning disable

namespace PaintPower;
public class ProjectInfo
{
    public string id { get; set; }
    public string title { get; set; }
    public override string ToString()
    {
        return $"{title}          ||          project id:{id}";
    }
}
