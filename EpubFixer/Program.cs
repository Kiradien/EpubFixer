﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;

namespace EpubFixer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var epubs = args.Where(item => item.EndsWith(new string[] { "epub", "zip" }));
            StringBuilder summarySB = new StringBuilder();
            if (epubs.Count() == 0)
            {
                if (args.Length > 0)
                {
                    Console.WriteLine("Incorrect file types detected.");
                }

                Console.WriteLine("Please provide a path to the epub to fix in order to run this app.");
                //Console.ReadKey();
            }
            else
            {
                int updatedFiles;
                foreach (string fileName in epubs)
                {
                    StartLoop:

                    try
                    {
                        ContentManager contentManager = new ContentManager(fileName);
                        updatedFiles = contentManager.Run();

                        Console.WriteLine($"Updated {updatedFiles} entries.");
                        Console.WriteLine("- - - - - - - - - - - - - - -\r\n");
                        summarySB.AppendLine($"Summary: [{fileName}] has a total of {updatedFiles} updates.");
                    }
                    catch (System.IO.IOException ex)
                    {
                        Console.WriteLine("- - - - - - - - - - - - - - -\r\n");
                        Console.WriteLine($"Unable to access the file. Please ensure the {fileName} isn't open in 7zip or otherwise locked.\r\n");
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("Retry? Y/n");
                        var result = Console.ReadKey().KeyChar.ToString().ToLower();
                        var retry = result == "y" || result == " ";
                        //Really lazy way to retry on IOException. Recursive Method is superior.
                        if (retry) goto StartLoop;
                    }
                }
            }
            Console.WriteLine(summarySB.ToString());

            Console.WriteLine();
            Console.WriteLine("Please press any key to close.");
            Console.ReadKey();
        }
    }
}