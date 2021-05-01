using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class SearchCriteriaViewModelTests
    {
        private SearchCriteriaViewModel searchCriteriaViewModel = null!;
        private IFssSearchStringBuilder fakeFssSearchStringBuilder = null!;

        [SetUp]
        public void Setup()
        {
            fakeFssSearchStringBuilder = A.Fake<IFssSearchStringBuilder>();
            searchCriteriaViewModel = new SearchCriteriaViewModel(fakeFssSearchStringBuilder);
        }

        [Test]
        public void TestAddSearchRow()
        {
            Assert.AreEqual(1, searchCriteriaViewModel.SearchCriteria.Count);
            searchCriteriaViewModel.AddNewCriterionCommand.Execute();
            Assert.AreEqual(2, searchCriteriaViewModel.SearchCriteria.Count);
            Assert.AreNotSame(searchCriteriaViewModel.SearchCriteria.First(),
                searchCriteriaViewModel.SearchCriteria.Last());
        }

        [Test]
        public void TestAddNewSearchRowAboveExistingEntry()
        {
            searchCriteriaViewModel.AddNewCriterionCommand.Execute();
            Assert.AreEqual(2, searchCriteriaViewModel.SearchCriteria.Count);

            var c0 = searchCriteriaViewModel.SearchCriteria[0];
            var c1 = searchCriteriaViewModel.SearchCriteria[1];

            c0.Value = "c0";
            c1.Value = "c1";
        }

        [Test]
        public void TestChangesToInitialCriterionFirePropertyChanged()
        {
            var firstSearchCriterionViewModel = searchCriteriaViewModel.SearchCriteria.First();
            searchCriteriaViewModel.AssertPropertyChanged(nameof(searchCriteriaViewModel.SearchCriteria),
                () =>
                {
                    firstSearchCriterionViewModel.SelectedField =
                        firstSearchCriterionViewModel.AvailableAttributes.First();
                });

            searchCriteriaViewModel.AssertPropertyChanged(nameof(searchCriteriaViewModel.SearchCriteria),
                () =>
                {
                    firstSearchCriterionViewModel.Operator = firstSearchCriterionViewModel.AvailableOperators.First();
                });

            searchCriteriaViewModel.AssertPropertyChanged(nameof(searchCriteriaViewModel.SearchCriteria),
                () => { firstSearchCriterionViewModel.Value = "Bob"; });
        }

        [Test]
        public void TestChangesToNewCriterionFirePropertyChanged()
        {
            searchCriteriaViewModel.AddNewCriterionCommand.Execute();
            var newSearchCriterionViewModel = searchCriteriaViewModel.SearchCriteria[1];

            searchCriteriaViewModel.AssertPropertyChanged(nameof(searchCriteriaViewModel.SearchCriteria),
                () =>
                {
                    newSearchCriterionViewModel.SelectedField =
                        newSearchCriterionViewModel.AvailableAttributes.First();
                });

            searchCriteriaViewModel.AssertPropertyChanged(nameof(searchCriteriaViewModel.SearchCriteria),
                () =>
                {
                    newSearchCriterionViewModel.Operator = newSearchCriterionViewModel.AvailableOperators.First();
                });

            searchCriteriaViewModel.AssertPropertyChanged(nameof(searchCriteriaViewModel.SearchCriteria),
                () => { newSearchCriterionViewModel.Value = "NewValue"; });
        }

        [Test]
        public void TestChangesToCriterionAfterItHasBeenRemovedDontFirePropertyChanged()
        {
            searchCriteriaViewModel.AddNewCriterionCommand.Execute();
            var newSearchCriterionViewModel = searchCriteriaViewModel.SearchCriteria[1];
            searchCriteriaViewModel.DeleteRowCommand.Execute(newSearchCriterionViewModel);

            searchCriteriaViewModel.AssertPropertyChangedNotFired(nameof(searchCriteriaViewModel.SearchCriteria),
                () =>
                {
                    newSearchCriterionViewModel.SelectedField =
                        newSearchCriterionViewModel.AvailableAttributes.First();
                });

            searchCriteriaViewModel.AssertPropertyChangedNotFired(nameof(searchCriteriaViewModel.SearchCriteria),
                () =>
                {
                    newSearchCriterionViewModel.Operator = newSearchCriterionViewModel.AvailableOperators.First();
                });

            searchCriteriaViewModel.AssertPropertyChangedNotFired(nameof(searchCriteriaViewModel.SearchCriteria),
                () => { newSearchCriterionViewModel.Value = "NewValue"; });
        }

        [Test]
        public void TestBuildSearchString()
        {
            A.CallTo(() => fakeFssSearchStringBuilder.BuildSearch(A<IEnumerable<ISearchCriterion>>.Ignored))
                .Returns("");
            Assert.AreEqual("", searchCriteriaViewModel.GetSearchString());


            A.CallTo(() =>
                    fakeFssSearchStringBuilder.BuildSearch(A<IEnumerable<ISearchCriterion>>.That.Matches(criteria =>
                        Equals(criteria, searchCriteriaViewModel.SearchCriteria))))
                .Returns("A Valid Search");
            Assert.AreEqual("A Valid Search", searchCriteriaViewModel.GetSearchString());
        }
    }
}