
namespace Git.SemVersioning.Scm
{
    internal class Tag : IRefInfo
    {
        public string Text { get; }
        public string Sha { get; }

        public Tag(string text, string sha)
        {
            Text = text;
            Sha = sha;
        }
    }
}
