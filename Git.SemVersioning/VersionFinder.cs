using Git.SemVersioning.Scm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Git.SemVersioning
{
    internal class VersionFinder
    {
        readonly HashSet<string> _visitedCommits = new HashSet<string>();
        readonly ILookup<string, Tag> tags;

        readonly ISettings settings;

        public VersionFinder(IEnumerable<Tag> tags, ISettings settings)
        {
            this.settings = settings;
            this.tags = tags.ToLookup(tag => tag.Sha);
        }

        public SemVersion GetVersion(Commit commit, bool isDirty)
        {
            var v = GetSemVersion(commit) ?? new SemVersion();
            v.CalculateNewVersion(isDirty, settings.DefaultPrerelease);
            return v;
        }

        private SemVersion GetSemVersion(Commit commit)
        {
            if (commit == null || _visitedCommits.Contains(commit.Sha))
            {
                return null;
            }
            _visitedCommits.Add(commit.Sha);

            try
            {
                var version = SemVersion.IsRelease(commit, settings) 
                    ? TryCreateSemVersion(commit)
                    : tags[commit.Sha].Select(TryCreateSemVersion).Where(NotNull).Max();

                if (version != null && !version.IsPreRelease)
                {
                    System.Diagnostics.Debug.WriteLine("Stopping search at version: {0}", version);
                    return version;
                }

                var parentVersion = commit.Parents.Select(GetSemVersion).Where(NotNull).Max() ?? new SemVersion();
                parentVersion.UpdateFromCommit(commit, settings, version);
                return parentVersion;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private SemVersion TryCreateSemVersion(IRefInfo r)
        {
            return SemVersion.TryParse(r, out var version) ? version : null;
        }

        private bool NotNull(object o)
        {
            return o != null;
        }
    }
}
