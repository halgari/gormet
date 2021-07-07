using System.Collections.Generic;
using System.Linq;
using Compression.BSA;
using Wabbajack.Common;

namespace ModOrganizer2.VFS.NET
{
    public class Mod
    {
        public string Name { get; }
        private readonly VFS _vfs;
        public Mod(string name, VFS vfs)
        {
            _vfs = vfs;
            Name = name;
            Folder = vfs.ModFolder.Combine(name);
        }

        public AbsolutePath Folder { get; }

        public IEnumerable<ModFile> Files => _vfs.ModFolder.Combine(Name).EnumerateFiles().Select(f => new ModFile(f, Folder, this));
    }

    public interface IModFile
    {
        public RelativePath Path { get; }
        public long Size { get; }
    }

    public class ModFile : IModFile
    {
        public RelativePath Path { get; }
        public long Size => AbsolutePath.Size;
        public AbsolutePath AbsolutePath { get; }
        public Mod Mod { get; }
        public ModFile(AbsolutePath absolutePath, AbsolutePath folder, Mod mod)
        {
            Path = absolutePath.RelativeTo(folder);
            AbsolutePath = absolutePath;
            Mod = mod;
        }
    }

    public class BSAModFile : IModFile
    {
        public RelativePath Path => File.Path;
        public long Size => File.Size;
        public IFile File { get; set; }
        public ModFile BSAFile { get; }
        public BSAModFile(ModFile bsaFile, IFile file)
        {

            File = file;
            BSAFile = bsaFile;
        }
    }
}