using System;
using System.Collections.Generic;
using System.Linq;

namespace Git.SemVersioning.Scm
{
    internal class GitProvider
    {
        private ISettings Settings { get; }

        public GitProvider(ISettings settings)
        {
            Settings = settings;
        }

        internal SemVersion GetSemVersion(string startingPath)
        {
            var repositoryPath = LibGit2Sharp.Repository.Discover(startingPath);
            if (repositoryPath == null)
            {
                return null;
            }
            using (var repository = new LibGit2Sharp.Repository(repositoryPath))
            {
                return GetSemVersion(repository);
            }
        }

        internal SemVersion GetSemVersion(LibGit2Sharp.IRepository repository)
        {
            var commits = GetHeadCommitsFromRepository(repository);
            var tags = GetTagsFromRepository(repository);
            var dirty = repository.RetrieveStatus(new LibGit2Sharp.StatusOptions()).IsDirty;
            var semVersion = new VersionFinder(tags, Settings).GetVersion(commits.FirstOrDefault(), dirty);
            return semVersion;
        }

        internal IEnumerable<Commit> GetHeadCommitsFromRepository(LibGit2Sharp.IRepository repository)
        {
            return repository.Head?.Commits?.Select(CreateCommit) ?? Enumerable.Empty<Commit>();
        }

        private Commit CreateCommit(LibGit2Sharp.Commit commit)
        {
            return new Commit(commit.Message, commit.Sha, From(commit).SelectMany(c => c.Parents).Select(CreateCommit));
        }

        internal IEnumerable<Tag> GetTagsFromRepository(LibGit2Sharp.IRepository repository)
        {
            return repository.Tags?.Select(CreateTag) ?? Enumerable.Empty<Tag>();
        }

        private Tag CreateTag(LibGit2Sharp.Tag tag)
        {
            return new Tag(tag.FriendlyName, tag.Target.Sha);
        }

        private IEnumerable<T> From<T>(params T[] items)
        {
            return items;
        }
    }
}
