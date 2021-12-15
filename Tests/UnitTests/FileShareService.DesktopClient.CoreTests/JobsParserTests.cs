using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace FileShareService.DesktopClient.CoreTests
{
    public class JobsParserTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("{jobs:null}")]
        [TestCase("{jobs:[]}")]
        public void TestEmptyJobs(string json)
        {
            var jobsParser = new JobsParser();
            var result = jobsParser.Parse(json).jobs;
            Assert.IsInstanceOf<JsonReaderException>(result.Cast<ErrorDeserializingJobsJob>().Single().Exception);
        }

        [Test]
        public void TestSampleActions()
        {
            var s = GetType().Assembly.GetManifestResourceStream(GetType(), "sampleActions.json")!;
            using var sr = new StreamReader(s);
            var result = new JobsParser().Parse(sr.ReadToEnd());
            Assert.AreEqual(3, result.jobs.Count());

            var newBatchJob = result.jobs.OfType<NewBatchJob>().Single();
            var appendAclJob = result.jobs.OfType<AppendAclJob>().Single();
            var setExpiryDateJob = result.jobs.OfType<SetExpiryDateJob>().Single();

            Assert.AreEqual("Upload DVDs", newBatchJob.DisplayName);
            Assert.AreEqual("ADDS", newBatchJob.ActionParams.BusinessUnit);
            CollectionAssert.AreEquivalent(new KeyValuePair<string, string>[]
            {
                new("Product Type", "AVCS"),
                new("Year", "$(now.Year)"), //2021, My need to be careful here because files being uploaded are probably for "next" week which may be in next year.
                new("Week Number", "12"), // Need to be careful here because files being uploaded are probably for "next" week.
                new("S63 Version", "1.2"),
                new("Exchange Set Type", "Base"),
                new("Media Type", "DVD")
            }, newBatchJob.ActionParams.Attributes);
            CollectionAssert.IsEmpty(newBatchJob.ActionParams.Acl.ReadUsers);
            CollectionAssert.AreEquivalent(new[] {"distributors", "vars"}, newBatchJob.ActionParams.Acl.ReadGroups);
            Assert.AreEqual("$(now.AddDays(21))", newBatchJob.ActionParams.ExpiryDate);
            Assert.AreEqual(2, newBatchJob.ActionParams.Files.Count);
            Assert.AreEqual("D:\\Data\\AVCS_DVDs\\Week 2021_19\\AVCS_S631-1_REISSUE_DVD*.iso",
                newBatchJob.ActionParams.Files[0].SearchPath);
            Assert.AreEqual("application/x-raw-disk-image", newBatchJob.ActionParams.Files[0].MimeType);
            Assert.AreEqual(2, newBatchJob.ActionParams.Files[0].ExpectedFileCount);
            Assert.AreEqual("D:\\Data\\AVCS_DVDs\\Week 2021_19\\AVCS_S631-1_REISSUE_DVD*.SHA1",
                newBatchJob.ActionParams.Files[1].SearchPath);
            Assert.AreEqual("text/plain", newBatchJob.ActionParams.Files[1].MimeType);
            Assert.AreEqual(2, newBatchJob.ActionParams.Files[1].ExpectedFileCount);

            Assert.AreEqual("Sample 2, change ACL later to make the batch public", appendAclJob.DisplayName);
            Assert.AreEqual("64c954fe-cb20-46e1-b990-51dfb9711fdc", appendAclJob.ActionParams.BatchId);
            CollectionAssert.AreEquivalent(new[] {"c95464fe-cb20-46e1-b990-51dfb9711fdc"},
                appendAclJob.ActionParams.ReadUsers);
            CollectionAssert.AreEquivalent(new[] {"public"}, appendAclJob.ActionParams.ReadGroups);

            Assert.AreEqual("Sample 3, change the Expiry Date later.", setExpiryDateJob.DisplayName);
            Assert.AreEqual("64c954fe-cb20-46e1-b990-51dfb9711fdc", setExpiryDateJob.ActionParams.BatchId);
            Assert.AreEqual("2021-04-01T00:00:00Z", setExpiryDateJob.ActionParams.ExpiryDate);
        }

        [Test]
        public void TestErrorDeserializingJobs()
        {
            var jobParser = new JobsParser();
            var result = jobParser.Parse("BadJson");
            Assert.IsInstanceOf<IJob>(result.jobs.Single());
            StringAssert.StartsWith("Unexpected character encountered while parsing", result.jobs.Single().ErrorMessages.Single());
            Assert.IsInstanceOf<JsonReaderException>(result.jobs.Cast<ErrorDeserializingJobsJob>().Single().Exception);
            Assert.AreEqual(1, jobParser.ErrorJobs?.Count);
        }

        [Test]
        public void TestErrorDeserializingJobsWhenParseInvalidFormatFile()
        {
            var jobParser = new JobsParser();
            var s = GetType().Assembly.GetManifestResourceStream(GetType(), "sampleActionsWithInvalidFormat.json")!;
            using var sr = new StreamReader(s);
            var result = jobParser.Parse(sr.ReadToEnd());

            Assert.IsInstanceOf<ErrorDeserializingJobsJob>(result.jobs.Single());
            Assert.IsInstanceOf<JsonReaderException>(result.jobs.Cast<ErrorDeserializingJobsJob>().Single().Exception);
            Assert.AreEqual(1, jobParser.ErrorJobs?.Count);
        }

        [Test]
        public void TestErrorDeserializingJobsWhenParseInvalidActions()
        {
            var jobParser = new JobsParser();
            var s = GetType().Assembly.GetManifestResourceStream(GetType(), "sampleActionsWithDuplicateJobs.json")!;
            using var sr = new StreamReader(s);
            var result = jobParser.Parse(sr.ReadToEnd());

            Assert.IsInstanceOf<ErrorDeserializingJobsJob>(result.jobs.First());
            Assert.AreEqual(1, jobParser.ErrorJobs?.Count);
        }

        [Test]
        public void TestErrorBlankFileAttributesWhenParseFileAttributes()
        {
            var s = GetType().Assembly.GetManifestResourceStream(GetType(), "sampleActionsWithFileAttributes.json")!;
            using var sr = new StreamReader(s);
            var result = new JobsParser().Parse(sr.ReadToEnd());

            Assert.IsInstanceOf<NewBatchJob>(result.jobs.First());
            StringAssert.StartsWith("File attribute key cannot be blank", result.jobs.First().ErrorMessages.First());
            StringAssert.StartsWith("File attribute value cannot be blank", result.jobs.First().ErrorMessages.ElementAt(1));
            StringAssert.StartsWith("Invalid file attribute", result.jobs.First().ErrorMessages.Last());
        }
    }
}