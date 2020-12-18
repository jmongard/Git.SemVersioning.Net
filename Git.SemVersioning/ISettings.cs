using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Git.SemVersioning
{
    public interface ISettings
    {
        string DefaultPrerelease { get; }
        string MajorPattern { get; }
        string MinorPattern { get; }
        string PatchPattern { get; }
        string ReleasePattern { get; }
    }
}
