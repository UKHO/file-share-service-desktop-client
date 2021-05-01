namespace UKHO.FileShareService.DesktopClient.Core.Models
{
    public interface ISearchCriterion
    {
        AndOr And { get; }
        IFssBatchAttribute? SelectedFssAttribute { get; }
        Operators? Operator { get; }
        string Value { get; }
    }
}