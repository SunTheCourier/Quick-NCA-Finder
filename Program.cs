using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibHac;
using System.Linq;
using LibHac.IO;

namespace Quick_NCA_Finder
{
    class Program
    {
        static readonly ProgressBar progress = new ProgressBar();
        static SwitchFs fs;
        static readonly DirectoryInfo ApplicationsFolder = new DirectoryInfo("./Apps");

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Quick-NCA-Finder.exe {Folder to search, make this the root of the NAND partition or SD.} {TID or name of application to search for, use * for all titles, or leave blank to list all titles.} {add `NSP` to pack into NSP or keep blank for no NSP");
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(args[0]);
            FileInfo prodkeys = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch/prod.keys"));
            FileInfo titlekeys = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch/title.keys"));
            FileInfo consolekeys = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch/console.keys"));
            int i = 0;

            if (!prodkeys.Exists || !titlekeys.Exists || !consolekeys.Exists)
            {
                Console.WriteLine("Your prod.keys, title.keys or console.keys do not exist at ~/.switch/ derive them with HACGUI or place them there.");
                return;
            }

            Keyset keys = new Keyset();
            keys = ExternalKeys.ReadKeyFile(prodkeys.FullName, titlekeys.FullName, consolekeys.FullName);
            fs = new SwitchFs(keys, new FileSystem(dir.FullName));

            if (args.Length == 1)
            {
                foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
                {
                    ulong titleId = kv.Key;
                    Title title = kv.Value;
                    Console.WriteLine($"{titleId:X8} {title.Name}");
                }
                Console.WriteLine("Done!");
                return;
            }

            ApplicationsFolder.Create();

            if (args[1] == "*" && args.Length == 3)
            {
                if (args[2].ToLower() == "nsp")
                {
                    Pfs0Builder NSP = new Pfs0Builder();
                    foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
                    {
                        ulong titleId = kv.Key;
                        Title title = kv.Value;

                        string titleRoot = $"{titleId:X8} [{title.Name}] [v{title.Version.Revision}].nsp";
                        string safeNSPName = new string(titleRoot.Where(c => !Path.GetInvalidPathChars().Contains(c)).ToArray()); //remove unsafe chars
                        safeNSPName = safeNSPName.Replace(":", ""); //manually remove `:` cuz mircosoft doesnt have it in their list
                        FileInfo SafeNSP = new FileInfo(Path.Combine(ApplicationsFolder.FullName, safeNSPName));

                        foreach (Nca nca in title.Ncas)
                        {
                            NSP.AddFile(nca.NcaId, nca.GetStorage().AsStream());
                            Console.WriteLine($"Adding {nca.Header.TitleId:X8} {title.Name}: {nca.Header.ContentType} to PFS0...");
                        }
                        Console.WriteLine("Saving PFS0");
                        using (FileStream dest = SafeNSP.Create())
                        {
                            NSP.Build(dest, progress);
                        }
                    }
                }
                Console.WriteLine("Done!");
                return;
            }

            if (args[1] == "*" && args.Length != 3)
            {
                Console.WriteLine("Saving all NCAs...");
                foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
                {
                    ulong titleId = kv.Key;
                    Title title = kv.Value;
                    string titleRoot = $"{titleId:X8} {title.Name}";
                    string safeDirectoryName = new string(titleRoot.Where(c => !Path.GetInvalidPathChars().Contains(c)).ToArray()); //remove unsafe chars
                    safeDirectoryName = safeDirectoryName.Replace(":", ""); //manually remove `:` cuz mircosoft doesnt have it in their list
                    DirectoryInfo safeDirectory = new DirectoryInfo(Path.Combine(ApplicationsFolder.FullName, safeDirectoryName));
                    safeDirectory.Create();
                    foreach (Nca nca in title.Ncas)
                    {
                        FileInfo ncainfo = safeDirectory.GetFile($"{nca.Header.ContentType}00.nca");
                        if (ncainfo.Exists)
                        {
                            i++;
                            ncainfo = safeDirectory.GetFile($"{nca.Header.ContentType}0{i}.nca");
                        }
                        else i = 0;
                        using (FileStream dest = ncainfo.Create())
                        using (IStorage storage = nca.GetStorage())
                        {
                            storage.AsStream().CopyStream(dest, storage.Length, progress);
                        }
                    }
                }
                Console.WriteLine("Done!");
                return;
            }

            ulong TID;
            try
            {
                TID = ulong.Parse(args[1], NumberStyles.HexNumber);
            }
            catch
            {
                //Better way to do this?
                if (args.Length == 3)
                {
                    if (args[2] == "nsp")
                    {
                        GetNSP(true, TitleName: args[1]);
                    }
                }
                else GetNCAs(true, TitleName: args[1]);
                return;
            }
            if (args.Length == 3)
            {
                if (args[2].ToLower() == "nsp")
                {
                    GetNSP(false, TID: TID);
                }
            }
            else GetNCAs(false, TID: TID);
        }

