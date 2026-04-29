namespace IISApp.Services;

public enum FrontendAccessMode
{
    ReadOnly,
    FullAccess
}

public sealed class PermissionService
{
    public PermissionService(FrontendAccessMode mode) => Mode = mode;

    public FrontendAccessMode Mode { get; }
    public bool CanRead => true;
    public bool CanWrite => Mode == FrontendAccessMode.FullAccess;

    public string DeniedMessage => "This action is disabled in ReadOnly mode. Switch AccessMode=FullAccess in App.config.";
}
