using Git.SemVersioning.Scm;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Git.SemVersioning
{
    internal class SemVersion : IComparable<SemVersion>
    {
        static readonly Regex tagPattern = new Regex(@"(?<Major>0|[1-9]\d*)\.(?<Minor>0|[1-9]\d*)(?:\.(?<Patch>0|[1-9]\d*)(?:\.(?<Revision>0|[1-9]\d*))?)?"
            + @"(?:-(?<PreRelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?"
            + @"(?:\+(?<BuildMetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?", RegexOptions.Compiled);

        public int MajorVersion { get; private set; }

        public int MinorVersion { get; private set; }

        public int PatchVersion { get; private set; }

        public int CommitCount { get; private set; }

        public string PreReleasePrefix { get; private set; }

        public int? PreReleaseVersion { get; private set; }

        public string Sha { get; private set; }

        public bool IsPreRelease => PreReleasePrefix != null || PreReleaseVersion > 0;

        public SemVersion()
        {
        }

        public static bool TryParse(IRefInfo refInfo, out SemVersion version)
        {
            var match = tagPattern.Match(refInfo.Text);
            if (!match.Success)
            {
                version = null;
                return false;
            }

            var preReleaseGroup = match.Groups["PreRelease"];
            var patchGroup = match.Groups["Patch"];
            var revisionGroup = match.Groups["Revision"];

            version = new SemVersion
            {
                Sha = refInfo.Sha,
                MajorVersion = int.Parse(match.Groups["Major"].Value),
                MinorVersion = int.Parse(match.Groups["Minor"].Value),
                PatchVersion = patchGroup.Success ? int.Parse(patchGroup.Value) : 0,
                PreRelease = preReleaseGroup.Success ? preReleaseGroup.Value : null
            };
            if (version.IsPreRelease && !version.PreReleaseVersion.HasValue && revisionGroup.Success)
            {
                version.PreReleaseVersion = int.Parse(revisionGroup.Value);
            }
            System.Diagnostics.Debug.WriteLine("Found version: {0} in text {1} ", version, refInfo.Text);
            return true;
        }

        private string PreRelease
        {
            set
            {
                var length = value?.Length;
                var prefixLength = length;
                while (prefixLength > 0 && char.IsDigit(value[prefixLength.Value - 1]))
                {
                    prefixLength -= 1;
                }
                if (prefixLength < length)
                {
                    PreReleaseVersion = int.Parse(value.Substring(prefixLength.Value));
                    PreReleasePrefix = prefixLength > 0 ? value.Substring(0, prefixLength.Value) : null;
                }
                else
                {
                    PreReleaseVersion = null;
                    PreReleasePrefix = value;
                }
            }
        }

        public int CompareTo(SemVersion other)
        {
            var i = MajorVersion.CompareTo(other.MajorVersion + other.bumpMajor);
            if (i == 0)
            {
                i = MinorVersion.CompareTo(other.MinorVersion + other.bumpMinor);
            }
            if (i == 0)
            {
                i = PatchVersion.CompareTo(other.PatchVersion + other.bumpPatch);
            }
            if (i == 0)
            {
                i = -IsPreRelease.CompareTo(other.IsPreRelease);
            }
            if (i == 0)
            {
                i = (PreReleasePrefix ?? string.Empty).CompareTo(other.PreReleasePrefix ?? string.Empty);
            }
            if (i == 0)
            {
                i = (PreReleaseVersion ?? 0).CompareTo(other.PreReleaseVersion + bumpPre ?? 0);
            }
            if (i == 0)
            {
                i = CommitCount.CompareTo(other.CommitCount);
            }
            return i;
        }

        private int bumpPatch = 0;
        private int bumpMinor = 0;
        private int bumpMajor = 0;
        private int bumpPre = 0;

        private SemVersion currentPreRelease;

        public void UpdateFromCommit(IRefInfo commit, ISettings settings, SemVersion preReleaseFromTag = null)
        {
            Sha = commit.Sha;
            CommitCount += 1;

            if (preReleaseFromTag != null)
            {
                var v = currentPreRelease ?? this;
                if (preReleaseFromTag.CompareTo(v) > 0)
                {
                    currentPreRelease = preReleaseFromTag;
                }
            }
            else if (IsRelease(commit, settings))
            {
                var v = currentPreRelease ?? this;
                ApplyPendingChanges(v);
                v.CommitCount = 0;
            }
            else if (currentPreRelease != null)
            {
                currentPreRelease.CommitCount += 1;
                if (currentPreRelease.MajorVersion == MajorVersion
                    && Regex.IsMatch(commit.Text, settings.MajorPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    bumpMajor = 1;
                }
                else if (currentPreRelease.MajorVersion == MajorVersion
                    && currentPreRelease.MinorVersion == MinorVersion
                    && Regex.IsMatch(commit.Text, settings.MinorPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    bumpMinor = 1;
                }
                else
                {
                    bumpPre = 1;
                }
            }
            else
            {
                if (Regex.IsMatch(commit.Text, settings.MajorPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    bumpMajor += 1;
                }
                else if (Regex.IsMatch(commit.Text, settings.MinorPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    bumpMinor += 1;
                }
                else if (Regex.IsMatch(commit.Text, settings.PatchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    bumpPatch += 1;
                }
            }
        }

        public static bool IsRelease(IRefInfo commit, ISettings settings)
        {
            return Regex.IsMatch(commit.Text, settings.ReleasePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }

        public void CalculateNewVersion(bool isDirty, string defaultPrerelease)
        {
            if (currentPreRelease == null && (CommitCount > 0 || isDirty))
            {
                PreRelease = defaultPrerelease;
                if (bumpMajor + bumpMinor + bumpPatch == 0)
                {
                    bumpPatch = 1;
                }
            }
            ApplyPendingChanges(currentPreRelease ?? this);
        }

        private void ApplyPendingChanges(SemVersion v)
        {
            if (bumpMajor > 0)
            {
                v.MajorVersion += bumpMajor;
                v.MinorVersion = 0;
                v.PatchVersion = 0;
                v.PreReleaseVersion = v.PreReleaseVersion.HasValue ? (int?)0 : null;
            }
            else if (bumpMinor > 0)
            {
                v.MinorVersion += bumpMinor;
                v.PatchVersion = 0;
                v.PreReleaseVersion = v.PreReleaseVersion.HasValue ? (int?)0 : null;
            }
            else if (bumpPatch > 0)
            {
                v.PatchVersion += bumpPatch;
                v.PreReleaseVersion = v.PreReleaseVersion.HasValue ? (int?)0 : null;
            }
            else if (bumpPre > 0)
            {
                v.PreReleaseVersion = v.PreReleaseVersion.HasValue ? v.PreReleaseVersion + bumpPre : null;
            }
            bumpMajor = bumpMinor = bumpPatch = bumpPre = 0;
        }

        public string ToVersionString()
        {
            var v = currentPreRelease ?? this;
            return new Version(v.MajorVersion, v.MinorVersion, v.PatchVersion).ToString();
        }

        public string ToFileVersionString(int prereleaseBits = 7)
        {
            var v = this; //currentPreRelease ?? this;
            int preReleaseMask = (1 << prereleaseBits) - 1;
            return new Version(v.MajorVersion, v.MinorVersion,
                Math.Min(v.PatchVersion << prereleaseBits | (v.PreReleaseVersion ?? 0) & preReleaseMask, 0xFFFE), v.CommitCount).ToString();
        }

        public string ToInfoVersionString(string commitCountStringFormat = "d3", int shaLength = 0, bool v2 = true)
        {
            var v = currentPreRelease ?? this;
            var builder = new StringBuilder();
            builder.Append(v.MajorVersion).Append('.').Append(v.MinorVersion).Append('.').Append(v.PatchVersion);

            if (v.IsPreRelease)
            {
                var preReleasePrefix = v.PreReleasePrefix;
                var preReleaseVersion = v.PreReleaseVersion;
                builder.Append('-');
                if (preReleasePrefix != null)
                {
                    builder.Append(v2 ? preReleasePrefix : Regex.Replace(preReleasePrefix, "[^0-9A-Za-z-]", string.Empty));
                }
                if (preReleaseVersion.HasValue)
                {
                    builder.Append(preReleaseVersion.Value);
                }
            }
            if (v2)
            {
                var metaSeparator = '+';
                var commitCount = v.CommitCount;
                if (commitCount > 0 && commitCountStringFormat.Length > 0)
                {
                    builder.Append(metaSeparator).Append(commitCount.ToString(commitCountStringFormat));
                    metaSeparator = '.';
                }
                var sha = v.Sha;
                if (sha != null && shaLength > 0)
                {
                    builder.Append(metaSeparator).Append("sha.").Append(sha.Substring(0, Math.Min(shaLength, sha.Length)));
                }
            }
            return builder.ToString();
        }

        public override string ToString()
        {
            return ToInfoVersionString(shaLength: 7);
        }
    }
}
