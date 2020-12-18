using System.Reflection;
using System.Runtime.CompilerServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyDescription(
@"A NuGet package for automatically versioning builds via the Git repository they are sitting in.

Usage: Tag your releases with ""v#.#.#"".

Additional Details: https://github.com/jmongard/Git.SemVersioning/blob/master/README.md")]

[assembly: AssemblyTitle("Automatic Versioning from Git")]
[assembly: AssemblyProduct("Git.SemVersioning")]
[assembly: AssemblyCompany("ExtendaRetail")]
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyCulture("")]
[assembly: InternalsVisibleTo("Git.SemVersioning.Tests")]
