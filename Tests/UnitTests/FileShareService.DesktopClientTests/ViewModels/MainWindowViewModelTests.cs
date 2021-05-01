using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.ViewModels;

namespace FileShareService.DesktopClientTests.ViewModels
{
    public class MainWindowViewModelTests
    {
        private IEnvironmentsManager fakeEnvironmentsManager = null!;
        private MainWindowViewModel vm = null!;
        private IList<EnvironmentConfig> environments = null!;

        [SetUp]
        public void Setup()
        {
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            vm = new MainWindowViewModel(fakeEnvironmentsManager);

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
                            new PropertyChangedEventArgs("Aspects"));
                });

            Assert.AreSame(fakeEnvironmentsManager.CurrentEnvironment, vm.CurrentEnvironment);
        }
    }
}