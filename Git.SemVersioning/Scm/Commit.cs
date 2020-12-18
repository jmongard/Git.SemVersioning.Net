using System;
using System.Collections.Generic;
using System.Linq;

namespace Git.SemVersioning.Scm
{
    internal class Commit : IRefInfo
    {
        public string Text { get; }

        public string Sha { get; }

        public IEnumerable<Commit> Parents { get; }

        public Commit(string text, string sha, IEnumerable<Commit> parents = null)
        {
            Text = text;
            Sha = sha;
            Parents = parents ?? Enumerable.Empty<Commit>();
        }
    }
}
