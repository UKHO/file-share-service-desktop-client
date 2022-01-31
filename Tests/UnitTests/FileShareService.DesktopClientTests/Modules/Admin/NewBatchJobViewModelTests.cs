using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareAdminClient.Models.Response;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Helper;
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
        private ILogger<NewBatchJobViewModel> fakeLoggerNewBatchJobVM = null!;
        private IMacroTransformer macroTransformer = null!;
        private IDateTimeValidator dateTimeValidator = null!;
        private IMessageBoxService fakeMessageBoxService = null!;
        private IBatchHandle fakeBatchHandle = null!;


        [SetUp]
        public void Setup()
        {
            fileSystem = new MockFileSystem();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakeCurrentDateTimeProvider = A.Fake<ICurrentDateTimeProvider>();
            fakeLoggerNewBatchJobVM = A.Fake<ILogger<NewBatchJobViewModel>>();
            macroTransformer = new MacroTransformer(fakeCurrentDateTimeProvider);
            dateTimeValidator = new DateTimeValidator(macroTransformer);
            fakeMessageBoxService = A.Fake<IMessageBoxService>();
            fakeBatchHandle = A.Fake<IBatchHandle>();

            A.CallTo(() => fakeBatchHandle.BatchId).Returns("TESTBATCH");
        }

        [TestCase("$(now.Year)")]
        [TestCase("$(now.Year2)")]
        [TestCase("$(now.Year   )")]
        [TestCase("$(now.Year2   )")]
        [TestCase("$(   now.Year)")]
        [TestCase("$(   now.Year2)")]
        [TestCase("$(   now.Year   )")]
        [TestCase("$(   now.Year2   )")]
        [TestCase("$(   now.AddDays(30).Year   )")]
        [TestCase("$(   now.AddDays(30).Year2   )")]
        public void TestExpandMacrosOfYearInNewBatchAttributes(string input)
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(new DateTime(DateTime.Now.Year, 02, 10, 15, 32, 10, DateTimeKind.Utc));
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute>
                    {
                        new KeyValueAttribute("BatchAttribute1", "Value1"),
                        new KeyValueAttribute("YearMacro1", input),
                        new KeyValueAttribute("YearMacro2", "Padding " + input),
                        new KeyValueAttribute("YearMacro3", $"Padding {input} and Right Padding")
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            var expandedAttributes = vm.Attributes!.ToDictionary(kv => kv.Key, kv => kv.Value);

            var expectedYear = DateTime.UtcNow.Year.ToString();
            if (input.Contains("Year2"))
            {
                expectedYear = expectedYear.Substring(2, 2);
            }

            Assert.AreEqual(expectedYear, expandedAttributes["YearMacro1"]);
            Assert.AreEqual("Padding " + expectedYear, expandedAttributes["YearMacro2"]);
            Assert.AreEqual("Padding " + expectedYear + " and Right Padding", expandedAttributes["YearMacro3"]);
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
                    Attributes = new List<KeyValueAttribute>
                    {
                        new KeyValueAttribute("BatchAttribute1", "Value1"),
                        new KeyValueAttribute("WeekMacro1", input),
                        new KeyValueAttribute("WeekMacro2", "Padding " + input),
                        new KeyValueAttribute("WeekMacro3", $"Padding {input} and Right Padding"),
                        new KeyValueAttribute("MultiWeekMacro", $"Padding {input} and {input} with Right Padding")
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            var expandedAttributes = vm.Attributes!.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedWeekNumber = WeekNumber.GetUKHOWeekFromDateTime(DateTime.UtcNow.AddDays(offset * 7)).Week.ToString();

            Assert.AreEqual(expectedWeekNumber, expandedAttributes["WeekMacro1"]);
            Assert.AreEqual("Padding " + expectedWeekNumber, expandedAttributes["WeekMacro2"]);
            Assert.AreEqual("Padding " + expectedWeekNumber + " and Right Padding", expandedAttributes["WeekMacro3"]);
            Assert.AreEqual("Padding " + expectedWeekNumber + " and " + expectedWeekNumber + " with Right Padding",
                expandedAttributes["MultiWeekMacro"]);
        }

        [TestCase("$(now.WeekNumber2)", 0)]
        [TestCase("$(now.AddDays(7).WeekNumber2)", 1)]
        [TestCase("$(now.AddDays(21).WeekNumber2)", 3)]
        [TestCase("$(now.AddDays(-14).WeekNumber2)", -2)]
        [TestCase("$(now.WeekNumber2   )", 0)]
        [TestCase("$(   now.WeekNumber2)", 0)]
        [TestCase("$(   now.WeekNumber2   )", 0)]
        [TestCase("$(now.WeekNumber2+1)", 1)]
        [TestCase("$(now.weeknumber2 +1)", 1)]
        [TestCase("$(now.WeekNumber2 + 1)", 1)]
        [TestCase("$(now.WeekNumber2+ 1)", 1)]
        [TestCase("$(now.WeekNumber2 +10)", 10)]
        [TestCase("$(now.WeekNumber2   -1)", -1)]
        [TestCase("$(now.WeekNumber2-1)", -1)]
        [TestCase("$(now.WeekNumber2 -  1)", -1)]
        [TestCase("$(now.WeekNumber2   -10)", -10)]
        [TestCase("$(now.weeknumber2   -10)", -10)]
        public void TestExpandMacrosOfUkhoZeroPaddedWeekInNewBatchAttributes(string input, int offset)
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
                    Attributes = new List<KeyValueAttribute>
                    {
                        new KeyValueAttribute("BatchAttribute1", "Value1"),
                        new KeyValueAttribute("WeekMacro1", input),
                        new KeyValueAttribute("WeekMacro2", "Padding " + input),
                        new KeyValueAttribute("WeekMacro3", $"Padding {input} and Right Padding"),
                        new KeyValueAttribute("MultiWeekMacro", $"Padding {input} and {input} with Right Padding")
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            var expandedAttributes = vm.Attributes!.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedWeekNumber = WeekNumber.GetUKHOWeekFromDateTime(DateTime.UtcNow.AddDays(offset * 7)).Week.ToString().PadLeft(2, '0');

            Assert.AreEqual(expectedWeekNumber, expandedAttributes["WeekMacro1"]);
            Assert.AreEqual("Padding " + expectedWeekNumber, expandedAttributes["WeekMacro2"]);
            Assert.AreEqual("Padding " + expectedWeekNumber + " and Right Padding", expandedAttributes["WeekMacro3"]);
            Assert.AreEqual("Padding " + expectedWeekNumber + " and " + expectedWeekNumber + " with Right Padding",
                expandedAttributes["MultiWeekMacro"]);
        }


        [TestCase("$(now.WeekNumber.Year)", 0)]
        [TestCase("$(now.WeekNumber.Year2)", 0)]
        [TestCase("$(now.AddDays(7).WeekNumber.Year)", 1)]
        [TestCase("$(now.AddDays(7).WeekNumber.Year2)", 1)]
        [TestCase("$(now.WeekNumber +10.Year)", 10)]
        [TestCase("$(now.WeekNumber +10.Year2)", 10)]
        public void TestExpandMacrosOfUkhoWeekYearInNewBatchAttributes(string input, int offset)
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
                    Attributes = new List<KeyValueAttribute>
                    {
                        new KeyValueAttribute("BatchAttribute1", "Value1"),
                        new KeyValueAttribute("WeekYearMacro1", input),
                        new KeyValueAttribute("WeekYearMacro2", "Padding " + input),
                        new KeyValueAttribute("WeekYearMacro3", $"Padding {input} and Right Padding"),
                        new KeyValueAttribute("MultiWeekYearMacro", $"Padding {input} and {input} with Right Padding")
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            var expandedAttributes = vm.Attributes!.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedWeekYear = WeekNumber.GetUKHOWeekFromDateTime(DateTime.UtcNow.AddDays(offset * 7)).Year.ToString();
            if (input.Contains("Year2"))
            {
                expectedWeekYear = expectedWeekYear.Substring(2, 2);
            }

            Assert.AreEqual(expectedWeekYear, expandedAttributes["WeekYearMacro1"]);
            Assert.AreEqual("Padding " + expectedWeekYear, expandedAttributes["WeekYearMacro2"]);
            Assert.AreEqual("Padding " + expectedWeekYear + " and Right Padding", expandedAttributes["WeekYearMacro3"]);
            Assert.AreEqual("Padding " + expectedWeekYear + " and " + expectedWeekYear + " with Right Padding",
                expandedAttributes["MultiWeekYearMacro"]);
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

            var newJob = new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
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
            };

            typeof(NewBatchJob).GetProperty(nameof(newJob.IsExpiryDateKeyExist))?.SetValue(newJob, true, null);

            var vm = new NewBatchJobViewModel(newJob,
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            Assert.AreEqual(input, vm.RawExpiryDate);

            var expiry = vm.ExpiryDate;
            Assert.IsNotNull(expiry);
            Assert.AreEqual(DateTime.UtcNow.AddDays(dayOffsetFromNow).Date, expiry!.Value.Date);
        }

        [Test]
        public void TestExpiryDateWithPastDate()
        {
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime).Returns(DateTime.UtcNow);
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var newJob = new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                    {
                        new NewBatchFiles
                        {
                            ExpectedFileCount = 1,
                            MimeType = "text/plain",
                            SearchPath = file1FullFileName
                        }
                    },
                    ExpiryDate = "$(now.AddDays(-5))"
                }
            };

            typeof(NewBatchJob).GetProperty(nameof(newJob.IsExpiryDateKeyExist))?.SetValue(newJob, true, null);

            var vm = new NewBatchJobViewModel(newJob,
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            Assert.AreEqual("$(now.AddDays(-5))", vm.RawExpiryDate);

            var expiry = vm.ExpiryDate;
            Assert.IsNotNull(expiry);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());
            StringAssert.StartsWith("Expiry date cannot be a past date", vm.ValidationErrors[0]);
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
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            Assert.AreEqual(2, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] { file1FullFileName, file2FullFileName },
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));

            Assert.IsTrue(vm.ExcecuteJobCommand.CanExecute());
        }

        [TestCase("c:\\data\\abc\\f1*.txt", "c:\\data\\abc\\f1*.txt")]
        [TestCase("c:\\data\\abc$(now.Year)_$(now.WeekNumber)\\f1*.txt", "c:\\data\\abc2020_11\\f1*.txt")]
        [TestCase("c:\\data\\abc$(now.AddDays(30).Year)_$(now.AddDays(30).WeekNumber)\\f1*.txt", "c:\\data\\abc2020_16\\f1*.txt")]
        [TestCase("c:\\data\\Week $(now.AddDays(-14).WeekNumber)\\f1*.txt", "c:\\data\\Week 9\\f1*.txt")]
        [TestCase("c:\\data\\abc$(now.AddDays(360).Year)_$(now.AddDays(360).WeekNumber)\\f1*.txt", "c:\\data\\abc2021_10\\f1*.txt")]
        [TestCase("c:\\data\\$(now.Year)*.txt", "c:\\data\\2020*.txt")]
        [TestCase("c:\\$(now.Year)\\*.txt", "c:\\2020\\*.txt")]
        public void TestFileSearchWithMacroInDirectory(string macroFilePath, string expandedFilePath)
        {
            var now = new DateTime(2020, 03, 18, 10, 30, 55, DateTimeKind.Utc);
            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime).Returns(now);

            var filesA = (macroFilePath: macroFilePath.Replace("*.txt", "A*.txt"), expandedFilePath: expandedFilePath.Replace("*.txt", "AA.txt"));
            var filesB = (macroFilePath: macroFilePath.Replace("*.txt", "B*.txt"), expandedFilePath: expandedFilePath.Replace("*.txt", "BB.txt"));
            fileSystem.AddFile(filesA.expandedFilePath, new MockFileData("File AA contents"));
            fileSystem.AddFile(filesB.expandedFilePath, new MockFileData("File BB contents"));
            fileSystem.AddDirectory(Path.GetDirectoryName(expandedFilePath));

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = filesA.macroFilePath
                            },
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = filesB.macroFilePath
                            }
                        }
                }
            },
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            Assert.AreEqual(2, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] { filesA.expandedFilePath, filesB.expandedFilePath },
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));

            Assert.IsTrue(vm.ExcecuteJobCommand.CanExecute());
        }


        [TestCase("abc$(now.AddDays(-21).WeekNumber2)", "abc08")]
        [TestCase("abc$(now.AddDays(-21).WeekNumber2)_$(now.AddDays(7).WeekNumber.Year)", "abc08_2020")]
        [TestCase("abc$(now.AddDays(-21).WeekNumber2)_$(now.AddDays(7).WeekNumber.Year2)", "abc08_20")]
        [TestCase("abc$(now.AddDays(-96).WeekNumber2)_$(now.AddDays(7).WeekNumber.Year2)", "abc50_20")]
        [TestCase("Week $(now.AddDays(-14).WeekNumber2)", "Week 09")]
        public void TestFileSearchWithMacroInDirectoryForWeekNumber2(string directoryMacro, string expandedDirectoryName)
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
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            Assert.AreEqual(2, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] { file1FullFileName, file2FullFileName },
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
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            Assert.AreEqual(1, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] { file1FullFileName },
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());
        }

        [Test]
        public void TestCannotExecuteCommandIfFoundFilesDontMatchExpectedFileCount()
        {
            var file1FullFileName = @"c:\data\files\f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
        }
                        }
                }
            },
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            Assert.AreEqual(1, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] { file1FullFileName },
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));

            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());
        }

        [Test]
        public void TestShowFilesAndDirectoriesInValidationErrorMessage()
        {
            var file1 = @"c:\data\files\f1.txt";
            var file2 = @"c:\data\files\temp1.txt";
            var file3 = @"c:\data\files\docs\doc1.txt";
            fileSystem.AddFile(file1, new MockFileData("File 1 contents"));
            fileSystem.AddFile(file2, new MockFileData("File 2 contents"));
            fileSystem.AddFile(file3, new MockFileData("File 3 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                        }
                }
            },
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            Assert.AreEqual(1, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] { file1 },
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));

            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            //Assert file names are exist in error message.
            Assert.IsTrue(vm.ValidationErrors[0].Contains("f1.txt"));
            Assert.IsTrue(vm.ValidationErrors[0].Contains("temp1.txt"));

            //Assert directory name is exist in error message.
            Assert.IsTrue(vm.ValidationErrors[0].Contains("docs"));
        }

        [Test]
        public void TestNoDirectoryAndFileExistsInSpecifiedPath()
        {
            fileSystem.AddDirectory(@"c:\data\files");

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                        }
                }
            },
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            Assert.AreEqual(1, vm.Files.Count);

            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            //Assert no file name amd folder name are exist in error message.
            Assert.IsTrue(vm.ValidationErrors[0].Contains(@"No subdirectories exist in directory 'c:\data\files'"));
            Assert.IsTrue(vm.ValidationErrors[0].Contains(@"No files exist in directory 'c:\data\files'"));
        }

        [Test]
        public async Task TestSimpleExceuteNewBatchJob() //test case updated as part of PBI-12467 - API responses for CreateNewBatch
        {
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName,
                                Attributes = new List<KeyValueAttribute>
                                            {
                                            new KeyValueAttribute("Product Type","AVCS"),
                                            new KeyValueAttribute("Exchange Set Type", "Base")
                                            }                                                       
                            }
                        }
                }
            },
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            Assert.AreEqual("Create new Batch 123", vm.DisplayName);            

            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored)).Returns(new BatchSearchResponse() { Total = 0 });
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.Yes);
            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None)).Returns(new Result<IBatchHandle>() { IsSuccess = true, Data = fakeBatchHandle });
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored, A<string>.Ignored, A<string>.Ignored, CancellationToken.None, A<KeyValuePair<string, string>>.Ignored)).Returns(new Result<AddFileToBatchResponse> { IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored, A<string>.Ignored, A<string>.Ignored, A<Action<(int, int)>>.Ignored, A<CancellationToken>.Ignored, A<KeyValuePair<string, string>[]>.Ignored)).Returns(new Result<AddFileToBatchResponse> { IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored, CancellationToken.None)).Returns(new Result<CommitBatchResponse> { IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.Committed });

            var executeTask = vm.OnExecuteCommand();                       
                   
            await executeTask;
            Assert.IsFalse(vm.IsExecuting);            
            Assert.IsTrue(vm.IsExecutingComplete);
            Assert.IsFalse(vm.IsCommitting);
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored, CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).MustHaveHappened();
            vm.CloseExecutionCommand!.Execute();
            Assert.IsFalse(vm.IsExecutingComplete);
        }

        [Test]
        public async Task TestCheckIsbatchCommittedResults()
        {
            var batchHandle = A.Fake<IBatchHandle>();
            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute(null!, null!) },

                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = "c:/abc/test1.txt"
                            }
                        }
                }
            },
            fileSystem, fakeLoggerNewBatchJobVM,
            () => fakeFileShareApiAdminClient,
            fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);



            // Testing method returns true when committed
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.Committed });
            var result = await vm.CheckBatchIsCommitted(fakeFileShareApiAdminClient, batchHandle, 1);
            Assert.IsTrue(result);

            // Testing method returns false when response is not committed within wait time
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.CommitInProgress });
            result = await vm.CheckBatchIsCommitted(fakeFileShareApiAdminClient, batchHandle, 0.1);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task TestExceuteNewBatchJobHasValidationErrors() //test case updated as part of PBI-12467 - API responses for CreateNewBatch
        {
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var newBatchJob = A.Fake<NewBatchJob>();
            newBatchJob.ErrorMessages.Add("Test validation error message.");
            newBatchJob.DisplayName = "Create new Batch";

            var vm = new NewBatchJobViewModel(newBatchJob,
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            Assert.AreEqual("Create new Batch", vm.DisplayName);

            var batchHandle = A.Fake<IBatchHandle>();
            var result_createBatchTcs = A.Fake<IResult<IBatchHandle>>();
            var result_addFileToBatchTcs = A.Fake<IResult<AddFileToBatchResponse>>();
            var result_commitBatchTcs = A.Fake<IResult<CommitBatchResponse>>();

            var createBatchTcs = new TaskCompletionSource<IResult<IBatchHandle>>();
            var addFileToBatchTcs = new TaskCompletionSource<IResult<AddFileToBatchResponse>>();
            var commitBatchTcs = new TaskCompletionSource<IResult<CommitBatchResponse>>();

            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None))
                .Returns(createBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored,
                A<string>.Ignored, A<string>.Ignored, CancellationToken.None)).Returns(addFileToBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored, CancellationToken.None)).Returns(commitBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored))
                .Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.Committed });

            var executeTask = vm.OnExecuteCommand();
            vm.ExcecuteJobCommand.Execute();
            Assert.IsTrue(vm.IsExecuting);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            createBatchTcs.SetResult(result_createBatchTcs);
            addFileToBatchTcs.SetResult(result_addFileToBatchTcs);
            commitBatchTcs.SetResult(result_commitBatchTcs);

            await executeTask;
            Assert.IsFalse(vm.IsExecuting);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());
            StringAssert.StartsWith("Test validation error", vm.ValidationErrors[0]);
        }

        [Test]
        public void TestCancelNewBatchJobAndYesResponse()
        {

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                         }
                }
            },
             fileSystem, fakeLoggerNewBatchJobVM,
             () => fakeFileShareApiAdminClient,
             fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            vm.CancellationTokenSource = new CancellationTokenSource();
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.Yes);
            vm.CancelJobExecutionCommand.Execute();
            Assert.IsTrue(vm.IsCanceled);
            Assert.IsTrue(vm.CancellationTokenSource.IsCancellationRequested);
        }

        [Test]
        public void TestCancelNewBatchJobAndNoResponse()
        {

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                         }
                }
            },
             fileSystem, fakeLoggerNewBatchJobVM,
             () => fakeFileShareApiAdminClient,
             fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            vm.CancellationTokenSource = new CancellationTokenSource();
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.No);
            vm.CancelJobExecutionCommand.Execute();
            Assert.IsFalse(vm.IsCanceled);
            Assert.IsFalse(vm.CancellationTokenSource.IsCancellationRequested);
        }

        [Test]
        public void TestCancelNewBatchJobWithRollBack()
        {
            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                        }
                }
            },
            fileSystem, fakeLoggerNewBatchJobVM,
            () => fakeFileShareApiAdminClient,
            fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            var batchHandle = A.Fake<IBatchHandle>();
            bool IsCommitting = false;

            MethodInfo? methodInfo = typeof(NewBatchJobViewModel).GetMethod("HandleCanceledOperationsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { batchHandle, IsCommitting, fakeFileShareApiAdminClient };
            if (methodInfo != null)
            {
                methodInfo.Invoke(vm, parameters);
                A.CallTo(() => fakeFileShareApiAdminClient.RollBackBatchAsync(batchHandle, CancellationToken.None)).MustHaveHappened();
                Assert.AreEqual("Canceled job is completed for batch ID:", vm.ExecutionResult);
            }
        }

        [Test]
        public void TestCancelNewBatchJobWithSetExpiry()
        {
            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                         }
                }
            },
             fileSystem, fakeLoggerNewBatchJobVM,
             () => fakeFileShareApiAdminClient,
             fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);


            var batchHandle = A.Fake<IBatchHandle>();
            bool IsCommitting = true;

            MethodInfo? methodInfo = typeof(NewBatchJobViewModel).GetMethod("HandleCanceledOperationsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { batchHandle, IsCommitting, fakeFileShareApiAdminClient };
            if (methodInfo != null)
            {
                methodInfo.Invoke(vm, parameters);
                A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(batchHandle.BatchId, A<BatchExpiryModel>.Ignored, CancellationToken.None)).MustHaveHappened();
                Assert.AreEqual("Canceled job is completed for batch ID:", vm.ExecutionResult);
            }
        }

        [Test]
        public async Task TestSimpleExceuteNewBatchJobWhenFileAttributesAdded() //test case updated as part of PBI-12467 - API responses for CreateNewBatch
        {
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 1,
                                MimeType = "text/plain",
                                SearchPath = file1FullFileName,
                                Attributes = new List<KeyValueAttribute>
                                {
                                    new KeyValueAttribute("Product Type","AVCS"),
                                    new KeyValueAttribute("Exchange Set Type", "Base")
                                },
                            }
                        }
                }
            },
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            Assert.AreEqual("Create new Batch 123", vm.DisplayName);            

            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None)).Returns(new Result<IBatchHandle>() { IsSuccess = true, Data = fakeBatchHandle });
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored, A<string>.Ignored, A<string>.Ignored, A<Action<(int, int)>>.Ignored, A<CancellationToken>.Ignored, A<KeyValuePair<string, string>[]>.Ignored)).Returns(new Result<AddFileToBatchResponse> { IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored, CancellationToken.None)).Returns(new Result<CommitBatchResponse> { IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored))
                .Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.Committed });

            var executeTask = vm.OnExecuteCommand();                      
           
            await executeTask;
            Assert.IsFalse(vm.IsExecuting);            
            Assert.IsTrue(vm.IsExecutingComplete);
            Assert.IsFalse(vm.IsCommitting);
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored, CancellationToken.None)).MustHaveHappened();
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).MustHaveHappened();
            vm.CloseExecutionCommand!.Execute();
            Assert.IsFalse(vm.IsExecutingComplete);
        }

        [Test]
        public void TestExceuteNewBatchJobWhenNoDuplicateBatchFound()
        {
            var vm = new NewBatchJobViewModel(new NewBatchJob
            {

                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                         }
                }
            },
             fileSystem, fakeLoggerNewBatchJobVM,
             () => fakeFileShareApiAdminClient,
             fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored)).Returns(new BatchSearchResponse() { Total = 0 });
            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None)).Returns(new Result<IBatchHandle>() { IsSuccess = true, Data = fakeBatchHandle });
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored, A<string>.Ignored, A<string>.Ignored, CancellationToken.None, A<KeyValuePair<string, string>>.Ignored)).Returns(new Result<AddFileToBatchResponse> { IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored, CancellationToken.None)).Returns(new Result<CommitBatchResponse> { IsSuccess = true });

            var executeTask = vm.OnExecuteCommand();
            Assert.IsTrue(vm.IsCommitting);
            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None)).MustHaveHappened();
        }

        [Test]
        public void TestExceuteNewBatchJobWhenDuplicateBatchesfoundAndNoResponse()
        {
            var vm = new NewBatchJobViewModel(new NewBatchJob
            {

                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                         }
                }
            },
             fileSystem, fakeLoggerNewBatchJobVM,
             () => fakeFileShareApiAdminClient,
             fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored)).Returns(new BatchSearchResponse() { Total = 2 });
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.No);
            var executeTask = vm.OnExecuteCommand();
            Assert.AreEqual("File Share Service create new batch cancelled. ", vm.ExecutionResult);
            Assert.IsFalse(vm.IsCommitting);
            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None)).MustNotHaveHappened();
        }

        [Test]
        public void TestExceuteNewBatchJobWhenDuplicateBatchesfoundAndYesResponse()
        {
            var vm = new NewBatchJobViewModel(new NewBatchJob
            {

                DisplayName = "Create new Batch 123",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = "TestBU1",
                    Attributes = new List<KeyValueAttribute> { new KeyValueAttribute("BatchAttribute1", "Value1") },
                    Files =
                        {
                            new NewBatchFiles
                            {
                                ExpectedFileCount = 2,
                                MimeType = "text/plain",
                                SearchPath = @"c:\data\files\f*.txt"
                            }
                         }
                }
            },
             fileSystem, fakeLoggerNewBatchJobVM,
             () => fakeFileShareApiAdminClient,
             fakeCurrentDateTimeProvider, macroTransformer, dateTimeValidator, fakeMessageBoxService);

            A.CallTo(() => fakeFileShareApiAdminClient.Search(A<string>.Ignored, A<int?>.Ignored, A<int?>.Ignored)).Returns(new BatchSearchResponse() { Total = 2 });
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.Yes);
            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None)).Returns(new Result<IBatchHandle>() { IsSuccess = true, Data = fakeBatchHandle});
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored, A<string>.Ignored, A<string>.Ignored, CancellationToken.None, A<KeyValuePair<string, string>>.Ignored)).Returns(new Result<AddFileToBatchResponse> { IsSuccess = true });
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored, CancellationToken.None)).Returns(new Result<CommitBatchResponse> { IsSuccess = true });
            
            var executeTask = vm.OnExecuteCommand();
            Assert.IsTrue(vm.IsCommitting);
            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored, CancellationToken.None)).MustHaveHappened();
        }
    }
}
