using System.Collections.Generic;
using System.Linq;
using Compression.BSA;
using Wabbajack.Common;

namespace Cooker
{
    public static class Definitions
    {
        public static List<BatchSettings> BatchSettings = new()
        {
            new()
            {
                Extension = new Extension(".dds"),
                Compress = true,
                FileFlags = FileFlags.Textures
            },
            new()
            {
                Extension = new Extension(".nif"),
                Compress = true,
                FileFlags = FileFlags.Meshes
            },
            new()
            {
                Extension = new Extension(".btr"),
                Compress = true,
                FileFlags = FileFlags.Meshes
            },
            new()
            {
                Extension = new Extension(".bto"),
                Compress = true,
                FileFlags = FileFlags.Meshes
            },
            new()
            {
                Extension = new Extension(".tri"),
                Compress = true,
                FileFlags = FileFlags.Meshes
            },
            new()
            {
                Extension = new Extension(".fuz"),
                Compress = false,
                FileFlags = FileFlags.Sounds
            },
            new()
            {
                Extension = new Extension(".wav"),
                Compress = false,
                FileFlags = FileFlags.Sounds
            },
            new()
            {
                Extension = new Extension(".pex"),
                Compress = false,
                FileFlags = FileFlags.Sounds
            },
            /*
            new BatchSettings
            {
                Extension = new Extension(".xwm"),
                Compress = false,
                FileFlags = FileFlags.Sounds
            },*/
            new()
            {
                Extension = new Extension(".lip"),
                Compress = false,
                FileFlags = FileFlags.Sounds
            },
            new()
            {
                Extension = new Extension(".hkx"),
                Compress = false,
                FileFlags = FileFlags.Meshes
            }
        };

        public static Dictionary<Extension, BatchSettings> BatchForExtension =
            BatchSettings.ToDictionary(b => b.Extension);

        public const long MaxBatchSize = 1_900_000_000;

    }

    public class BatchSettings
    {
        public Extension Extension { get; set; }
        public bool Compress { get; set; }
        public FileFlags FileFlags { get; set; }
    }
}