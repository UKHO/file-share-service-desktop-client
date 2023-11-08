using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Admin;

namespace UKHO.FileShareService.DesktopClient.Modules.AdHocUpload
{
    public class AdHocUploadPageButton : AdminPageButton
    {
        public override string DisplayName => "Ad-hoc Upload";
        public override string NavigationTarget => NavigationTargets.AdHocUpload;

        public AdHocUploadPageButton(IAuthProvider authProvider) : base(authProvider)
        {
        }
    }
}