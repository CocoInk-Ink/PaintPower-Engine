#pragma warning disable

namespace Toolbox.Networking;
public class ProjectInfo
{
    public string id { get; set; }
    public string title { get; set; }
    public override string ToString()
    {
        return $"{title}          ||          project id:{id}";
    }
}
