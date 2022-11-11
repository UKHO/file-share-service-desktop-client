using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareAdminClient.Models.Response;
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
        private IEnvironmentsManager fakeEnvironmentsManager = null!;
        private IFssUserAttributeListProvider fakeFssUserAttributeListProvider = null!;
        private IMessageBoxService fakeMessageBoxService = null!;
        private IFileService fakeFileService = null!;
        private ISaveFileDialogService fakesaveFileDialogService = null!;
        private ILogger<SearchViewModel> fakeLoggerSearchVM = null!;
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private IEventAggregator fakeEventAggregator = null!;
        


        [SetUp]
        public void Setup()
        {
            fakeAuthProvider = A.Fake<IAuthProvider>();
            fakeFssSearchStringBuilder = A.Fake<IFssSearchStringBuilder>();
            fakeFileShareApiAdminClientFactory = A.Fake<IFileShareApiAdminClientFactory>();
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            fakeFssUserAttributeListProvider = A.Fake<IFssUserAttributeListProvider>();
            fakeMessageBoxService = A.Fake<IMessageBoxService>();
            fakeFileService = A.Fake<IFileService>();
            fakesaveFileDialogService = A.Fake<ISaveFileDialogService>();
            fakeLoggerSearchVM = A.Fake<ILogger<SearchViewModel>>();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakeEventAggregator = A.Fake<IEventAggregator>();
            
            searchViewModel =
                new SearchViewModel(fakeAuthProvider, fakeFssSearchStringBuilder, fakeFileShareApiAdminClientFactory,
                    fakeFssUserAttributeListProvider, fakeEnvironmentsManager, fakeMessageBoxService, fakeFileService,fakesaveFileDialogService,fakeLoggerSearchVM, fakeEventAggregator);
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

        [Test]
        public void TestSearchCountSummary()
        {
            //If Search count is 0
            var batchSearchResponse1 = new BatchSearchResponse(total: 0);
            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchResult),
                () => searchViewModel.SearchResult = batchSearchResponse1);
            Assert.AreSame(batchSearchResponse1, searchViewModel.SearchResult);
            Assert.AreSame("No batches found.", searchViewModel.SearchCountSummary);

            //If Search count is greater than 0
            var batchSearchResponse2 = new BatchSearchResponse(10, 25);
            searchViewModel.AssertPropertyChanged(nameof(searchViewModel.SearchResult),
                () => searchViewModel.SearchResult = batchSearchResponse2);
            Assert.AreSame(batchSearchResponse2, searchViewModel.SearchResult);
            Assert.AreEqual("Showing 1-10 of 25", searchViewModel.SearchCountSummary);
        }

        [Test]
        public void TestExecuteSearchForOKResponseWithCancellation()
        {
            var searchVM = new SearchViewModel(fakeAuthProvider, fakeFssSearchStringBuilder, fakeFileShareApiAdminClientFactory,
                                                fakeFssUserAttributeListProvider, fakeEnvironmentsManager, fakeMessageBoxService,
                                                fakeFileService, fakesaveFileDialogService, fakeLoggerSearchVM, fakeEventAggregator);

            var expectedResult = new BatchSearchResponse
            {
                Count = 2,
                Total = 2,
                Entries = new List<BatchDetails>
                {
                    new BatchDetails("batch1"), new BatchDetails("batch2")
                },
                Links = new Links(new Link("self"))
            };
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<BatchSearchResponse> { Data = expectedResult, StatusCode = 200, IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<SetExpiryDateResponse> { StatusCode = 400, IsSuccess = false });

            searchVM.SearchCommand.Execute();

            Assert.AreEqual(expectedResult.Total, searchVM.SearchResult?.Total);
            Assert.AreEqual(expectedResult.Count, searchVM.SearchResult?.Count);
            Assert.AreEqual(expectedResult.Entries.Count, searchVM.SearchResult?.Entries.Count);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public void TestExecuteSearchForBadRequesteWithCancellation()
        {
            var searchVM = new SearchViewModel(fakeAuthProvider, fakeFssSearchStringBuilder, fakeFileShareApiAdminClientFactory,
                                                fakeFssUserAttributeListProvider, fakeEnvironmentsManager, fakeMessageBoxService,
                                                fakeFileService, fakesaveFileDialogService, fakeLoggerSearchVM, fakeEventAggregator);

            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<BatchSearchResponse> { StatusCode = 400, IsSuccess = false });

            searchVM.SearchCommand.Execute();

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).MustHaveHappened();
        }

        [Test]
        public void TestExecuteSearchForInternalServerErroreWithCancellation()
        {
            var searchVM = new SearchViewModel(fakeAuthProvider, fakeFssSearchStringBuilder, fakeFileShareApiAdminClientFactory,
                                                fakeFssUserAttributeListProvider, fakeEnvironmentsManager, fakeMessageBoxService,
                                                fakeFileService, fakesaveFileDialogService, fakeLoggerSearchVM, fakeEventAggregator);

            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<BatchSearchResponse> { StatusCode = 500, IsSuccess = false });

            searchVM.SearchCommand.Execute();

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).MustHaveHappened();
        }

        [Test]
        [TestCase(HttpStatusCode.Forbidden, false, TestName = "WhenSetExpiryDateResponseIsForbiddenThenUserCanNotSetBatchExpiryDate")]
        [TestCase(HttpStatusCode.BadRequest, true, TestName = "WhenSetExpiryDateResponseIsBadRequestThenUserCanSetBatchExpiryDate")]
        public void TestExecuteSearchForSetExpiryDateResponseWithCancellation(HttpStatusCode setExpiryDateResponseStatusCode, bool expectedCanSetBatchExpiryDate)
        {
            var searchVM = new SearchViewModel(fakeAuthProvider, fakeFssSearchStringBuilder, fakeFileShareApiAdminClientFactory,
                                                fakeFssUserAttributeListProvider, fakeEnvironmentsManager, fakeMessageBoxService,
                                                fakeFileService, fakesaveFileDialogService, fakeLoggerSearchVM, fakeEventAggregator);

            var expectedResult = new BatchSearchResponse
            {  
                Entries = new List<BatchDetails>
                {
                    new BatchDetails("batch1"), new BatchDetails("batch2")
                }
            };
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<BatchSearchResponse> { Data = expectedResult, StatusCode = 200, IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<SetExpiryDateResponse> { StatusCode = (int)setExpiryDateResponseStatusCode, IsSuccess = false });

            searchVM.SearchCommand.Execute();

            Assert.True(searchVM.BatchDetailsVM?.All(b => b.CanSetBatchExpiryDate == expectedCanSetBatchExpiryDate));
        }       
    }
}