using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EpubFixer
{
    public static class Utility
    {
        public static bool EndsWith(this string s, string[] pattern)
        {
            return pattern.Any(item => s.EndsWith(item));
        }

        //The below may be better as a StringBuilder.
        public static string Replace(this string s, Match match, string replacement)
        {
            s = s.Remove(match.Index, match.Length);
            return s.Insert(match.Index, replacement);
        }
    }
}
