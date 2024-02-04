using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpubFixer
{
    public static class Utility
    {
        public static bool EndsWith(this string s, string[] pattern)
        {
            return pattern.Any(item => s.EndsWith(item));
        }
    }
}