        private static void GetNCAs(bool SearchByName, ulong TID = 0, string TitleName = null)
        {
            int i = 0;
            if (SearchByName)
            {

                foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
                {
                    ulong titleId = kv.Key;
                    Title title = kv.Value;

                    if (title.Name.ToLower() == TitleName.ToLower() || title.Name.ToLower().Contains(TitleName.ToLower()))
                    {
                        Console.WriteLine("Found!");
                        string titleRoot = $"{titleId:X8} {title.Name}";
                        string safeDirectoryName = new string(titleRoot.Where(c => !Path.GetInvalidPathChars().Contains(c)).ToArray()); //remove unsafe chars
                        safeDirectoryName = safeDirectoryName.Replace(":", ""); //manually remove `:` cuz mircosoft doesnt have it in their list
                        DirectoryInfo safeDirectory = new DirectoryInfo(Path.Combine(ApplicationsFolder.FullName, safeDirectoryName));
                        safeDirectory.Create();
                        Console.WriteLine($"Saving {title.Name} v{title.Version.Revision} to working directory...");

                        foreach (Nca nca in title.Ncas)
                        {
                            FileInfo ncainfo = safeDirectory.GetFile($"{nca.Header.ContentType}00.nca");
                            if (ncainfo.Exists)
                            {
                                i++;
                                ncainfo = safeDirectory.GetFile($"{nca.Header.ContentType}0{i}.nca");
                            }
                            else i = 0;

                            using (FileStream dest = ncainfo.Create())
                            using (IStorage storage = nca.GetStorage())
                            {
                                storage.AsStream().CopyStream(dest, storage.Length, progress);
                            }
                        }
                        //weird progress bar bug(?)
                        Console.WriteLine("\nDone!");
                        return;
                    }
                    else Console.WriteLine($"{titleId:X8}");
                }
                Console.WriteLine("TID not found!");
            }
            else
            {
                foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
                {
                    ulong titleId = kv.Key;
                    Title title = kv.Value;

                    if (titleId == TID)
                    {
                        Console.WriteLine("Found!");
                        string titleRoot = $"{titleId:X8} {title.Name}";
                        string safeDirectoryName = new string(titleRoot.Where(c => !Path.GetInvalidPathChars().Contains(c)).ToArray()); //remove unsafe chars
                        safeDirectoryName = safeDirectoryName.Replace(":", ""); //manually remove `:` cuz mircosoft doesnt have it in their list
                        DirectoryInfo safeDirectory = new DirectoryInfo(Path.Combine(ApplicationsFolder.FullName, safeDirectoryName));
                        safeDirectory.Create();
                        Console.WriteLine($"Saving {title.Name} v{title.Version.Revision} to working directory...");

                        foreach (Nca nca in title.Ncas)
                        {
                            FileInfo ncainfo = safeDirectory.GetFile($"{nca.Header.ContentType}00.nca");
                            if (ncainfo.Exists)
                            {
                                i++;
                                ncainfo = safeDirectory.GetFile($"{nca.Header.ContentType}0{i}.nca");
                            }
                            else i = 0;

                            using (FileStream dest = ncainfo.Create())
                            using (IStorage storage = nca.GetStorage())
                            {
                                storage.AsStream().CopyStream(dest, storage.Length, progress);
                            }
                        }
                        Console.WriteLine("Done!");
                        return;
                    }
                    else Console.WriteLine($"{titleId:X8}");
                }
                Console.WriteLine("TID not found!");
            }
        }
        private static void GetNSP(bool SearchByName, ulong TID = 0, string TitleName = null)
        {
            Pfs0Builder NSP = new Pfs0Builder();
            if (SearchByName)
            {
                foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
                {
                    ulong titleId = kv.Key;
                    Title title = kv.Value;

                    if (title.Name.ToLower() == TitleName.ToLower() || title.Name.ToLower().Contains(TitleName.ToLower()))
                    {
                        string titleRoot = $"{titleId:X8} [{title.Name}] [v{title.Version.Revision}].nsp";
                        string safeNSPName = new string(titleRoot.Where(c => !Path.GetInvalidPathChars().Contains(c)).ToArray()); //remove unsafe chars
                        safeNSPName = safeNSPName.Replace(":", ""); //manually remove `:` cuz mircosoft doesnt have it in their list
                        FileInfo SafeNSP = new FileInfo(Path.Combine(ApplicationsFolder.FullName, safeNSPName));

                        foreach (Nca nca in title.Ncas)
                        {
                            NSP.AddFile(nca.NcaId, nca.GetStorage().AsStream());
                            Console.WriteLine($"Saving {nca.Header.TitleId:X8} {title.Name}: {nca.Header.ContentType} to PFS0...");
                        }
                        if (SafeNSP.Exists) SafeNSP.Delete();

                        Console.WriteLine("Saving PFS0");
                        using (FileStream dest = SafeNSP.Create())
                        {
                            NSP.Build(dest, progress);
                        }
                        Console.WriteLine("Done!");
                        return;
                    }
                    else Console.WriteLine($"{titleId:X8}");
                }
                Console.WriteLine("TID not found!");
            }
            else
            {
                foreach (KeyValuePair<ulong, Title> kv in fs.Titles)
                {
                    ulong titleId = kv.Key;
                    Title title = kv.Value;

                    if (titleId == TID)
                    {
                        string titleRoot = $"{titleId:X8} [{title.Name}] [v{title.Version.Revision}].nsp";
                        string safeNSPName = new string(titleRoot.Where(c => !Path.GetInvalidPathChars().Contains(c)).ToArray()); //remove unsafe chars
                        safeNSPName = safeNSPName.Replace(":", ""); //manually remove `:` cuz mircosoft doesnt have it in their list
                        FileInfo SafeNSP = new FileInfo(Path.Combine(ApplicationsFolder.FullName, safeNSPName));


                        foreach (Nca nca in title.Ncas)
                        {
                            NSP.AddFile(nca.NcaId, nca.GetStorage().AsStream());
                            Console.WriteLine($"Adding {nca.Header.TitleId:X8} {title.Name}: {nca.Header.ContentType} to PFS0...");
                        }
                        if (SafeNSP.Exists) SafeNSP.Delete();

                        Console.WriteLine("Saving PFS0");
                        using (FileStream dest = SafeNSP.Create())
                        {
                            NSP.Build(dest, progress);
                        }
                        Console.WriteLine("Done!");
                        return;
                    }
                    else Console.WriteLine($"{titleId:X8}");
                }
                Console.WriteLine("TID not found!");
            }
        }
    }
}
