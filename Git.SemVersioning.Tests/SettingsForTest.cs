using Git.SemVersioning.Scm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Git.SemVersioning
{
    class SettingsForTest : ISettings
    {
        public string MajorPattern { get; } = Resources.MajorPattern;

        public string MinorPattern { get; } = Resources.MinorPattern;

        public string PatchPattern { get; } = Resources.PatchPattern;

        public string ReleasePattern { get; } = Resources.ReleasePattern;

        public string DefaultPrerelease { get; } = Resources.DefaultPrerelease;
    }
}
