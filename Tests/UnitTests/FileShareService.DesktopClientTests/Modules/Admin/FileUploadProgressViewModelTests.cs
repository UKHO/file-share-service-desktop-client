using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    public class FileUploadProgressViewModelTests
    {
        [Test]
        public void TestDisplayString()
        {
            var vm = new FileUploadProgressViewModel("File1.txt");
            Assert.AreEqual(string.Empty, vm.DisplayString);

            vm.TotalBlocks = 3;
            Assert.AreEqual("0%", vm.DisplayString);

            vm.CompleteBlocks = 1;
            Assert.AreEqual("33%", vm.DisplayString);

            vm.CompleteBlocks = 2;
            Assert.AreEqual("67%", vm.DisplayString);

            vm.CompleteBlocks = 3;
            Assert.AreEqual("100%", vm.DisplayString);
        }
    }
}