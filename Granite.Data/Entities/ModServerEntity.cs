namespace GraniteServer.Data.Entities;

public class ModServerEntity
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public Guid ModId { get; set; }
    public Guid? InstalledReleaseId { get; set; }
    public Guid? RunningReleaseId { get; set; }

    // Navigation properties
    public ServerEntity? Server { get; set; }
    public ModEntity? Mod { get; set; }
    public ModReleaseEntity? InstalledRelease { get; set; }
    public ModReleaseEntity? RunningRelease { get; set; }
}
