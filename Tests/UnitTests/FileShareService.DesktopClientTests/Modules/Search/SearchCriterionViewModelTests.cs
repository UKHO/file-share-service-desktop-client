using System.Collections.Generic;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class SearchCriterionViewModelTests
    {
        private SearchCriterionViewModel searchCriterionViewModel = null!;
        private ISearchCriteriaViewModel fakeSearchCriteriaViewModel = null!;

        [SetUp]
        public void Setup()
        {
            fakeSearchCriteriaViewModel = A.Fake<ISearchCriteriaViewModel>();
            searchCriterionViewModel = new SearchCriterionViewModel(fakeSearchCriteriaViewModel);
        }

        [Test]
        public void TestAndPropertyFiresPropertyChanged()
        {
            Assert.AreEqual(AndOr.And, searchCriterionViewModel.And);
            searchCriterionViewModel.AssertPropertyChanged(nameof(searchCriterionViewModel.And),
                () => searchCriterionViewModel.And = AndOr.Or);
            searchCriterionViewModel.AssertPropertyChangedNotFired(nameof(searchCriterionViewModel.And),
                () => searchCriterionViewModel.And = AndOr.Or);
            Assert.AreEqual(AndOr.Or, searchCriterionViewModel.And);
            searchCriterionViewModel.AssertPropertyChanged(nameof(searchCriterionViewModel.And),
                () => searchCriterionViewModel.And = AndOr.And);
            Assert.AreEqual(AndOr.And, searchCriterionViewModel.And);
        }

        [Test]
        public void TestAvailableAttributes()
        {
            A.CallTo(() => fakeSearchCriteriaViewModel.AvailableAttributes).Returns(new List<Attribute>());

            var expectedAttributes = fakeSearchCriteriaViewModel.AvailableAttributes;
            Assert.AreSame(expectedAttributes, searchCriterionViewModel.AvailableAttributes);
        }

        [Test]
        public void TestSelectedFieldPropertyFiresPropertyChanged()
        {
            Assert.IsNull(searchCriterionViewModel.SelectedField);

            searchCriterionViewModel.AssertPropertiesChanged(
                () => searchCriterionViewModel.SelectedField =
                    new Attribute("newAttr", AttributeType.UserAttributeString),
                nameof(searchCriterionViewModel.SelectedField),
                nameof(searchCriterionViewModel.SelectedFssAttribute),
                nameof(searchCriterionViewModel.AvailableOperators)
            );

            Assert.AreEqual("newAttr", searchCriterionViewModel.SelectedField!.DisplayName);

            Assert.AreSame(searchCriterionViewModel.SelectedFssAttribute, searchCriterionViewModel.SelectedField);
        }

        [Test]
        public void TestOperatorPropertyFiresPropertyChanged()
        {
            Assert.IsNull(searchCriterionViewModel.Operator);
            searchCriterionViewModel.AssertPropertyChanged(nameof(searchCriterionViewModel.Operator),
                () => searchCriterionViewModel.Operator = Operators.Equals);

            Assert.AreEqual(Operators.Equals, searchCriterionViewModel.Operator);
        }

        [Test]
        public void TestAvailableOperators()
        {
            CollectionAssert.IsEmpty(searchCriterionViewModel.AvailableOperators);
            searchCriterionViewModel.AssertPropertyChanged(nameof(searchCriterionViewModel.AvailableOperators), () =>
                searchCriterionViewModel.SelectedField = new Attribute("Field", "Field", AttributeType.String));
            CollectionAssert.AreEqual(searchCriterionViewModel.SelectedField!.AvailableOperators(),
                searchCriterionViewModel.AvailableOperators);
        }

        [Test]
        public void TestValuePropertyFiresPropertyChanged()
        {
            Assert.AreEqual("", searchCriterionViewModel.Value);
            searchCriterionViewModel.AssertPropertyChanged(nameof(searchCriterionViewModel.Value),
                () => searchCriterionViewModel.Value = "A New value");
            searchCriterionViewModel.AssertPropertyChangedNotFired(nameof(searchCriterionViewModel.Value),
                () => searchCriterionViewModel.Value = "A New value");
            Assert.AreEqual("A New value", searchCriterionViewModel.Value);
        }
    }
}