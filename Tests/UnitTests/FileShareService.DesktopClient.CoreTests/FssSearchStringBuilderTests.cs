using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace FileShareService.DesktopClient.CoreTests
{
    public class FssSearchStringBuilderTests
    {
        private FssSearchStringBuilder fssSearchStringBuilder = null!;

        private readonly IFssBatchAttribute simpleBatchAttribute1 =
            new TestFssBatchAttribute(AttributeType.UserAttributeString, "Attr1");

        private readonly IFssBatchAttribute simpleBatchAttribute2 =
            new TestFssBatchAttribute(AttributeType.UserAttributeString, "Attr2");

        private readonly IFssBatchAttribute stringBatchSystemAttribute1 =
            new TestFssBatchAttribute(AttributeType.String, "businessUnit");

        private readonly IFssBatchAttribute filesizeBatchAttribute =
            new TestFssBatchAttribute(AttributeType.Number, "filesize");

        private readonly IFssBatchAttribute pubDateBatchAttribute =
            new TestFssBatchAttribute(AttributeType.Date, "pubDate");

        private readonly IFssBatchAttribute nullableDateBatchAttribute =
            new TestFssBatchAttribute(AttributeType.NullableDate, "expiryDate");

        [SetUp]
        public void Setup()
        {
            fssSearchStringBuilder = new FssSearchStringBuilder();
        }

        [Test]
        public void TestEmptyCriteriaReturnsEmptyString()
        {
            Assert.AreEqual("", fssSearchStringBuilder.BuildSearch(System.Array.Empty<ISearchCriterion>()));
        }

        [Test]
        public void TestEmptyCriterionReturnsEmptyString()
        {
            Assert.AreEqual("", fssSearchStringBuilder.BuildSearch(new ISearchCriterion[] {new TestSearchCriterion()}));
        }

        [Test]
        public void TestPartialCriterionReturnsEmptyString()
        {
            Assert.AreEqual("",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                    {new TestSearchCriterion {SelectedFssAttribute = simpleBatchAttribute1}}));
        }

        [Test]
        public void TestTwoPartialCriterionsReturnsEmptyString()
        {
            Assert.AreEqual("",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion {SelectedFssAttribute = simpleBatchAttribute1},
                    new TestSearchCriterion {SelectedFssAttribute = simpleBatchAttribute2}
                }));
        }

        [Test]
        public void TestSimpleBatchAttributeEqualsValue()
        {
            Assert.AreEqual("$batch(Attr1) eq 'Product 1'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.Equals,
                        Value = "Product 1"
                    }
                }));
        }

        [Test]
        public void TestSimpleBatchAttributeNotEqualsValue()
        {
            Assert.AreEqual("$batch(Attr1) ne 'Product 1'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.NotEquals,
                        Value = "Product 1"
                    }
                }));
        }

        [Test]
        public void TestSimpleBatchAttributeExists()
        {
            Assert.AreEqual("$batch(Attr1) ne null",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.Exists,
                        Value = "Not used"
                    }
                }));
        }

        [Test]
        public void TestSimpleBatchAttributeNotExist()
        {
            Assert.AreEqual("$batch(Attr1) eq null",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.NotExists,
                        Value = "unused"
                    }
                }));
        }

        [Test]
        public void TestStringBatchSystemAttributeEqualsValue()
        {
            Assert.AreEqual("businessUnit eq 'BU 1'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = stringBatchSystemAttribute1,
                        Operator = Operators.Equals,
                        Value = "BU 1"
                    }
                }));
        }

        [Test]
        public void TestStringBatchSystemAttributeNotEqualsValue()
        {
            Assert.AreEqual("businessUnit ne 'BU 2'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = stringBatchSystemAttribute1,
                        Operator = Operators.NotEquals,
                        Value = "BU 2"
                    }
                }));
        }

        [Test]
        public void TestNumberBatchAttributeEqualsValue()
        {
            Assert.AreEqual("filesize eq 1234",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = filesizeBatchAttribute,
                        Operator = Operators.Equals,
                        Value = "1234"
                    }
                }));
        }

        [Test]
        public void TestNumberBatchAttributeNotEqualsValue()
        {
            Assert.AreEqual("filesize ne 1234",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = filesizeBatchAttribute,
                        Operator = Operators.NotEquals,
                        Value = "1234"
                    }
                }));
        }

        [Test]
        public void TestNumberBatchAttributeGtAndLtValue()
        {
            Assert.AreEqual("filesize gt 1234 and filesize ge 1235 and filesize lt 4321 and filesize le 4320",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = filesizeBatchAttribute,
                        Operator = Operators.GreaterThan,
                        Value = "1234"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = filesizeBatchAttribute,
                        Operator = Operators.GreaterThanOrEquals,
                        Value = "1235"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = filesizeBatchAttribute,
                        Operator = Operators.LessThan,
                        Value = "4321"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = filesizeBatchAttribute,
                        Operator = Operators.LessThanOrEquals,
                        Value = "4320"
                    }
                }));
        }

        [Test]
        public void TestDateBatchAttributeEqualsValue()
        {
            Assert.AreEqual(
                "pubDate eq 2012-12-03T07:16:23Z",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = pubDateBatchAttribute,
                        Operator = Operators.Equals,
                        Value = "2012-12-03T07:16:23Z"
                    }
                }));
        }

        [Test]
        public void TestDateBatchAttributeNotEqualsValue()
        {
            Assert.AreEqual(
                "pubDate ne 2012-12-03T07:16:23Z",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = pubDateBatchAttribute,
                        Operator = Operators.NotEquals,
                        Value = "2012-12-03T07:16:23Z"
                    }
                }));
        }

        [Test]
        public void TestDateBatchAttributeGtAndLtValue()
        {
            Assert.AreEqual(
                "pubDate gt 2012-12-03T07:16:23Z and pubDate ge 2012-12-03T07:16:00Z and pubDate lt 2021-02-19T58:15:53Z and pubDate le 2021-02-19T58:15:50Z",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = pubDateBatchAttribute,
                        Operator = Operators.GreaterThan,
                        Value = "2012-12-03T07:16:23Z"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = pubDateBatchAttribute,
                        Operator = Operators.GreaterThanOrEquals,
                        Value = "2012-12-03T07:16:00Z"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = pubDateBatchAttribute,
                        Operator = Operators.LessThan,
                        Value = "2021-02-19T58:15:53Z"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = pubDateBatchAttribute,
                        Operator = Operators.LessThanOrEquals,
                        Value = "2021-02-19T58:15:50Z"
                    }
                }));
        }

        [Test]
        public void TestNullableDateBatchAttributeEqualsValue()
        {
            Assert.AreEqual(
                "expiryDate eq 2012-12-03T07:16:23Z",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.Equals,
                        Value = "2012-12-03T07:16:23Z"
                    }
                }));
        }

        [Test]
        public void TestNullableDateBatchAttributeNotEqualsValue()
        {
            Assert.AreEqual(
                "expiryDate ne 2012-12-03T07:16:23Z",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.NotEquals,
                        Value = "2012-12-03T07:16:23Z"
                    }
                }));
        }

        [Test]
        public void TestNullableDateBatchAttributeExists()
        {
            Assert.AreEqual(
                "expiryDate ne null",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.Exists,
                        Value = "NotRequired"
                    }
                }));
        }

        [Test]
        public void TestNullableDateBatchAttributeNotExists()
        {
            Assert.AreEqual(
                "expiryDate eq null",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.NotExists,
                        Value = "NotRequired"
                    }
                }));
        }

        [Test]
        public void TestNullableDateBatchAttributeGtAndLtValue()
        {
            Assert.AreEqual(
                "expiryDate gt 2012-12-03T07:16:23Z and expiryDate ge 2012-12-03T07:16:00Z and expiryDate lt 2021-02-19T58:15:53Z and expiryDate le 2021-02-19T58:15:50Z",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.GreaterThan,
                        Value = "2012-12-03T07:16:23Z"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.GreaterThanOrEquals,
                        Value = "2012-12-03T07:16:00Z"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.LessThan,
                        Value = "2021-02-19T58:15:53Z"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = nullableDateBatchAttribute,
                        Operator = Operators.LessThanOrEquals,
                        Value = "2021-02-19T58:15:50Z"
                    }
                }));
        }

        [Test]
        public void TestOneValidCriterionAndOnePartialReturnsEmptyString()
        {
            Assert.AreEqual("$batch(Attr1) eq 'Value 1'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.Equals,
                        Value = "Value 1"
                    },
                    new TestSearchCriterion {SelectedFssAttribute = simpleBatchAttribute2}
                }));
        }

        [Test]
        public void TestOnePartialCriterionAndOneValidReturnsEmptyString()
        {
            Assert.AreEqual("$batch(Attr1) eq 'Value 1'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion {SelectedFssAttribute = simpleBatchAttribute2},
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.Equals,
                        Value = "Value 1"
                    }
                }));
        }

        [Test]
        public void TestSimpleTwoBatchAttributeEqualsValueJoinedWithAnd()
        {
            Assert.AreEqual("$batch(Attr1) eq 'Value 1' and $batch(Attr2) eq 'Value 2'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.Equals,
                        Value = "Value 1"
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute2,
                        Operator = Operators.Equals,
                        Value = "Value 2"
                    }
                }));
        }

        [Test]
        public void TestSimpleTwoBatchAttributeEqualsValueJoinedWithOr()
        {
            Assert.AreEqual("$batch(Attr1) eq 'Value 1' or $batch(Attr2) eq 'Value 2'",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.Equals,
                        Value = "Value 1",
                        And = AndOr.And  //This will not be considered as this is 1st row
                    },
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute2,
                        Operator = Operators.Equals,
                        Value = "Value 2",
                        And = AndOr.Or
                    }
                }));
        }

        #region Function operator tests

        [Test]
        public void TestStringBatchSystemAttributeContainsValue()
        {
            Assert.AreEqual("contains(businessUnit, 'service')",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = stringBatchSystemAttribute1,
                        Operator = Operators.Contains,
                        Value = "service"
                    }
                }));
        }

        [Test]
        public void TestStringBatchSystemAttributeStartsWithValue()
        {
            Assert.AreEqual("startswith(businessUnit, 'service')",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = stringBatchSystemAttribute1,
                        Operator = Operators.StartsWith,
                        Value = "service"
                    }
                }));
        }

        [Test]
        public void TestStringBatchSystemAttributeEndsWithValue()
        {
            Assert.AreEqual("endswith(businessUnit, 'service')",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = stringBatchSystemAttribute1,
                        Operator = Operators.EndsWith,
                        Value = "service"
                    }
                }));
        }

        [Test]
        public void TestBatchAttributeContainsValue()
        {
            Assert.AreEqual("contains($batch(Attr1), 'service')",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.Contains,
                        Value = "service"
                    }
                }));
        }

        [Test]
        public void TestBatchAttributeStartsWithValue()
        {
            Assert.AreEqual("startswith($batch(Attr1), 'service')",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.StartsWith,
                        Value = "service"
                    }
                }));
        }

        [Test]
        public void TestBatchAttributeEndsWithValue()
        {
            Assert.AreEqual("endswith($batch(Attr1), 'service')",
                fssSearchStringBuilder.BuildSearch(new ISearchCriterion[]
                {
                    new TestSearchCriterion
                    {
                        SelectedFssAttribute = simpleBatchAttribute1,
                        Operator = Operators.EndsWith,
                        Value = "service"
                    }
                }));
        }

        #endregion
    }

    internal class TestSearchCriterion : ISearchCriterion
    {
        public AndOr And { get; init; }
        public IFssBatchAttribute? SelectedFssAttribute { get; init; }
        public Operators? Operator { get; init; }
        public string Value { get; init; } = string.Empty;
    }

    internal class TestFssBatchAttribute : IFssBatchAttribute
    {
        public TestFssBatchAttribute(AttributeType type, string attributeName)
        {
            Type = type;
            AttributeName = attributeName;
        }

        public AttributeType Type { get; }
        public string AttributeName { get; }
    }
}