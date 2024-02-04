using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EpubFixer
{
    public class ContentManager
    {
        public const string htmlTagDetector = "&lt;([\\w\\-_\\+]{0,10})&gt;(.*?)&lt;(/\\1)&gt;";
        public const string replaceLN = "Starkiller";
        public const string replaceFN = "Zeke";
        public const string replaceNN = "Zee"; //Nickname
        public const string replaceHC = "brunette"; //Hair
        public const string replaceEC = "emerald"; //Eyes

        public int totalOffenses = 0;

        private ZipArchive? zipArchive;
        private string zipArchiveFileName;

        public ContentManager() { }

        public ContentManager(string fileName) 
        { 
            this.zipArchiveFileName = fileName;
            //zipArchive = ZipFile.Open(fileName, ZipArchiveMode.Update);
        }

        public ZipArchive Open()
        {
            return ZipFile.Open(zipArchiveFileName, ZipArchiveMode.Update);
        }

        /// <summary>
        /// Retrieve the contents of an entry in the zip.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public string ParseZipArchiveEntry(ZipArchiveEntry entry)
        {
            StringBuilder document;
            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                document = new StringBuilder(reader.ReadToEnd());
            }

            return document.ToString();
        }

        public string FixContent(ZipArchiveEntry entry, string regexSearch, string regexReplace, ref int totalOffenses)
        {
            //Consider making totalOffenses a global variable.
            totalOffenses = 0;
            return FixContent(ParseZipArchiveEntry(entry), regexSearch, regexReplace, ref totalOffenses);
        }

        public string FixContent(string content, string regexSearch, string regexReplace, ref int totalOffenses)
        {
            int offenseCount = Regex.Count(content, regexSearch, RegexOptions.IgnoreCase);

            if (offenseCount > 0)
            {
                System.Text.RegularExpressions.Match match;
                do
                {
                    match = Regex.Match(content, regexSearch, RegexOptions.IgnoreCase);

                    if (match.Success && Regex.Match(match.Value, regexReplace, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace).Success)
                        break;
                    //Basic breaking condition; may need more developed regexReplace in the future with replaceVals set.
                    totalOffenses += offenseCount;
                    content = Regex.Replace(content, regexSearch, regexReplace, RegexOptions.IgnoreCase);
                    offenseCount = Regex.Count(content, regexSearch, RegexOptions.IgnoreCase);
                } while (offenseCount > 0);
            }
            return content;
        }
        public void FixContent(ref string content, string regexSearch, string regexReplace, ref int totalOffenses)
        {
            content = FixContent(content, regexSearch, regexReplace, ref totalOffenses);
        }

        public void FixLinks(ref string content, string regexSearch, string linkText)
        {
            //This method is currently fundamentally flawed; need to introduce detection searching for existing links.
            content = Regex.Replace(content, regexSearch, $"<a href='$1'>{linkText}</a>", RegexOptions.IgnoreCase);
        }

        public void RemoveDuplicates(ref string content, string regexSearch, ref int totalOffenses)
        {
            var matches = Regex.Matches(content, regexSearch, RegexOptions.IgnoreCase);
            if (matches.Count > 1) {
                totalOffenses++;
                content = new Regex(regexSearch).Replace(content, string.Empty, 100, matches[1].Index - 1);
            }
        }

        public bool UpdateFile(ZipArchive archive, string entryName)
        {
            ZipArchiveEntry? entry = archive.GetEntry(entryName);

            if (entry == null)
            {
                Console.WriteLine($"Could not find archive entry: [{entryName}]");
            }
            else
            {
                int totalOffenses = 0;
                var content = FixContent(entry, htmlTagDetector, "<$1>$2</$1>", ref totalOffenses);
                this.StripWatermarks(ref content, ref totalOffenses);
                content = FixContent(content, "<hr\\s*/>|<hr [a-z =\"\\d]{0,30}/>", "<p> - - - - - - - - - - </p>", ref totalOffenses);
                content = FixContent(content, "∼", "~", ref totalOffenses);
                content = FixContent(content, "(\\w)`(\\w)", "$1'$2", ref totalOffenses);
                //content = FixCringe(content, ref totalOffenses);

                RemoveDuplicates(ref content, "((?:https?://)?(?:www.)?PATREON\\.com/[\\w_\\-/]+)", ref totalOffenses);
                RemoveDuplicates(ref content, "((?:https?://)?(?:www.)?discord\\.gg/[\\w_\\-\\d]+)", ref totalOffenses);
                //FixLinks(ref content, "((?:https?://)?(?:www.)?PATREON\\.com/[\\w_\\-/]+)", "Patreon");

                if (false && !string.IsNullOrWhiteSpace(replaceFN) && !string.IsNullOrWhiteSpace(replaceLN))
                {
                    content = FixContent(content, "[\\(\\[\\{]Y/N[\\)\\]\\}]", replaceFN, ref totalOffenses);
                    content = FixContent(content, "[\\(\\[\\{]L/N[\\)\\]\\}]", replaceLN, ref totalOffenses);
                    content = FixContent(content, "[\\(\\[\\{]N/N[\\)\\]\\}]", replaceNN, ref totalOffenses);
                    content = FixContent(content, "[\\(\\[\\{]E/C[\\)\\]\\}]", replaceEC, ref totalOffenses);
                    content = FixContent(content, "[\\(\\[\\{]H/C[\\)\\]\\}]", replaceHC, ref totalOffenses);
                }

                if (totalOffenses > 0)
                {
                    Console.WriteLine($"{entry.Name} contained {totalOffenses} issues to fix. They have been resolved.");
                    var filename = entry.FullName;
                    entry.Delete();
                    entry = archive.CreateEntry(filename);

                    using (var stream = entry.Open())
                    {
                        stream.SetLength(0);
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.Write(content);
                        }
                    }
                }
                return totalOffenses > 0;
            }
            return false;
        }

        public void StripWatermarks(ref string content, ref int totalOffenses)
        {
            {
                string patreonFormat = string.Format("\\(?{0}P{0}[A@]{0}T{0}R{0}[E3]{0}[O0]{0}N{0}\\)?", "[().\\s\\[\\]\\-]{0,5}");
                FixContent(ref content, patreonFormat, " PATREON ", ref totalOffenses);
                //PATREON FixContent could be improved through a "confirmation" regex of sorts. Pain in the butt with minimal benefit under current schema.
            }
        }

        public string FixCringe(string content, ref int totalOffenses)
        {
            FixContent(ref content, "Emperor Organization", "The Round Table", ref totalOffenses);
            return content;
        }
    }
}
