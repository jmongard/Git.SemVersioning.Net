using Git.SemVersioning.Scm;
using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.JustMock;
using Xunit;

namespace Git.SemVersioning.Tests.Scm
{
    public class GitProviderTest
    {
        static SettingsForTest settings = new SettingsForTest();
        private GitProvider gitProvider = new GitProvider(settings);

        [Fact]
        public void commits_when_head_is_null()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head).Returns<LibGit2Sharp.Branch>(null);

            // act
            var commits = gitProvider.GetHeadCommitsFromRepository(repository);

            // assert
            Assert.NotNull(commits);
            Assert.Empty(commits);
        }

        [Fact]
        public void commits_when_head_commits_is_null()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns<LibGit2Sharp.ICommitLog>(null);

            // act
            var commits = gitProvider.GetHeadCommitsFromRepository(repository);

            // assert
            Assert.NotNull(commits);
            Assert.Empty(commits);
        }

        [Fact]
        public void commits_when_commits_found()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            var commit1 = Mock.Create<LibGit2Sharp.Commit>();
            var commit2 = Mock.Create<LibGit2Sharp.Commit>();
            var commit3 = Mock.Create<LibGit2Sharp.Commit>();
            Mock.Arrange(() => commit1.Sha).Returns("first");
            Mock.Arrange(() => commit2.Sha).Returns("second");
            Mock.Arrange(() => commit3.Sha).Returns("third");
            Mock.Arrange(() => repository.Head.Commits.GetEnumerator()).Returns(new List<LibGit2Sharp.Commit> { commit1, commit2, commit3 }.GetEnumerator());

            // act
            var commits = gitProvider.GetHeadCommitsFromRepository(repository).ToList();

            // assert
            Assert.NotNull(commits);
            Assert.Equal(3, commits.Count());
            Assert.Equal("first", commits[0].Sha);
            Assert.Equal("second", commits[1].Sha);
            Assert.Equal("third", commits[2].Sha);
        }

        [Fact]
        public void tags_when_tags_is_null()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Tags).Returns(null as LibGit2Sharp.TagCollection);

            // act
            var tags = gitProvider.GetTagsFromRepository(repository);

            // assert
            Assert.NotNull(tags);
            Assert.Empty(tags);
        }

        [Fact]
        public void GenerateFileContents_when_default_mock()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("0.0.0", actualContents.ToInfoVersionString());
        }

        [Fact]
        public void GenerateFileContents_when_commits_without_tags()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns(MockCommitLog(new[] { "third", "second", "first" }));

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("0.0.1-NEXT+003", actualContents.ToInfoVersionString());
        }

        [Fact]
        public void GenerateFileContents_when_commits_and_tags()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns(MockCommitLog(new[] { "third", "second", "first", "zero" }));
            Mock.Arrange(() => repository.Tags).Returns(MockTags(new Dictionary<string, string> { { "zero", "v1.2" } }));

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("1.2.1-NEXT+003", actualContents.ToInfoVersionString());
        }

        [Fact]
        public void GenerateFileContents_tag_without_sha()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns(MockCommitLog(new[] { "third", "second", "first", "zero" }));
            Mock.Arrange(() => repository.Tags).Returns(MockTags(new Dictionary<string, string> { { "does-not-exist", "v1.2" } }));

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("0.0.1-NEXT+004", actualContents.ToInfoVersionString());
        }

        [Fact]
        public void GenerateFileContents_non_version_tag()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns(MockCommitLog(new[] { "third", "second", "first", "zero" }));
            Mock.Arrange(() => repository.Tags).Returns(MockTags(new Dictionary<string, string> { { "zero", "1,2" } }));

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("0.0.1-NEXT+004", actualContents.ToInfoVersionString());
        }

        [Fact]
        public void GenerateFileContents_multiple_tags()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns(MockCommitLog(new[] { "third", "second", "first", "zero" }));
            Mock.Arrange(() => repository.Tags).Returns(MockTags(new Dictionary<string, string> { { "zero", "v1.2" }, { "second", "v1.3" } }));

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("1.3.1-NEXT+001", actualContents.ToInfoVersionString());
        }

        [Fact]
        public void GenerateFileContents_prerelease_tag()
        {
            // arrange
            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns(MockCommitLog(new[] { "third", "second", "first", "zero" }));
            Mock.Arrange(() => repository.Tags).Returns(MockTags(new Dictionary<string, string> { { "zero", "v1.2" }, { "second", "v1.3-RC" } }));

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("1.3.0-RC+001", actualContents.ToInfoVersionString());
        }

        [Fact]
        public void TestParentsThrowingException()
        {
            // arrange
            var mockCommit1 = Mock.Create<LibGit2Sharp.Commit>();
            Mock.Arrange(() => mockCommit1.Sha).Returns("one");
            Mock.Arrange(() => mockCommit1.Parents).Throws(new Exception("Missing parent"));

            var mockCommit2 = Mock.Create<LibGit2Sharp.Commit>();
            Mock.Arrange(() => mockCommit2.Sha).Returns("two");
            Mock.Arrange(() => mockCommit2.Parents).Returns(new[] { mockCommit1 });

            var repository = Mock.Create<LibGit2Sharp.IRepository>();
            Mock.Arrange(() => repository.Head.Commits).Returns(MockCommitLog(new[] { mockCommit2 }));
            Mock.Arrange(() => repository.Tags).Returns(MockTags(new Dictionary<string, string> { { "oneX", "v1.2" } }));

            // act
            var actualContents = gitProvider.GetSemVersion(repository);

            // assert
            Assert.Equal("0.0.1-NEXT+001", actualContents.ToInfoVersionString());
        }

        private static LibGit2Sharp.ICommitLog MockCommitLog(IEnumerable<string> commitShas)
        {
            return MockCommitLog(AsCommits(commitShas));
        }

        private static LibGit2Sharp.ICommitLog MockCommitLog(IEnumerable<LibGit2Sharp.Commit> enumerable)
        {
            var commitLog = Mock.Create<LibGit2Sharp.ICommitLog>();
            Mock.Arrange(() => commitLog.GetEnumerator()).Returns(enumerable.GetEnumerator());
            return commitLog;
        }

        private static LibGit2Sharp.TagCollection MockTags(IDictionary<string, string> tagShaToName)
        {
            var tagCollection = Mock.Create<LibGit2Sharp.TagCollection>();
            var mockTags = tagShaToName.Select(shaAndName => MockTag(shaAndName.Key, shaAndName.Value));
            Mock.Arrange(() => tagCollection.GetEnumerator()).Returns(mockTags.GetEnumerator());
            return tagCollection;
        }

        private static IEnumerable<LibGit2Sharp.Commit> AsCommits(IEnumerable<string> shas)
        {
            return shas.Take(1).Select(sha => MockCommit(sha, AsCommits(shas.Skip(1))));
        }

        private static LibGit2Sharp.Commit MockCommit(string commitSha, IEnumerable<LibGit2Sharp.Commit> parents)
        {
            var mockCommit = Mock.Create<LibGit2Sharp.Commit>();
            Mock.Arrange(() => mockCommit.Sha).Returns(commitSha);
            Mock.Arrange(() => mockCommit.Parents).Returns(parents);
            return mockCommit;
        }

        private static LibGit2Sharp.Tag MockTag(string targetSha, string name)
        {
            var mockTag = Mock.Create<LibGit2Sharp.Tag>();
            Mock.Arrange(() => mockTag.Target.Sha).Returns(targetSha);
            Mock.Arrange(() => mockTag.FriendlyName).Returns(name);
            return mockTag;
        }
    }
}
