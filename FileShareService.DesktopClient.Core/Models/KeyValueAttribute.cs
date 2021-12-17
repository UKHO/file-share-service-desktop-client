namespace UKHO.FileShareService.DesktopClient.Core.Models
{
    public class KeyValueAttribute
    {
        public KeyValueAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
