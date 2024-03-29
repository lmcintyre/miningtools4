using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Lumina;
using Lumina.Data;
using Lumina.Data.Structs;
using Lumina.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using ZiPatchLib.Chunk.SqpkCommand;

// pmgr on GitHub
namespace miningtools4;

public class IndexHashLengthCalculator
{
    private readonly string _gamePath;
    private readonly GeneratorConfig _config;

    public IndexHashLengthCalculator(GameData lumina, GeneratorConfig config)
    {
        _gamePath = lumina.DataPath.ToString();
        _config = config;
    }

    public void Output()
    {
        var thisFileContents = new List<string>();
        Directory.CreateDirectory(_config.OutputFilename);
        foreach (var file in Directory.EnumerateFiles(_gamePath, "*.index", SearchOption.AllDirectories))
        {
            var index = new FileInfo(file);
            var index2 = new FileInfo($@"{index.FullName}2");

            Console.WriteLine($"Processing {index.Name.Replace(".win32.index", "")}");
            var hasher = new IndexHasher(index, index2);
            hasher.MakeOffsetDict();
            hasher.GuessTableLengths();

            foreach (var groups in hasher.HashTable.GroupBy(kv => kv.Value.FileNameLen))
            {
                if (groups.Key == 0) continue;
                var outFilePath = @$"{_config.OutputFilename}{index.Name.Replace(".win32.index", $"_hashed_names_{groups.Key}")}";
                foreach (var (_, entry) in groups)
                    if (!HashDatabaseAccessor.FileExists(unchecked((int) entry.FileName)))
                        thisFileContents.Add($"{entry.FileName:X8}:00000000");
                if (thisFileContents.Any())
                    File.WriteAllLines(outFilePath, thisFileContents);
                thisFileContents.Clear();
            }
        }
    }

    private void FindExtension(string cat, uint fileName)
    {
            
    }

    class IndexHasher
    {
        public class HashEntry
        {
            public uint FullPath;
            public uint Folder;
            public uint FileName;
            public uint FileNameLen;

            public override string ToString()
            {
                return $"Folder: {Folder:X8}; FileName: {FileName:X8}; FullPath: {FullPath:X8}; FileNameLen: {FileNameLen}";
            }
        }

        private Dictionary<ulong, IndexHashTableEntry> HashTableEntries { get; set; }
        private Dictionary<uint, Index2HashTableEntry> HashTableEntries2 { get; set; }

        public Dictionary<uint, HashEntry> HashTable { get; set; } = new Dictionary<uint, HashEntry>();

        private FileInfo Index { get; set; }
        private FileInfo Index2 { get; set; }

        private Crc32 Crc { get; set; } = new Crc32();

        public void GuessTableLengths()
        {
            foreach (var (offset, entry) in HashTable)
            {
                if (GuessTableLength(entry, out var len))
                {
                    entry.FileNameLen = len;
                }
                else
                {
                    Console.WriteLine($"Failed at {offset:X8}. {entry}");
                }
            }
        }

        private bool GuessTableLength(HashEntry entry, out uint len)
        {
            Crc.Crc = entry.Folder;
            Crc.Update(0x2f);
            Crc.Crc = Crc.Checksum;
            for (len = 1; len <= 4096; len++)
            {
                Crc.Update(0x00);
                if ((Crc.Crc ^ entry.FileName) == entry.FullPath)
                    return true;
            }

            return false;
        }

        internal void MakeOffsetDict()
        {
            foreach (var (hash, entry) in HashTableEntries)
            {
                var hashEntry = new HashEntry
                {
                    FileName = (uint) (hash >> 0),
                    Folder = (uint) (hash >> 32)
                };

                HashTable.Add(entry.data, hashEntry);
            }

            foreach (var (hash, entry) in HashTableEntries2)
            {
                if (HashTable.TryGetValue(entry.data, out var hashEntry))
                {
                    hashEntry.FullPath = hash;
                }
                else
                {
                    Console.WriteLine($"Uh-oh. Index vs Index2 mismatch Off:{entry.data:X8}; Hash:{entry.hash:X8}");
                }
            }
        }

        public IndexHasher(FileInfo index, FileInfo index2)
        {
            Index = index;
            Index2 = index2;

            HashTableEntries = Util.LoadIndex(Index.OpenRead());
            HashTableEntries2 = Util.LoadIndex2(Index2.OpenRead());
        }
    }
}