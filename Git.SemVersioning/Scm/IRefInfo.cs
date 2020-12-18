using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Git.SemVersioning.Scm
{
    interface IRefInfo
    {
        string Text { get; }

        string Sha { get; }
    }
}
