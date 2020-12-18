using Git.SemVersioning.Scm;
using System;
using Xunit;

namespace Git.SemVersioning.Tests
{
    public class SemVersionTests
    {
        const string SHA = "8727a3eb8f17ac5e1cd1663beda8afa62da1d09a";

        SettingsForTest settings = new SettingsForTest();

        [Theory]
        [InlineData("foo")]
        [InlineData("v1")]
        [InlineData("v1.")]
        [InlineData("va1,2")]
        [InlineData("av1,2")]
        //[InlineData("v1.2a")]
        //[InlineData("v1.2-")]
        [InlineData("v1.-2")]
        [InlineData("v-1,2")]
        public void null_version_tags(string tagName)
        {
            // arrange
            var success = SemVersion.TryParse(new Tag(tagName, SHA), out var version);

            // assert
            Assert.False(success);
            Assert.Null(version);
        }

        [Theory]
        [InlineData("v1.2", 1, 2, 0, null, null)]
        [InlineData("v1.2-alpha", 1, 2, 0, "alpha", null)]
        [InlineData("v1.2-alpha5", 1, 2, 0, "alpha", 5)]
        [InlineData("v1.2-betaV.5", 1, 2, 0, "betaV.", 5)]
        [InlineData("v1.2.0-beta.5", 1, 2, 0, "beta.", 5)]
        [InlineData("v1.2.3-beta.5", 1, 2, 3, "beta.", 5)]
        [InlineData("v1.2.3-5", 1, 2, 3, null, 5)]
        [InlineData("v1.2.3-alpha.beta", 1, 2, 3, "alpha.beta", null)]
        [InlineData("1.2.3.4", 1, 2, 3, null, null)]
        [InlineData("v9.5.0.41-rc", 9, 5, 0, "rc", 41)]
        public void valid_version_tags(string tagName, int majorVersion, int minorVersion, int patchVersion, string suffix, int? preRelease)
        {
            // arrange
            var success = SemVersion.TryParse(new Tag(tagName, SHA), out var version);

            // assert
            Assert.True(success);
            Assert.NotNull(version);
            Assert.Equal(majorVersion, version.MajorVersion);
            Assert.Equal(minorVersion, version.MinorVersion);
            Assert.Equal(patchVersion, version.PatchVersion);
            Assert.Equal(suffix, version.PreReleasePrefix);
            Assert.Equal(preRelease, version.PreReleaseVersion);
        }

        [Theory]
        [InlineData("v1.0", "v2.0")]
        [InlineData("v1.1", "v1.2")]
        [InlineData("v1.1.0", "v1.1.1")]
        [InlineData("v1.1.0-RC", "v1.1.0")]
        [InlineData("v1.1.0-RC", "v1.1.0-RC.0")]
        [InlineData("v1.1.0-Alpha", "v1.1.0-Beta")]
        [InlineData("v1.1.0-Alpha.2", "v1.1.0-Beta.1")]
        [InlineData("v1.1.0-RC1", "v1.1.0-RC2")]
        [InlineData("v1.1.0-RC.1", "v1.1.0-RC.2")]
        public void TestSemVerOrdering(string lesserVersion, string greaterVersion)
        {
            SemVersion.TryParse(new Tag(lesserVersion, SHA), out var lesserSemVersion);
            SemVersion.TryParse(new Tag(greaterVersion, SHA), out var greaterSemVersion);
            Assert.True(greaterSemVersion.CompareTo(lesserSemVersion) > 0);
        }

        [Fact]
        public void TestSemVerOrdering_Differing_CommitCount()
        {
            SemVersion v1 = new SemVersion();
            SemVersion v2 = new SemVersion();
            v2.UpdateFromCommit(new Commit("", SHA), settings);

            Assert.True(v1.CompareTo(new SemVersion()) == 0);
            Assert.True(v1.CompareTo(v2) < 0);
        }

        [Fact]
        public void TestPreReleaseVersion()
        {
            var success = SemVersion.TryParse(new Tag("1.0.0-RC.2", SHA), out var pre);
            Assert.True(success);
            SemVersion v = new SemVersion();
            v.UpdateFromCommit(new Commit("tagged prerelease", SHA), settings, pre);

            v.UpdateFromCommit(new Commit("text", SHA), settings);
            v.UpdateFromCommit(new Commit("text", SHA), settings);
            v.UpdateFromCommit(new Commit("text", SHA), settings);

            v.CalculateNewVersion(false, settings.DefaultPrerelease);
            Assert.Equal("1.0.0-RC.3+003", v.ToInfoVersionString());
        }

        [Theory]
        [InlineData("1.1.1-NEXT+003", "v1.1.0", "Commit 1", "Commit 2", "Commit 3")]
        [InlineData("1.2.1-NEXT+003", "v1.2", "Commit 1", "Commit 2", "Commit 3")]
        [InlineData("2.2.3-NEXT+001", "v2.2.2", "Commit 1")]
        [InlineData("3.0.0-NEXT+001", "v2.2.2", "refactor!: drop some support")]
        [InlineData("3.0.0-NEXT+001", "v2.2.2", "feat:  new api\r\n\r\nReplacing the old API\r\n\r\nBREAKING CHANGE: drop support")]
        [InlineData("2.3.0-NEXT+001", "v2.2.2", "feat:  new")]
        [InlineData("2.2.3-NEXT+001", "v2.2.2", "fix:   bug")]
        [InlineData("1.0.2-NEXT+002", "v1.0.0", "fix:   bug", "fix: bug")]

