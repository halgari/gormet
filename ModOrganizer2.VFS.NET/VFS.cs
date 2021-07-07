using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Compression.BSA;
using Wabbajack.Common;

namespace ModOrganizer2.VFS.NET
{
    public class VFS
    {
        public VFS(AbsolutePath mo2Path, string profile, AbsolutePath[] ignoreFolders)
        {
            IgnoredFolders = ignoreFolders;
            ModOrganizer2Path = mo2Path;
            MO2Ini = ModOrganizer2Path.Combine("ModOrganizer.ini").LoadIniFile();

            ModFolder = mo2Path.Combine("mods");
            Mods = ModFolder.EnumerateDirectories(false)
                .Where(f => !ignoreFolders.Any(f.InFolder))
                .Select(m => new Mod(m.FileName.ToString(), this))
                .ToDictionary(m => m.Name);

            ProfileFolder = mo2Path.Combine("profiles", profile);
            ModlistDefinition = LoadModList(ProfileFolder.Combine("modlist.txt"));


            ModList = ModlistDefinition.Where(m => m.Status == ModStatus.Enabled)
                .Select(m => (m.Status, Mods[m.Name]))
                .ToArray();

            AppliedList = ModList.Select(m => m.Mod).SelectMany(m => m.Files).ToLookup(m => m.Path);

            PluginListDefinition = LoadPluginList(ProfileFolder.Combine("plugins.txt"));
            Plugins = PluginListDefinition.Where(p => p.Status == PluginStatus.Enabled)
                .Select(p => AppliedList[(RelativePath)p.Name].First())
                .ToArray();

            AppliedBSAs = Plugins.SelectMany(e =>
                {
                    var name = e.Path.FileNameWithoutExtension.ToString();
                    return new[]
                    {
                        (RelativePath) (name + ".bsa"),
                        (RelativePath) (name + " - Textures.bsa")
                    };
                }).Select(bsa => AppliedList[bsa])
                .Where(bsa => bsa.Any())
                .Select(bsa => bsa.First())
                .Select(bsa => (bsa, BSADispatch.OpenRead(bsa.AbsolutePath).GetAwaiter().GetResult()))
                .ToArray();

            AppliedBSAFiles = AppliedBSAs
                .SelectMany(bsa => bsa.BSA.Files
                    .Select(f => new BSAModFile(bsa.Mod, f))).ToArray();
            AllAppliedFiles = AppliedBSAFiles
                .Concat(ModList.Select(m => m.Mod).SelectMany(m => m.Files).Cast<IModFile>())
                .ToLookup(f => f.Path);
        }

        public AbsolutePath[] IgnoredFolders { get; }

        public ILookup<RelativePath,IModFile> AllAppliedFiles { get; }

        public BSAModFile[] AppliedBSAFiles { get; }

        public (ModFile Mod, IBSAReader BSA)[] AppliedBSAs { get; }

        public ModFile[] Plugins { get; }
        public (PluginStatus Status, string Name)[] PluginListDefinition { get; }

        private (PluginStatus Status, string Name)[] LoadPluginList(AbsolutePath plugins)
        {
            return plugins.ReadAllLines()
                .Select(l =>
                {
                    if (l.Length < 1) return (PluginStatus.Disabled, l);
                    var c = l[0];
                    return c switch
                    {
                        '*' => (PluginStatus.Enabled, l[1..]),
                        '#' => (PluginStatus.Comment, l),
                        _ => (PluginStatus.Disabled, l)
                    };
                }).ToArray();
        }

        public AbsolutePath ProfileFolder { get; set; }

        public ILookup<RelativePath,ModFile> AppliedList { get; }
        public (ModStatus Status, Mod Mod)[] ModList { get; }
        public AbsolutePath ModFolder { get; }

        private void Configure()
        {
            throw new System.NotImplementedException();
        }

        private (ModStatus, string)[] LoadModList(AbsolutePath modlist)
        {
            return modlist.ReadAllLines()
                .Select(l =>
                {
                    if (l.Length < 1) return (ModStatus.Comment, l);
                    var c = l[0];
                    return c switch
                    {
                        '*' => (ModStatus.Implicit, l[1..]),
                        '+' => (ModStatus.Enabled, l[1..]),
                        '-' when l.EndsWith("_separator") => (ModStatus.Separator,
                            l.Substring(1, l.Length - 1 - "_separator".Length)),
                        '-' => (ModStatus.Disabled, l[1..]),
                        _ => (ModStatus.Comment, l)
                    };
                }).ToArray();
        }

        public enum ModStatus
        {
            Enabled,
            Disabled,
            Separator,
            Implicit,
            Comment
        }

        public enum PluginStatus
        {
            Enabled,
            Disabled,
            Comment,
        }

        public Dictionary<string,Mod> Mods { get; }
        public dynamic MO2Ini { get; }
        public AbsolutePath ModOrganizer2Path { get; }
        public (ModStatus Status, string Name)[] ModlistDefinition { get; }
    }
}