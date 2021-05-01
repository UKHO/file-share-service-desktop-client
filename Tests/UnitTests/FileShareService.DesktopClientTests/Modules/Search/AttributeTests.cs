using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class AttributeTests
    {
        [Test]
        public void TestDateSystemAttribute()
        {
            var pubDate = new Attribute("Publish Date", "PubDate", AttributeType.Date);
            Assert.AreEqual("Publish Date", pubDate.DisplayName);
            Assert.AreEqual("PubDate", pubDate.AttributeName);
            Assert.AreEqual(AttributeType.Date, pubDate.Type);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    Operators.Equals, Operators.NotEquals,
                    Operators.GreaterThan, Operators.GreaterThanOrEquals,
                    Operators.LessThan, Operators.LessThanOrEquals
                }, pubDate.AvailableOperators());
        }

        [Test]
        public void TestNullableDateSystemAttribute()
        {
            var pubDate = new Attribute("Expiry Date", "ExpiryDate", AttributeType.NullableDate);
            Assert.AreEqual("Expiry Date", pubDate.DisplayName);
            Assert.AreEqual("ExpiryDate", pubDate.AttributeName);
            Assert.AreEqual(AttributeType.NullableDate, pubDate.Type);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    Operators.Equals, Operators.NotEquals,
                    Operators.Exists, Operators.NotExists,
                    Operators.GreaterThan, Operators.GreaterThanOrEquals,
                    Operators.LessThan, Operators.LessThanOrEquals
                }, pubDate.AvailableOperators());
        }

        [Test]
        public void TestNumberSystemAttribute()
        {
            var pubDate = new Attribute("File Size", "FileSize", AttributeType.Number);
            Assert.AreEqual("File Size", pubDate.DisplayName);
            Assert.AreEqual("FileSize", pubDate.AttributeName);
            Assert.AreEqual(AttributeType.Number, pubDate.Type);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    Operators.Equals, Operators.NotEquals, Operators.GreaterThan, Operators.GreaterThanOrEquals,
                    Operators.LessThan, Operators.LessThanOrEquals
                }, pubDate.AvailableOperators());
        }

        [Test]
        public void TestSystemStringAttribute()
        {
            var userAttr = new Attribute("Filename", "filename", AttributeType.String);
            Assert.AreEqual("Filename", userAttr.DisplayName);
            Assert.AreEqual("filename", userAttr.AttributeName);
            Assert.AreEqual(AttributeType.String, userAttr.Type);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    Operators.Equals, Operators.NotEquals,
                    Operators.Contains, Operators.StartsWith, Operators.EndsWith
                }, userAttr.AvailableOperators());
        }

        [Test]
        public void TestUserAttribute()
        {
            var userAttr = new Attribute("Usr1", AttributeType.UserAttributeString);
            Assert.AreEqual("Usr1", userAttr.DisplayName);
            Assert.AreEqual("Usr1", userAttr.AttributeName);
            Assert.AreEqual(AttributeType.UserAttributeString, userAttr.Type);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    Operators.Equals, Operators.NotEquals,
                    Operators.Exists, Operators.NotExists,
                    Operators.Contains, Operators.StartsWith, Operators.EndsWith
                }, userAttr.AvailableOperators());
        }
    }
}