using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace EpubFixer
{
    internal static class CustomFixes
    {
        internal static void Fix(ref string content, ref int totalOffenses) => content = Fix(content, ref totalOffenses);

        internal static string Fix(string content, ref int totalOffenses)
        {
            //Code logic goes here.
            return content;
        }
    }
}
