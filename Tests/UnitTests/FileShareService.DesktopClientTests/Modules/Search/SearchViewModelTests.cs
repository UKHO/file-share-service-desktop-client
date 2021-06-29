﻿using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class SearchViewModelTests
    {
        private SearchViewModel searchViewModel = null!;
        private IAuthProvider fakeAuthProvider = null!;
        private IFssSearchStringBuilder fakeFssSearchStringBuilder = null!;
        private IFileShareApiAdminClientFactory fakeFileShareApiAdminClientFactory = null!;

        [SetUp]
        public void Setup()
        {
            fakeAuthProvider = A.Fake<IAuthProvider>();
            fakeFssSearchStringBuilder = A.Fake<IFssSearchStringBuilder>();
            fakeFileShareApiAdminClientFactory = A.Fake<IFileShareApiAdminClientFactory>();
            searchViewModel =
                new SearchViewModel(fakeAuthProvider, fakeFssSearchStringBuilder, fakeFileShareApiAdminClientFactory);
        }

        [Test]
        public void TestSearchTextPropertyChanged()
        {
            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchText),
                () => searchViewModel.SearchText = "NewValue");
            Assert.AreEqual("NewValue", searchViewModel.SearchText);
        }

        [Test]
        public void TestSearchInProgressPropertyChanged()
        {
            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchInProgress),
                () => searchViewModel.SearchInProgress = true);
            Assert.IsTrue(searchViewModel.SearchInProgress);
            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchInProgress),
                () => searchViewModel.SearchInProgress = false);
            Assert.IsFalse(searchViewModel.SearchInProgress);
            searchViewModel.AssertPropertyChangedNotFired(nameof(searchViewModel.SearchInProgress),
                () => searchViewModel.SearchInProgress = false);
        }

        [Test]
        public void TestSearchResultPropertyChanged()
        {
            var batchSearchResponse1 = new BatchSearchResponse(1);
            var batchSearchResponse2 = new BatchSearchResponse(2);
            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchResult),
                () => searchViewModel.SearchResult = batchSearchResponse1);
            Assert.AreSame(batchSearchResponse1, searchViewModel.SearchResult);

            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchResult),
                () => searchViewModel.SearchResult = batchSearchResponse2);
            Assert.AreSame(batchSearchResponse2, searchViewModel.SearchResult);

            searchViewModel.AssertPropertyChangedNotFired(nameof(searchViewModel.SearchResult),
                () => searchViewModel.SearchResult = batchSearchResponse2);
        }

        [Test]
        public void TestSearchCriteriaChangesUpdateSearchText()
        {
            A.CallTo(() => fakeFssSearchStringBuilder.BuildSearch(A<IEnumerable<ISearchCriterion>>.Ignored))
                .Returns("").Twice().Then.Returns("New Search Text");

            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchText), () =>
            {
                var firstSearchCriterionViewModel = searchViewModel.SearchCriteria.SearchCriteria.First();
                firstSearchCriterionViewModel.SelectedField = firstSearchCriterionViewModel.AvailableAttributes.First();
                firstSearchCriterionViewModel.Operator = firstSearchCriterionViewModel.AvailableOperators.First();
                firstSearchCriterionViewModel.Value = "Bob";
            });
        }
    }
}