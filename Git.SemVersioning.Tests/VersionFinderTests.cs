using Git.SemVersioning.Scm;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Git.SemVersioning.Tests
{
    public class VersionFinderTests
    {
        [Fact]
        public void release_and_prerelease_tags()
        {
            // arrange
            var commits = new List<string>
            {
                "sixth",
                "fifth",
                "fourth",
                "third",
                "second",
                "first",
                "zero",
            };
            var tags = new List<Tag>
            {
                new Tag("v1.2", "zero"),
                new Tag("v1.3.1-RC", "third"),
            };

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("1.3.1", versions.ToVersionString());
            Assert.Equal("1.2.0.6", versions.ToFileVersionString());
            Assert.Equal("1.3.1-RC+003", versions.ToInfoVersionString());
        }

        [Fact]
        public void release_tag_only()
        {
            // arrange
            var commits = new List<string>
            {
                "sixth",
                "fifth",
                "fourth",
                "third",
                "second",
                "first",
                "zero",
            };
            var tags = new List<Tag>
            {
                new Tag("v1.2.1", "zero"),
            };

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("1.2.2", versions.ToVersionString());
            Assert.Equal("1.2.2-NEXT+006", versions.ToInfoVersionString());
            Assert.Equal("1.2.256.6", versions.ToFileVersionString());
        }

        [Fact]
        public void multiple_release_tags()
        {
            // arrange
            var commits = new List<string>
            {
                "sixth",
                "fifth",
                "fourth",
                "third",
                "second",
                "first",
                "zero",
            };
            var tags = new List<Tag>
            {
                new Tag("v1.2", "zero"),
                new Tag("v1.3.1-RC", "third"),
                new Tag("v1.3.1", "fifth"),
            };

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("1.3.2", versions.ToVersionString());
            Assert.Equal("1.3.2-NEXT+001", versions.ToInfoVersionString());
            Assert.Equal("1.3.256.1", versions.ToFileVersionString());
        }

        [Fact]
        public void no_commits()
        {
            // arrange
            var commits = new List<string>();
            var tags = new List<Tag>();

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("0.0.0", versions.ToVersionString());
            Assert.Equal("0.0.0.0", versions.ToFileVersionString());
            Assert.Equal("0.0.0", versions.ToInfoVersionString());
        }

        [Fact]
        public void one_commit_no_tags()
        {
            // arrange
            var commits = new List<string> { "first" };
            var tags = new List<Tag>();

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("0.0.1", versions.ToVersionString());
            Assert.Equal("0.0.1-NEXT+001", versions.ToInfoVersionString());
            Assert.Equal("0.0.128.1", versions.ToFileVersionString());
        }

        [Fact]
        public void one_commit_one_tag()
        {
            // arrange
            var commits = new List<string> { "first" };
            var tags = new List<Tag> { new Tag("v1.0", "first") };

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("1.0.0", versions.ToVersionString());
            Assert.Equal("1.0.0.0", versions.ToFileVersionString());
            Assert.Equal("1.0.0", versions.ToInfoVersionString());
        }

        [Fact]
        public void two_commit_one_pretag()
        {
            // arrange
            var commits = new List<string> { "first", "second" };
            var tags = new List<Tag> { new Tag("v1.0.0-Alpha.1", "first") };

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("1.0.0", versions.ToVersionString());
            Assert.Equal("0.0.0.2", versions.ToFileVersionString());
            Assert.Equal("1.0.0-Alpha.1", versions.ToInfoVersionString());
        }

        [Fact]
        public void multiple_tags_same_commit()
        {
            // arrange
            var commits = new List<string> { "first" };
            var tags = new List<Tag>
            {
                new Tag("foo", "first"),
                new Tag("v1.0", "first"),
                new Tag("bar", "first"),
            };

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("1.0.0", versions.ToVersionString());
            Assert.Equal("1.0.0.0", versions.ToFileVersionString());
            Assert.Equal("1.0.0", versions.ToInfoVersionString());
        }

        [Fact]
        public void multiple_prerelease_tags_same_commit()
        {
            // arrange
            var commits = new List<string>
            {
                "third",
                "second",
                "first",
            };
            var tags = new List<Tag>
            {
                new Tag("v1.0.0", "first"),
                new Tag("foo", "second"),
                new Tag("v1.0.1-RC.2", "second"),
                new Tag("bar", "second"),
            };

            // act
            var versions = GetVersion(commits, tags);

            // assert
            Assert.Equal("1.0.1", versions.ToVersionString());
            Assert.Equal("1.0.0.2", versions.ToFileVersionString());
            Assert.Equal("1.0.1-RC.3+001", versions.ToInfoVersionString());
        }

        [Fact]
        public void TestDox()
        {
            // given
            var commits = new List<string> { "1" };
            var tags = new List<Tag>
            {
                new Tag("v1.2", "1"),
                new Tag("v1.3-RC", "5"),
                new Tag("v1.3", "9"),
            };

            // when 
            var versions = GetVersion(commits, tags);

            // then 
            Assert.Equal("1.2.0", versions.ToVersionString());
            Assert.Equal("1.2.0.0", versions.ToFileVersionString());
            Assert.Equal("1.2.0", versions.ToInfoVersionString());

            // given
            commits.Insert(0, "2");
            commits.Insert(0, "3");
            commits.Insert(0, "4");

            // when 
            versions = GetVersion(commits, tags);

            // then 
            Assert.Equal("1.2.1", versions.ToVersionString());
            Assert.Equal("1.2.128.3", versions.ToFileVersionString());
            Assert.Equal("1.2.1-NEXT+003", versions.ToInfoVersionString());

            // given
            commits.Insert(0, "5");

            // when 
            versions = GetVersion(commits, tags);

            // then 
            Assert.Equal("1.3.0", versions.ToVersionString());
            Assert.Equal("1.2.0.4", versions.ToFileVersionString());
            Assert.Equal("1.3.0-RC", versions.ToInfoVersionString());

            // given
            commits.Insert(0, "6");
            commits.Insert(0, "7");
            commits.Insert(0, "8");

            // when 
            versions = GetVersion(commits, tags);

            // then 
            Assert.Equal("1.3.0", versions.ToVersionString());
            Assert.Equal("1.2.0.7", versions.ToFileVersionString());
            Assert.Equal("1.3.0-RC+003", versions.ToInfoVersionString());

            // given
            commits.Insert(0, "9");

            // when 
            versions = GetVersion(commits, tags);

            // then 
            Assert.Equal("1.3.0", versions.ToVersionString());
            Assert.Equal("1.3.0.0", versions.ToFileVersionString());
            Assert.Equal("1.3.0", versions.ToInfoVersionString());
        }


        private SemVersion GetVersion(List<string> commits, List<Tag> tags, bool dirty = false)
        {
            return new VersionFinder(tags, new SettingsForTest()).GetVersion(AsCommits(commits).FirstOrDefault(), dirty);
        }

        private IEnumerable<Commit> AsCommits(IEnumerable<string> shas)
        {
            return shas.Take(1).Select(sha => new Commit(string.Empty, sha, AsCommits(shas.Skip(1))));
        }
    }
}
