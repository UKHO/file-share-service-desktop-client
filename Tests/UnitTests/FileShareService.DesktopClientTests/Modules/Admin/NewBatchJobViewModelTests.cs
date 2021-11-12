using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareClient.Models;
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
        private  ILogger<NewBatchJobViewModel> fakeLoggerNewBatchJobVM =null!;


        [SetUp]
        public void Setup()
        {
            fileSystem = new MockFileSystem();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakeCurrentDateTimeProvider = A.Fake<ICurrentDateTimeProvider>();
            fakeLoggerNewBatchJobVM = A.Fake<ILogger<NewBatchJobViewModel>>();
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            var expandedAttributes = vm.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            var expandedAttributes = vm.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedWeekNumber = WeekNumber.GetUKHOWeekFromDateTime(DateTime.UtcNow.AddDays(offset * 7)).Week.ToString();            

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
                    Attributes = new Dictionary<string, string>
                        {
                            {"BatchAttribute1", "Value1"},
                            {"WeekYearMacro1", input},
                            {"WeekYearMacro2", "Padding " + input},
                            {"WeekYearMacro3", $"Padding {input} and Right Padding"},
                            {"MultiWeekYearMacro", $"Padding {input} and {input} with Right Padding"}
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
                fakeCurrentDateTimeProvider);

            var expandedAttributes = vm.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);            
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);


            Assert.AreEqual(input, vm.RawExpiryDate);

            var expiry = vm.ExpiryDate;
            Assert.IsNotNull(expiry);
            Assert.AreEqual(DateTime.UtcNow.AddDays(dayOffsetFromNow).Date, expiry!.Value.Date);
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            Assert.AreEqual(2, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] {file1FullFileName, file2FullFileName},
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));

            Assert.IsTrue(vm.ExcecuteJobCommand.CanExecute());
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
                fileSystem, fakeLoggerNewBatchJobVM,
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            Assert.AreEqual(1, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] {file1FullFileName},
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
                        Attributes = new Dictionary<string, string> {{"BatchAttribute1", "Value1"}},
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
                fakeCurrentDateTimeProvider);

            Assert.AreEqual(1, vm.Files.SelectMany(f => f.Files).Count());
            CollectionAssert.AreEqual(new[] {file1FullFileName},
                vm.Files.SelectMany(f => f.Files.Select(fi => fi.FullName)));


            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());
        }

        [Test]
        public async Task TestSimpleExceuteNewBatchJob()
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
                fileSystem, fakeLoggerNewBatchJobVM,
                () => fakeFileShareApiAdminClient,
                fakeCurrentDateTimeProvider);

            Assert.AreEqual("Create new Batch 123", vm.DisplayName);

            var batchHandle = A.Fake<IBatchHandle>();

            var createBatchTcs = new TaskCompletionSource<IBatchHandle>();
            var addFileToBatchTcs = new TaskCompletionSource();
            var commitBatchTcs = new TaskCompletionSource();

            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored))
                .Returns(createBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(addFileToBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored)).Returns(commitBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.Committed});

            var executeTask = vm.OnExecuteCommand();
            vm.ExcecuteJobCommand.Execute();
            Assert.IsTrue(vm.IsExecuting);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            createBatchTcs.SetResult(batchHandle);
            addFileToBatchTcs.SetResult();
            commitBatchTcs.SetResult();
            

            await executeTask;
            Assert.IsFalse(vm.IsExecuting);
            Assert.IsTrue(vm.ExcecuteJobCommand.CanExecute());
            Assert.IsTrue(vm.IsExecutingComplete);
            Assert.IsFalse(vm.IsCommitting);
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(batchHandle)).MustHaveHappened();
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(batchHandle)).MustHaveHappened();
            vm.CloseExecutionCommand.Execute();
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
                    Attributes = new Dictionary<string, string>()
                        ,
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
            fakeCurrentDateTimeProvider);
            
            // Testing method returns true when committed
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.Committed });
            var result = await vm.CheckBatchIsCommitted(fakeFileShareApiAdminClient,batchHandle,1);
            Assert.IsTrue(result);

            // Testing method returns false when response is not committed within wait time
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored)).Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.CommitInProgress });
            result = await vm.CheckBatchIsCommitted(fakeFileShareApiAdminClient, batchHandle, 0.1);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task TestExceuteNewBatchJobHasValidationErrors()
        {
            var file1FullFileName = @"c:/data/files/f1.txt";
            fileSystem.AddFile(file1FullFileName, new MockFileData("File 1 contents"));

            var vm = new NewBatchJobViewModel(new NewBatchJob
            {
                DisplayName = "Create new Batch",
                ActionParams = new NewBatchJobParams
                {
                    BusinessUnit = " ",
                    Attributes = new Dictionary<string, string> { { "BatchAttribute1", "Value1" } },
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
                fakeCurrentDateTimeProvider);

            Assert.AreEqual("Create new Batch", vm.DisplayName);

            var batchHandle = A.Fake<IBatchHandle>();

            var createBatchTcs = new TaskCompletionSource<IBatchHandle>();
            var addFileToBatchTcs = new TaskCompletionSource();
            var commitBatchTcs = new TaskCompletionSource();


            A.CallTo(() => fakeFileShareApiAdminClient.CreateBatchAsync(A<BatchModel>.Ignored))
                .Returns(createBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.AddFileToBatch(A<IBatchHandle>.Ignored, A<Stream>.Ignored,
                A<string>.Ignored, A<string>.Ignored)).Returns(addFileToBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.CommitBatch(A<IBatchHandle>.Ignored)).Returns(commitBatchTcs.Task);
            A.CallTo(() => fakeFileShareApiAdminClient.GetBatchStatusAsync(A<IBatchHandle>.Ignored))
                .Returns(new BatchStatusResponse() { BatchId = "Ingnore", Status = BatchStatusResponse.StatusEnum.Committed });

            var executeTask = vm.OnExecuteCommand();
            vm.ExcecuteJobCommand.Execute();
            Assert.IsTrue(vm.IsExecuting);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            createBatchTcs.SetResult(batchHandle);
            addFileToBatchTcs.SetResult();
            commitBatchTcs.SetResult();

            await executeTask;
            Assert.IsFalse(vm.IsExecuting);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            Assert.AreEqual(1, vm.ValidationErrors.Count);
            Assert.AreEqual("Business Unit is missing or is not specified.", vm.ValidationErrors[0]);
        }        
    }
}