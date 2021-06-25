using Microsoft.Win32;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient
{
    public class KeyValueStore : IKeyValueStore
    {
        private const string RegRoot = @"SOFTWARE\UKHO\FileShareServiceDesktopClient";

        public string? this[string key]
        {
            get
            {
                var regKey = Registry.CurrentUser.OpenSubKey(RegRoot);
                var value = regKey?.GetValue(key) as string;
                return value;
            }
            set
            {
                var regKey = Registry.CurrentUser.CreateSubKey(RegRoot);
                if (value != null)
                    regKey.SetValue(key, value);
            }
        }
    }
}