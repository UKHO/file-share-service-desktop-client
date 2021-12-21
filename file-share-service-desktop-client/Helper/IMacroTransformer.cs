namespace UKHO.FileShareService.DesktopClient.Helper
{
    public interface IMacroTransformer
    {
        string ExpandMacros(string value);
    }
}
