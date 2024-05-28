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
        public const string htmlTagDetector = "&lt;([\\w\\-_\\+]{0,12})(\\s+(?:style|class|data-annotation-id)=(['\"`]).*?\\3)?\\s*&gt;(.*?)&lt;(/\\1)&gt;";

        public int totalOffenses = 0;

        private ZipArchive? _zipArchive;
        private string _zipArchiveFileName;

        public ContentManager(string fileName) 
        { 
            this._zipArchiveFileName = fileName;
        }

        public ZipArchive Open()
        {
            return ZipFile.Open(_zipArchiveFileName, ZipArchiveMode.Update);
        }

        /// <summary>
        /// Core runtime logic for opening, navigating and cleaning up an epub.
        /// </summary>
        /// <returns>Filecount of affected files.</returns>
        public int Run()
        {
            int updatedFiles = 0;
            using (var archive = this.Open())
            {
                Console.WriteLine("Zipfile Opened");

                var items = archive.Entries.Where(item => item.FullName.StartsWith("OEBPS/Text/", true, null) && item.FullName.Contains("htm")).ToList();

                foreach (ZipArchiveEntry entry in items)
                {
                    Console.WriteLine($"Parsing: {entry.Name}");
                    updatedFiles += this.UpdateFile(archive, entry.FullName) ? 1 : 0;
                }
            }
            return updatedFiles;
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
                var content = GenericFixes.FixContent(entry, htmlTagDetector, "<$1$2>$4</$1>", ref totalOffenses);
                GenericFixes.FixContent(ref content, "&lt;br\\s*/\\s*&gt;", " <br />", ref totalOffenses);
                this.StripWatermarks(ref content, ref totalOffenses);
                //TODO:: Consider DOM Parser replacement for some of these.
                GenericFixes.FixContent(ref content, "<hr\\s*/?>|<hr [a-z =\"\\d]{0,30}/?>", "<p> - - - - - - - - - - </p>", ref totalOffenses);
                GenericFixes.FixContent(ref content, "∼", "~", ref totalOffenses);
                GenericFixes.FixContent(ref content, "(\\w)`(\\w)", "$1'$2", ref totalOffenses);
                GenericFixes.FixContent(ref content, "🎶", "♪", ref totalOffenses);
                CustomFixes.Fix(ref content, ref totalOffenses);

                GenericFixes.RemoveDuplicates(ref content, "((?:https?://)?(?:www.)?PATREON\\.com/[\\w_\\-/]+)", ref totalOffenses);
                GenericFixes.RemoveDuplicates(ref content, "((?:https?://)?(?:www.)?discord\\.gg/[\\w_\\-\\d]+)", ref totalOffenses);
                //FixLinks(ref content, "((?:https?://)?(?:www.)?PATREON\\.com/[\\w_\\-/]+)", "Patreon");


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
                string patreonFormat = string.Format("\\(?{0}P{0}[A@]{0}T{0}R{0}[E3]{0}[O0]{0}N{0}(?:c{0}o{0}m)?\\)?", "[().\\s\\[\\]\\-\\*]{0,5}");
                GenericFixes.FixContent(ref content, patreonFormat, " PATREON ", ref totalOffenses);
                //PATREON FixContent could be improved through a "confirmation" regex of sorts. Pain in the butt with minimal benefit under current schema.
            }
        }
    }
}
