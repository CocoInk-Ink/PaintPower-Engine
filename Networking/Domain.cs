using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaintPower.Networking;

// A class for a domain.
public class Domain
{
    public bool IsConnected { get; set; }
    public bool IsDisconnected { get; set; }
    public bool IsAllowed { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public string Protocol = string.Empty;
    public string Host = string.Empty; public static string Port = string.Empty;
    public string? Username = string.Empty;
    public string? Password = string.Empty;
    public string domain = string.Empty;
    public string CSRF_Token = string.Empty;

    public Domain(string nDomain, string nProtocol = "http", string nName = "", string nHost = "", string nPort = "") {
        // Initalize domain
        IsAllowed = false;
        IsConnected = false;
        IsDisconnected = false;
        Name = nName;
        Host = nHost;
        Port = nPort;
        domain = nDomain;
        Protocol = nProtocol;
    }

    public override string ToString()
    {
        return $"Domain: {Name} ({Protocol}://{Host}:{Port})";
    }
}
