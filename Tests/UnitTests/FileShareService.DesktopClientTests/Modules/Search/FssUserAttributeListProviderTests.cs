using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareAdminClient;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class FssUserAttributeListProviderTests
    {
        private FssUserAttributeListProvider fssUserAttributeListProvider = null!;
        private IFileShareApiAdminClientFactory fakeFileShareApiAdminClientFactory = null!;
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;

        [SetUp]
        public void Setup()
        {
            fakeFileShareApiAdminClientFactory = A.Fake<IFileShareApiAdminClientFactory>();
            fssUserAttributeListProvider = new FssUserAttributeListProvider(fakeFileShareApiAdminClientFactory);

            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
        }

        [Test]
        public async Task TestGetAttributes()
        {
            var expectedAttributes = new List<string> {"Attr1", "Attr2"};
            A.CallTo(() => fakeFileShareApiAdminClient.GetUserAttributesAsync())
                .Returns(expectedAttributes);

            var results = await fssUserAttributeListProvider.GetAttributesAsync();

            Assert.AreSame(expectedAttributes, results);
        }
    }
}