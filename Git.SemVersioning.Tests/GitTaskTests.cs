using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Telerik.JustMock;
using Xunit;

namespace Git.SemVersioning.Tests
{
    public class GitTaskTests
    {
        [Fact]
        public void TestGenerateFileContents()
        {
            string dir = Directory.GetCurrentDirectory();
            var t = CreateGitTask(Path.Combine(dir, Path.GetRandomFileName()));
            try
            {
                var result = t.Execute();
                Assert.True(result);
                Assert.True(File.Exists(t.OutputFilePath));

                var actualContents = File.ReadAllText(t.OutputFilePath);
                Assert.Contains("assembly: AssemblyVersion", actualContents);
                Assert.Contains("assembly: AssemblyFileVersion", actualContents);
                Assert.Contains("assembly: AssemblyInformationalVersion", actualContents);
            }
            finally
            {
                File.Delete(t.OutputFilePath);
            }
        }

        [Fact]
        public void TestGenerate_No_git_repo()
        {
            var t = CreateGitTask(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            try
            {
                var result = t.Execute();
                Assert.True(result);
                Assert.False(File.Exists(t.OutputFilePath));
            }
            finally
            {
                File.Delete(t.OutputFilePath);
            }
        }

        private GitTask CreateGitTask(string outputFilePath)
        {
            var buildEngine = Mock.Create<IBuildEngine>();

            return new GitTask
            {
                BuildEngine = buildEngine,
                OutputFilePath = outputFilePath,
                IncludeAssemblyVersion = true,
                IncludeAssemblyFileVersion = true,
                IncludeAssemblyInformationalVersion = true
            };
        }
    }
}
