using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace UKHO.FileShareService.DesktopClient.Core
{
    static class TokenCacheHelper
    {
        static TokenCacheHelper()
        {
            try
            {
                // For packaged desktop apps (MSIX packages, also called desktop bridge) the executing assembly folder is read-only. 
                // In that case we need to use Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path + "\msalcache.bin" 
                // which is a per-app read/write folder for packaged apps.
                // See https://docs.microsoft.com/windows/msix/desktop/desktop-to-uwp-behind-the-scenes
                //  CacheFilePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, "msalcache.bin");
                CacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "msalcache.bin");
                // CacheFilePath = Path.Combine(@"D:\FSSTooling\TestPath", "msalcache.bin");
            }
            catch (System.InvalidOperationException)
            {
                // Fall back for an unpackaged desktop app
                CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + "msalcache.bin";
            }
        }

        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static string CacheFilePath { get; private set; }

        private static readonly object FileLock = new object();

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                        ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath),
                                                 null,
                                                 DataProtectionScope.CurrentUser)
                        : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(CacheFilePath,
                                       ProtectedData.Protect(args.TokenCache.SerializeMsalV3(),
                                                             null,
                                                             DataProtectionScope.CurrentUser)
                                      );
                }
            }
        }

        internal static void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }
    }
}
