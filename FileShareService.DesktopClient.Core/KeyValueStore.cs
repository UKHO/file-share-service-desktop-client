namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface IKeyValueStore
    {
        string? this[string key] { get; set; }
    }
}