        [InlineData("3.0.0", "v2.2.2", "refactor!: drop some support", "release: it")]
        [InlineData("2.2.3", "v2.2.2", "fix:   bug", "release: ok")]
        [InlineData("1.0.2", "v1.0.0", "fix:   bug", "fix: bug", "release: ok")]
        [InlineData("1.0.4-NEXT+002", "v1.0.0", "fix:   bug", "fix: bug", "release: ok", "fix: bug", "fix: bug")]
        [InlineData("2.3.0", "v2.2.2", "feat:  wow", "release: ok")]
        [InlineData("3.0.0", "v2.2.2", "feat!: WOW", "release: ok")]
        [InlineData("2.2.4-NEXT+001", "v2.2.2", "fix:   bug", "release: ok", "fix: bug")]
        [InlineData("2.3.1-NEXT+001", "v2.2.2", "feat:  new", "release: ok", "fix: bug")]
        [InlineData("3.0.1-NEXT+001", "v2.2.2", "feat!: WOW", "release: ok", "fix: bug")]
        [InlineData("2.3.0-NEXT+001", "v2.2.2", "fix:   bug", "release: ok", "feat: new")]
        [InlineData("2.4.0-NEXT+001", "v2.2.2", "feat:  new", "release: ok", "feat: new")]
        [InlineData("3.1.0-NEXT+001", "v2.2.2", "feat!: WOW", "release: ok", "feat: new")]
        [InlineData("3.0.0-NEXT+001", "v2.2.2", "fix:   bug", "release: ok", "feat!: WOW")]
        [InlineData("3.0.0-NEXT+001", "v2.2.2", "feat:  new", "release: ok", "feat!: WOW")]
        [InlineData("4.0.0-NEXT+001", "v2.2.2", "feat!: WOW", "release: ok", "feat!: WOW")]
        //[InlineData("9.9.9", "v2.2.2", "feat!: WOW", "release: I name thee v9.9.9")]
        //[InlineData("9.9.9-NINE.9", "v2.2.2", "feat!: WOW", "release: I name thee v9.9.9-NINE.9")]
        //[InlineData("2.2.3-RC.2", "v2.2.2", "release: v2.2.3-RC.0", "fix: bug", "release: it", "fix: bug", "release: it")]
        public void TestUpdateFromCommit(string expectedVersion, string tagName, params string[] commits)
        {
            // act
            SemVersion.TryParse(new Tag(tagName, SHA), out var version);

            foreach (var commit in commits)
            {
                version.UpdateFromCommit(new Commit(commit, SHA), settings);
            }

            version.CalculateNewVersion(false, settings.DefaultPrerelease);

            // assert
            Assert.Equal(expectedVersion, version.ToInfoVersionString());
        }

        [Theory]
        [InlineData("1.1.1-Beta.3+001", "v1.1.0", "1.1.1-Beta.2", "Commit 1")]
        [InlineData("2.2.3-Alpha+001", "v2.2.2", "2.2.3-Alpha", "Commit 1")]
        [InlineData("2.2.3-Alpha.1+001", "v2.2.2", "2.2.3-Alpha.0", "fix: bug")]
        [InlineData("2.2.3-Beta.4+001", "v2.2.2", "2.2.3-Beta.3", "fix: bug")]
        [InlineData("3.0.0-Beta.1+001", "v2.2.2", "3.0.0-Beta.0", "refactor!: drop some support")]
        [InlineData("3.0.0-Beta.0+001", "v2.2.2", "2.2.3-Beta.0", "refactor!: drop some support")]
        [InlineData("3.0.0-Beta.1+001", "v2.2.2", "3.0.0-Beta.0", "feat: new api\r\n\r\nA message\r\n\r\nBREAKING CHANGE: drop support")]
        [InlineData("3.0.0-Beta.0+001", "v2.2.2", "2.2.3-Beta.0", "feat: new api\r\n\r\nA message\r\n\r\nBREAKING CHANGE: drop support")]
        [InlineData("2.3.0-NEXT.2+001", "v2.2.2", "2.3.0-NEXT.1", "feat: new")]
        [InlineData("2.3.0-NEXT.0+001", "v2.2.2", "2.2.3-NEXT.1", "feat: new")]
        [InlineData("2.3.0-NEXT+002", "v2.2.2", "1.0.0-NEXT", "feat: new")]
        [InlineData("2.3.0-NEXT+001", "v2.2.2", "2.2.3-NEXT", "feat: new")]
        public void TestUpdateFromCommit_prerelease_dirty(string expectedVersion, string tagName, string prerelease, string commit)
        {
            // act
            SemVersion.TryParse(new Tag(tagName, SHA), out var version);
            SemVersion.TryParse(new Tag(prerelease, SHA), out var pre);

            version.UpdateFromCommit(new Commit("Taged commit", SHA), settings, pre);
            version.UpdateFromCommit(new Commit(commit, SHA), settings);


            version.CalculateNewVersion(true, settings.DefaultPrerelease);

            // assert
            Assert.Equal(expectedVersion, version.ToInfoVersionString());
        }

        [Theory]
        [InlineData("1.1.1-NEXT", "v1.1.0")]
        [InlineData("2.2.3-NEXT", "v2.2.2")]
        public void TestIncrementVersion_dirty(string expectedVersion, string tagName)
        {
            // act
            SemVersion.TryParse(new Tag(tagName, SHA), out var actualVersion);

            actualVersion.CalculateNewVersion(true, settings.DefaultPrerelease);

            // assert
            Assert.Equal(expectedVersion, actualVersion.ToInfoVersionString());
        }

        [Fact]
        public void TestInfoVersionSha()
        {
            SemVersion.TryParse(new Tag("1.0.0", SHA), out var actualVersion);
            Assert.Equal("1.0.0+sha.8727a3e", actualVersion.ToString());
        }
    }
}
