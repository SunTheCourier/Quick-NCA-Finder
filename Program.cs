﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibHac;

namespace Quick_NCA_Finder
{
    class Program
    {
        public static ulong TID;
        public static FileInfo controlNCA;
        public static FileInfo metaNCA;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: Quick-NCA-Finder.exe {folder to search, make this is the root of the NAND partition} {TID to search for}");
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(args[0]);
            string ID = args[1];
            try
            { 
                TID = ulong.Parse(ID, NumberStyles.HexNumber);
            }
            catch
            {
                Console.WriteLine("The inputted TID is not valid");
            }

            DirectoryInfo userpath = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            FileInfo prodkeys = new FileInfo(Path.Combine(userpath.FullName, ".switch/prod.keys"));
            FileInfo titlekeys = new FileInfo(Path.Combine(userpath.FullName, ".switch/title.keys"));

            if (!prodkeys.Exists && !titlekeys.Exists)
            {
                Console.WriteLine("Your prod.keys or title.keys do not exist at ~/.switch/ derive them in HACGUI or place them there.");
                return;
            }

            Keyset keys = new Keyset();
            keys = ExternalKeys.ReadKeyFile(prodkeys.FullName, titlekeys.FullName);
            FileSystem filesystem = new FileSystem(dir.FullName);
            SwitchFs fs = new SwitchFs(keys, filesystem);

           foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
           {
                ulong titleId = kv.Key;
                Title title = kv.Value;
                FileInfo ncainfo;

                if (kv.Key == TID)
                {
                    Console.WriteLine("Found!");
                    Console.WriteLine($"-->{titleId:X8}<--");
                    Console.WriteLine("Saving NCA to working directory...");
                    DirectoryInfo titleRoot = new DirectoryInfo($"./{titleId:X8}");

                    foreach (Nca nca in title.Ncas)
                    {
                        ncainfo = titleRoot.GetFile($"{nca.Header.ContentType.ToString()}.nca");
                        using (Stream source = nca.GetStream())
                        using (FileStream dest = ncainfo.Create())
                        {
                            source.CopyTo(dest);
                        }
                    }

                    Console.WriteLine("Done!");
                    return;
                }
                else
                {
                    Console.WriteLine($"{titleId:X8}");
                }
           }
            Console.WriteLine("TID not found!");
        }

    }
}
