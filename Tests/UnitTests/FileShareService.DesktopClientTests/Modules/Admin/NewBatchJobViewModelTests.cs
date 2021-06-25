using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;
using UKHO.WeekNumberUtils;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    [TestFixture]
    public class NewBatchJobViewModelTests
    {
        private MockFileSystem fileSystem = null!;
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private ICurrentDateTimeProvider fakeCurrentDateTimeProvider = null!;


        [SetUp]
        public void Setup()
        {
            fileSystem = new MockFileSystem();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakeCurrentDateTimeProvider = A.Fake<ICurrentDateTimeProvider>();
        }

        [TestCase("$(now.Year)")]
        [TestCase("$(now.Year   )")]
        [TestCase("$(   now.Year)")]
        [TestCase("$(   now.Year   )")]
        [TestCase("$(   now.AddDays(30).Year   )")]
        public void TestExpandMacrosOfYearInNewBatchAttributes(string input)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(new DateTime(2021, 02, 10, 15, 32, 10, DateTimeKind.Utc));
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
                {
                    DisplayName = "Create new Batch 123",
                    ActionParams = new NewBatchJobParams
                    {
                        BusinessUnit = "TestBU1",
                        Attributes = new Dictionary<string, string>
                        {
                            {"BatchAttribute1", "Value1"},
                            {"YearMacro1", input},
                            {"YearMacro2", "Padding " + input},
                            {"YearMacro3", $"Padding {input} and Right Padding"}
                        },
                        Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName
                            }
                        }
                    }
                },
                fileSystem,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            var expandedAttributes = vm.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedYear1 = DateTime.UtcNow.Year + "";
            Assert.AreEqual(expectedYear1, expandedAttributes["YearMacro1"]);
            Assert.AreEqual("Padding " + expectedYear1, expandedAttributes["YearMacro2"]);
            Assert.AreEqual("Padding " + expectedYear1 + " and Right Padding", expandedAttributes["YearMacro3"]);
        }

        [TestCase("$(now.WeekNumber)", 0)]
        [TestCase("$(now.AddDays(7).WeekNumber)", 1)]
        [TestCase("$(now.AddDays(21).WeekNumber)", 3)]
        [TestCase("$(now.AddDays(-14).WeekNumber)", -2)]
        [TestCase("$(now.WeekNumber   )", 0)]
        [TestCase("$(   now.WeekNumber)", 0)]
        [TestCase("$(   now.WeekNumber   )", 0)]
        [TestCase("$(now.WeekNumber+1)", 1)]
        [TestCase("$(now.WeekNumber +1)", 1)]
        [TestCase("$(now.WeekNumber + 1)", 1)]
        [TestCase("$(now.WeekNumber+ 1)", 1)]
        [TestCase("$(now.WeekNumber +10)", 10)]
        [TestCase("$(now.WeekNumber   -1)", -1)]
        [TestCase("$(now.WeekNumber-1)", -1)]
        [TestCase("$(now.WeekNumber -  1)", -1)]
        [TestCase("$(now.WeekNumber   -10)", -10)]
        public void TestExpandMacrosOfUkhoWeekInNewBatchAttributes(string input, int offset)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime).Returns(DateTime.UtcNow);
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
                {
                    DisplayName = "Create new Batch 123",
                    ActionParams = new NewBatchJobParams
                    {
                        BusinessUnit = "TestBU1",
                        Attributes = new Dictionary<string, string>
                        {
                            {"BatchAttribute1", "Value1"},
                            {"WeekMacro1", input},
                            {"WeekMacro2", "Padding " + input},
                            {"WeekMacro3", $"Padding {input} and Right Padding"},
                            {"MultiWeekMacro", $"Padding {input} and {input} with Right Padding"}
                        },
                        Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName
                            }
                        }
                    }
                },
                fileSystem,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            var expandedAttributes = vm.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedWeekNumber = "" + (WeekNumber.GetUKHOWeekFromDateTime(DateTime.UtcNow).Week + offset);
            Assert.AreEqual(expectedWeekNumber, expandedAttributes["WeekMacro1"]);
            Assert.AreEqual("Padding " + expectedWeekNumber, expandedAttributes["WeekMacro2"]);
            Assert.AreEqual("Padding " + expectedWeekNumber + " and Right Padding", expandedAttributes["WeekMacro3"]);
            Assert.AreEqual("Padding " + expectedWeekNumber + " and " + expectedWeekNumber + " with Right Padding",
                expandedAttributes["MultiWeekMacro"]);
        }

        [TestCase("$(now)", 0)]
        [TestCase("$(now.AddDays(0))", 0)]
        [TestCase("$(now.AddDays(1))", 1)]
        [TestCase("$(now.AddDays(+1))", 1)]
        [TestCase("$(now.AddDays( 1 ))", 1)]
        [TestCase("$(now.AddDays( + 1 ))", 1)]
        [TestCase("$(now.AddDays(7))", 7)]
        [TestCase("$(now.AddDays(31))", 31)]
        [TestCase("$(now.AddDays(365))", 365)]
        [TestCase("$(now.AddDays(-1))", -1)]
        [TestCase("$(now.AddDays(-14))", -14)]
        [TestCase("$(now.AddDays( - 1 ))", -1)]
        public void TestExpandExpiryDateMacros(string input, int dayOffsetFromNow)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime).Returns(DateTime.UtcNow);
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
                {
                    DisplayName = "Create new Batch 123",
                    ActionParams = new NewBatchJobParams
                    {
                        BusinessUnit = "TestBU1",
                        Attributes = new Dictionary<string, string>
                        {
                            {"BatchAttribute1", "Value1"}
                        },
                        Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName
                            }
                        },
                        ExpiryDate = input
                    }
                },
                fileSystem,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);


            Assert.AreEqual(input, vm.RawExpiryDate);

            var expiry = vm.ExpiryDate;
            Assert.IsNotNull(expiry);
            Assert.AreEqual(DateTime.UtcNow.AddDays(dayOffsetFromNow).Date, expiry.Value.Date);
        }

        [Test]
        public void TestFileSearch()
        {
            var file1FullFileName = @"c:\data\files\f1.txt";
            var file2FullFileName = @"c:\data\files\f2.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));
            fileSystem.AddFile(file2FullFileName, new MockFileData("File 2 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
                {
                    DisplayName = "Create new Batch 123",
                    ActionParams = new NewBatchJobParams
                    {
                        BusinessUnit = "TestBU1",
                        Attributes = new Dictionary<string, string> {{"BatchAttribute1", "Value1"}},
                        Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName
                            },
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file2FullFileName
                            }
                        }
                    }
                },
                fileSystem,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            Assert.AreEqual(2, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] {file1FullFileName, file2FullFileName},
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));
        }

        [TestCase("abc", "abc")]
        [TestCase("abc$(now.Year)_$(now.WeekNumber)", "abc2020_11")]
        [TestCase("abc$(now.AddDays(30).Year)_$(now.AddDays(30).WeekNumber)", "abc2020_16")]
        [TestCase("Week $(now.AddDays(-14).WeekNumber)", "Week 9")]
        [TestCase("abc$(now.AddDays(360).Year)_$(now.AddDays(360).WeekNumber)", "abc2021_10")]
        public void TestFileSearchWithMacroInDirectory(string directoryMacro, string expandedDirectoryName)
        {
            var now = new DateTime(2020, 03, 18, 10, 30, 55, DateTimeKind.Utc);
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime).Returns(now);

            var file1FullFileName = Path.Combine("c:\\data", expandedDirectoryName, "f1.txt");
            var file2FullFileName = Path.Combine("c:\\data", expandedDirectoryName, "f2.txt");
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));
            fileSystem.AddFile(file2FullFileName, new MockFileData("File 2 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
                {
                    DisplayName = "Create new Batch 123",
                    ActionParams = new NewBatchJobParams
                    {
                        BusinessUnit = "TestBU1",
                        Attributes = new Dictionary<string, string> {{"BatchAttribute1", "Value1"}},
                        Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = Path.Combine("c:\\data", directoryMacro, "f1.txt")
                            },
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = Path.Combine("c:\\data", directoryMacro, "f2.txt")
                            }
                        }
                    }
                },
                fileSystem,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            Assert.AreEqual(2, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] {file1FullFileName, file2FullFileName},
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));
        }

        [Test]
        public void TestFileSearchForInvalidDirectory()
        {
            var file1FullFileName = @"c:\data\files\f1.txt";
            var file2FullFileName = @"c:\dirNotExist\files\f2.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
                {
                    DisplayName = "Create new Batch 123",
                    ActionParams = new NewBatchJobParams
                    {
                        BusinessUnit = "TestBU1",
                        Attributes = new Dictionary<string, string> {{"BatchAttribute1", "Value1"}},
                        Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName
                            },
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file2FullFileName
                            }
                        }
                    }
                },
                fileSystem,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            Assert.AreEqual(1, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] {file1FullFileName},
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));
        }


        [Test]
        public void TestWibble()
        {
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
                {
                    DisplayName = "Create new Batch 123",
                    ActionParams = new NewBatchJobParams
                    {
                        BusinessUnit = "TestBU1",
                        Attributes = new Dictionary<string, string> {{"BatchAttribute1", "Value1"}},
                        Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName
                            }
                        }
                    }
                },
                fileSystem,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            Assert.AreEqual("Create new Batch 123", vm.DisplayName);
            var batchModelToSendToFss = vm.BuildBatchModel();
            Assert.IsNotNull(batchModelToSendToFss);


            var createBatchTcs = new TaskCompletionSource<IBatchHandle>();
            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored))
                .Returns(createBatchTcs.Task);

            vm.ExcecuteJobCommand.Execute();

            createBatchTcs.SetResult(A.Fake<IBatchHandle>());

            Assert.Inconclusive();
        }
    }
}