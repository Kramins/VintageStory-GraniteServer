namespace GraniteServer.Api.Models;

public class PlayerDTO
{
    public object Name { get; internal set; }
    public object Id { get; internal set; }
    public bool IsAdmin { get; internal set; }
    public string IpAddress { get; internal set; }
    public string LanguageCode { get; internal set; }
    public float Ping { get; internal set; }
    public string RolesCode { get; internal set; }
    public string FirstJoinDate { get; internal set; }
    public string LastJoinDate { get; internal set; }
    public string[] Privileges { get; internal set; }
}
