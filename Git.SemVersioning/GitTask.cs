using Git.SemVersioning.Scm;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;

namespace Git.SemVersioning
{
    public class GitTask : Task, ISettings
    {
        [Required]
        public string OutputFilePath { get; set; }
        public bool IncludeAssemblyVersion { get; set; }
        public bool IncludeAssemblyFileVersion { get; set; }
        public bool IncludeAssemblyInformationalVersion { get; set; }

        public string MajorPattern { get; set; } = Resources.MajorPattern;

        public string MinorPattern { get; set; } = Resources.MinorPattern;

        public string PatchPattern { get; set; } = Resources.PatchPattern;

        public string ReleasePattern { get; set; } = Resources.ReleasePattern;

        public string DefaultPrerelease { get; set; } = Resources.DefaultPrerelease;

        [Output]
        public string SemVer2 { get; set; }

        [Output]
        public string SemVer1 { get; set; }

        public override bool Execute()
        {
            var repositorySearchPathStart = Path.GetDirectoryName(OutputFilePath);
            var version = new GitProvider(this).GetSemVersion(repositorySearchPathStart);
            if (version != null)
            {
                SemVer2 = version.ToInfoVersionString();
                SemVer1 = version.ToInfoVersionString(v2: false);

                Log.LogMessage(MessageImportance.Normal, "Setting version to: {0}", version);
                var fileContents = VersionFileGenerator.GenerateFileContents(
                            IncludeAssemblyVersion ? version.ToVersionString() : null,
                            IncludeAssemblyFileVersion ? version.ToFileVersionString() : null,
                            IncludeAssemblyInformationalVersion ? SemVer2 : null);
                File.WriteAllText(OutputFilePath, fileContents);
            }
            return !Log.HasLoggedErrors;
        }
    }
}
