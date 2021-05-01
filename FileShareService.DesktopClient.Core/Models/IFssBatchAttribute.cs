namespace UKHO.FileShareService.DesktopClient.Core.Models
{
    public interface IFssBatchAttribute
    {
        AttributeType Type { get; }
        string AttributeName { get; }
    }
}