using System.ComponentModel;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class SearchPageButtonTests
    {
        private SearchPageButton searchButton = null!;
        private IAuthProvider fakeAuthProvider = null!;

        [SetUp]
        public void Setup()
        {
            fakeAuthProvider = A.Fake<IAuthProvider>();
            searchButton = new SearchPageButton(fakeAuthProvider);
        }

        [Test]
        public void TestSearchPageButtonPropertyValues()
        {
            Assert.AreEqual("Search", searchButton.DisplayName);
            Assert.AreEqual(NavigationTargets.Search, searchButton.NavigationTarget);
        }

        [Test]
        public void TestEnabledChangedWithAuth()
        {
            Assert.IsFalse(searchButton.Enabled);

            A.CallTo(() => fakeAuthProvider.IsLoggedIn).Returns(true);
            searchButton.AssertPropertyChanged(nameof(searchButton.Enabled),
                () => fakeAuthProvider.PropertyChanged +=
                        Raise.FreeForm<PropertyChangedEventHandler>.With(fakeAuthProvider,
                            new PropertyChangedEventArgs(nameof(fakeAuthProvider.IsLoggedIn))
                        ));

            Assert.IsTrue(searchButton.Enabled);
        }
    }
}