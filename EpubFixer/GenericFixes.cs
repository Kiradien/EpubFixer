using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EpubFixer
{
    internal static class GenericFixes
    {
        /// <summary>
        /// Retrieve the contents of an entry in the zip.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private static string ParseZipArchiveEntry(ZipArchiveEntry entry)
        {
            StringBuilder document;
            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                document = new StringBuilder(reader.ReadToEnd());
            }

            return document.ToString();
        }
        internal static string FixContent(ZipArchiveEntry entry, string regexSearch, string regexReplace, ref int totalOffenses)
        {
            //Consider making totalOffenses a global variable.
            totalOffenses = 0;
            return FixContent(ParseZipArchiveEntry(entry), regexSearch, regexReplace, ref totalOffenses);
        }

        internal static string FixContent(string content, string regexSearch, string regexReplace, ref int totalOffenses)
        {
            int offenseCount = Regex.Count(content, regexSearch, RegexOptions.IgnoreCase);

            if (offenseCount > 0)
            {
                Match match = Regex.Match(content, regexSearch, RegexOptions.IgnoreCase);
                while (match.Success)
                {
                    if (match.Success && !Regex.Match(match.Value, regexReplace, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace).Success)
                    {
                        content = content.Replace(match, regexReplace);
                        totalOffenses++;

                        if (offenseCount-- > 0) match = Regex.Match(content, regexSearch, RegexOptions.IgnoreCase); //re-cycle matches for updated indexing.
                    }
                    match = match.NextMatch();
                }
            }
            return content;
        }
        internal static void FixContent(ref string content, string regexSearch, string regexReplace, ref int totalOffenses)
        {
            content = FixContent(content, regexSearch, regexReplace, ref totalOffenses);
        }

        internal static void FixLinks(ref string content, string regexSearch, string linkText)
        {
            //This method is currently fundamentally flawed; need to introduce detection searching for existing links.
            content = Regex.Replace(content, regexSearch, $"<a href='$1'>{linkText}</a>", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Remove duplicate text matching regex parameter from content
        /// </summary>
        /// <param name="content">Main search criteria and output</param>
        /// <param name="regexSearch"></param>
        /// <param name="totalOffenses"></param>
        internal static void RemoveDuplicates(ref string content, string regexSearch, ref int totalOffenses)
        {
            var matches = Regex.Matches(content, regexSearch, RegexOptions.IgnoreCase);
            if (matches.Count > 1)
            {
                totalOffenses++;
                content = new Regex(regexSearch).Replace(content, string.Empty, 100, matches[1].Index - 1);
            }
        }
    }
}
