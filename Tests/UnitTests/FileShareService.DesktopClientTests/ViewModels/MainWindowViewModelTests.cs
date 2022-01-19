using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.ViewModels;
using Unity;

namespace FileShareService.DesktopClientTests.ViewModels
{
    public class MainWindowViewModelTests
    {
        private IEnvironmentsManager fakeEnvironmentsManager = null!;
        private MainWindowViewModel vm = null!;
        private IList<EnvironmentConfig> environments = null!;
        private List<IPageButton> pageButtons = null!;
        private IAuthProvider fakeAuthProvider = null!;
        private IUnityContainer fakeContainerRegistry = null!;
        private INavigation fakeNavigation = null!;
        private ICurrentDateTimeProvider fakecurrentDateTimeProvider = null!;

        [SetUp]
        public void Setup()
        {
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            pageButtons = new List<IPageButton>();
            fakeAuthProvider = A.Fake<IAuthProvider>();
            fakeContainerRegistry = A.Fake<IUnityContainer>();
            fakeNavigation = A.Fake<INavigation>();
            vm = new MainWindowViewModel(fakeEnvironmentsManager, fakeContainerRegistry, fakeAuthProvider,
                fakeNavigation, fakecurrentDateTimeProvider);

            environments = new List<EnvironmentConfig>();

#pragma warning disable 8620
            A.CallTo(() => fakeEnvironmentsManager.Environments)
                .Returns(environments as IReadOnlyList<EnvironmentConfig>);
#pragma warning restore 8620
        }

        [Test]
        public void TestEnvironmentsFromEnvManager()
        {
            environments.Add(new EnvironmentConfig {Name = "Env1"});
            environments.Add(new EnvironmentConfig {Name = "Env2"});
            environments.Add(new EnvironmentConfig {Name = "Env3"});

            Assert.AreSame(environments, vm.Environments);
        }

        [Test]
        public void TestPropertyChangeForCurrentEnvironment()
        {
            environments.Add(new EnvironmentConfig {Name = "Env1"});
            environments.Add(new EnvironmentConfig {Name = "Env2"});
            environments.Add(new EnvironmentConfig {Name = "Env3"});

            vm.AssertPropertyChanged(nameof(vm.CurrentEnvironment), () => vm.CurrentEnvironment = environments.First());
            vm.AssertPropertyChanged(nameof(vm.CurrentEnvironment), () => vm.CurrentEnvironment = environments.Last());
            vm.AssertPropertyChangedNotFired(nameof(vm.CurrentEnvironment),
                () => vm.CurrentEnvironment = environments.Last());
            Assert.AreSame(environments.Last(), vm.CurrentEnvironment);
            Assert.AreSame(fakeEnvironmentsManager.CurrentEnvironment, vm.CurrentEnvironment);
        }

        [Test]
        public void TestPropertyChangeFiredWhenEnvManagerCurrentEnvChanged()
        {
            environments.Add(new EnvironmentConfig {Name = "Env1"});
            environments.Add(new EnvironmentConfig {Name = "Env2"});
            environments.Add(new EnvironmentConfig {Name = "Env3"});


            vm.AssertPropertyChanged(nameof(vm.CurrentEnvironment),
                () =>
                {
                    fakeEnvironmentsManager.PropertyChanged +=
                        Raise.FreeForm<PropertyChangedEventHandler>.With(fakeEnvironmentsManager,
                            new PropertyChangedEventArgs(nameof(fakeEnvironmentsManager.CurrentEnvironment)));
                });

            Assert.AreSame(fakeEnvironmentsManager.CurrentEnvironment, vm.CurrentEnvironment);
        }

        [Test]
        public void TestPageButtons()
        {
            CollectionAssert.IsEmpty(vm.PageButtons);


            pageButtons.Add(A.Fake<IPageButton>());
            pageButtons.Add(A.Fake<IPageButton>());
            pageButtons.Add(A.Fake<IPageButton>());

            A.CallTo(() => fakeContainerRegistry.Resolve(typeof(IPageButton).MakeArrayType(), null))
                .Returns(pageButtons);

            vm.AssertPropertyChanged(nameof(vm.PageButtons),
                () =>
                {
                    fakeAuthProvider.PropertyChanged +=
                        Raise.FreeForm<PropertyChangedEventHandler>.With(fakeAuthProvider,
                            new PropertyChangedEventArgs(nameof(fakeAuthProvider.IsLoggedIn)));
                });

            CollectionAssert.AreEqual(pageButtons, vm.PageButtons);
        }

        [Test]
        public void TestPageButtonCommand()
        {
            var fakePageButton = A.Fake<IPageButton>();
            A.CallTo(() => fakePageButton.NavigationTarget).Returns("NavTarget1");
            vm.PageButtonCommand.Execute(fakePageButton);
            A.CallTo(() => fakeNavigation.RequestNavigate("NavTarget1")).MustHaveHappened();
        }
    }
}