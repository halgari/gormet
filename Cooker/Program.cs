using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Compression.BSA;
using ModOrganizer2.VFS.NET;
using Wabbajack.Common;

namespace Cooker
{
    class Program
    {
        private static int CookedMods = 0;
        static async Task Main(string[] args)
        {
            var mo2Folder = (AbsolutePath) args[0];
            var fromProfile = args[1];
            var toProfile = args[2];
            var modFolder = mo2Folder.Combine("mods");
            
            Console.WriteLine("Loading VFS");
            var vfs = new VFS(mo2Folder, fromProfile, new [] {mo2Folder.Combine("mods", "Cooked Files")});

            CreateFolders(mo2Folder, toProfile);
            await ThinBSAs(vfs, mo2Folder);
            var newEsps = await CookFiles(vfs, mo2Folder, true);
            await CopyOtherFiles(vfs, mo2Folder);
            await CreateNewProfile(newEsps, vfs, mo2Folder, fromProfile, toProfile);

        }

        private static async Task CreateNewProfile(string [] newEsps, VFS vfs, AbsolutePath mo2Folder, string fromProfile, string toProfile)
        {
            var fromFolder = mo2Folder.Combine("profiles", fromProfile);
            var toFolder = mo2Folder.Combine("profiles", toProfile);
            foreach (var file in fromFolder.EnumerateFiles())
            {
                var to = file.RelativeTo(fromFolder).RelativeTo(toFolder);
                to.Parent.CreateDirectory();
                Console.WriteLine($"Copying {to.RelativeTo(mo2Folder)}");
                await file.CopyToAsync(to);
            }

            await toFolder.Combine("modlist.txt").WriteAllLinesAsync(new[]
            {
                "+Cooked Files"
            });

            var plugins = 
                newEsps.Concat(vfs.Plugins.Select(p => p.Path.FileName.ToString()))
                    .Select(p => "*" + p)
                .ToArray();
            await toFolder.Combine("plugins.txt").WriteAllLinesAsync(plugins);
        }

        private static async Task CopyOtherFiles(VFS vfs, AbsolutePath to)
        {
            var bsaExtension = new Extension(".bsa");
            var files = vfs.AllAppliedFiles.Select(f => f.First()).OfType<ModFile>()
                .Where(f => !Definitions.BatchForExtension.ContainsKey(f.Path.Extension))
                .Where(f => f.AbsolutePath.Extension != bsaExtension || f.AbsolutePath.FileName.StartsWith("Skyrim - Textures"));
            foreach (var file in files)
            {
                var outFile = to.Combine("mods", "Cooked Files", file.Path.ToString());
                outFile.Parent.CreateDirectory();
                Console.WriteLine($"Copying {file.Path}");
                await file.AbsolutePath.CopyToAsync(outFile);
            }
        }
        private static async Task<string[]> CookFiles(VFS vfs, AbsolutePath to, bool trial = false)
        {
            var files = vfs.AllAppliedFiles.Select(f => f.First()).OfType<ModFile>()
                .Where(f => Definitions.BatchForExtension.ContainsKey(f.Path.Extension))
                .GroupIntoSizes(f => f.Size, Definitions.MaxBatchSize)
                .Select((itms, idx) => (itms, idx))
                .ToArray();

            List<string> newEsps = new();
            Console.WriteLine($"Found {files.Length} archives to build");
            foreach (var (group, idx) in files)
            {
                var name = $"_Cooked {idx:0000}.bsa";
                var outFile = to.Combine("mods", "Cooked Files", name);
                if (!trial)
                {
                    Console.WriteLine($"Building {name}");
                    var flags = group.Select(f => Definitions.BatchForExtension[f.Path.Extension]).Distinct()
                        .Aggregate((uint) 0, (a, b) => a | (uint) b.FileFlags);
                    await using var bsa = await BSABuilder.Create(new BSAStateObject()
                    {
                        ArchiveFlags = ((int) (ArchiveFlags.HasFileNames | ArchiveFlags.HasFolderNames
                            /* | ArchiveFlags.HasFileNameBlobs*/)) | 0x10 | 0x80,
                        FileFlags = flags,
                        Magic = Encoding.ASCII.GetString(new byte[] {0x42, 0x53, 0x41, 0x00}),
                        Version = 0x67
                    }, Definitions.MaxBatchSize);
                    bsa.HeaderType = VersionType.SSE;

                    foreach (var (file, fidx) in group.Select((itm, fidx) => (itm, fidx)))
                    {
                        var state = new BSAFileStateObject
                        {
                            Path = (RelativePath)((string)file.Path).ToLowerInvariant().Replace('/', '\\'),
                            Index = fidx,
                            FlipCompression = false
                        };
                        await bsa.AddFile(state, await file.AbsolutePath.OpenRead());
                    }

                    Console.WriteLine($"Writing {name}");

                    await bsa.Build(outFile);
                    Console.WriteLine($"Done building {name}");
                }

                Console.WriteLine($"Writing Stub");
                outFile = outFile.ReplaceExtension(new Extension(".esp"));
                newEsps.Add(outFile.FileName.ToString());
                await "Stub.esp".RelativeTo(AbsolutePath.EntryPoint).CopyToAsync(outFile);
            }

            return newEsps.ToArray();
        }

        private static async Task ThinBSAs(VFS vfs, AbsolutePath to)
        {
            var indexedBSAFiles = vfs.AllAppliedFiles.Select(f => f.First()).OfType<BSAModFile>()
                .ToLookup(f => f.BSAFile.Path);

            foreach (var bsa in vfs.AppliedBSAs)
            {
                var outFile = to.Combine("mods", "Cooked Files", bsa.Mod.Path.ToString());

                var surviving = indexedBSAFiles[bsa.Mod.Path];
                if (bsa.BSA.Files.Count() == surviving.Count())
                {
                    Console.WriteLine($"Not thinning {bsa.Mod.Path} no files are overriden");
                    await CopyIfNewer(bsa.Mod.AbsolutePath, outFile);
                }
                else
                {
                    Console.WriteLine($"Thinning {bsa.Mod.Path}");
                    throw new NotImplementedException();
                }

            }
        }

        private static void CreateFolders(AbsolutePath to, string profile)
        {
            Console.WriteLine("Creating output folders");
            to.CreateDirectory();
            to.Combine("mods", "Cooked Files").CreateDirectory();
            to.Combine("profiles", profile).CreateDirectory();
            
        }

        private static async Task CopyIfNewer(AbsolutePath from, AbsolutePath to)
        {
            if (to.Exists && from.Exists && to.Size == from.Size && to.LastModified >= from.LastModified)
                return;
            await from.CopyToAsync(to);
        }
    }
}