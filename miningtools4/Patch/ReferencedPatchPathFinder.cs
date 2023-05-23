using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Lumina;
using Lumina.Data;
using Lumina.Data.Structs;
using Lumina.Extensions;
using ZiPatchLib;
using ZiPatchLib.Chunk;
using ZiPatchLib.Chunk.SqpkCommand;
using ZiPatchLib.Util;

namespace miningtools4;

// Searches all files for references to other 
public class ReferencedPatchPathFinder
{
    private GameData _lumina;
    private GeneratorConfig _config;

    private List<string> _lines;

    private string[] _searchTerms = {"common/", "bgcommon/", "bg/", "cut/", "chara/", "shader/", "ui/", "sound/", "vfx/", "ui_script/", "exd/", "game_script/", "music/", "sqpack_test/", "debug/"};
    private Dictionary<string, byte[]> _searchBytesMap = new Dictionary<string, byte[]>();
    private readonly string _patchFilePath;
    private string _patchFileName;

    public ReferencedPatchPathFinder(GameData lumina, GeneratorConfig config, string patchFilePath)
    {
        _lumina = lumina;
        _config = config;
        _patchFilePath = patchFilePath;

        foreach (var term in _searchTerms)
        {
            _searchBytesMap[term] = Encoding.ASCII.GetBytes(term);
        }
        _lines = Load();
    }

    public void Output()
    {
        if (_lines == null) Load();

        if (_config.OutputToConsole)
            _lines.ForEach(Console.WriteLine);
        if (_config.OutputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_config.OutputFilename));
            File.WriteAllLines(_config.OutputFilename.Replace("%patch%", _patchFileName), _lines);
        }
    }

    private bool IsInRange(byte b)
    {
        return (b >= 45 && b <= 57) || (b >= 65 && b <= 90) || (b >= 97 && b <= 122) || b == 95;
    }

    private List<string> SearchFile(byte[] data, int length)
    {
        var results = new List<string>();
        for (int i = 0; i < length; i++)
        {
            // here we don't want to search if it's part of an existing path
            // i.e. if a path goes "...ff/eeebg/gjrijije" or something,
            // we don't care about that bg
            if (i == 0 || !IsInRange(data[i - 1]))
            {
                foreach (var _searchTerm in _searchTerms)
                {
                    if (i > length - _searchTerm.Length)
                        continue;
                    if (data.AsSpan().Slice(i, _searchTerm.Length).SequenceEqual(_searchBytesMap[_searchTerm]))
                    {
                        var bytes = new List<byte>();
                        int foundIndex = i;
                        while (IsInRange(data[foundIndex]))
                            bytes.Add(data[foundIndex++]);
                        var str = Encoding.ASCII.GetString(bytes.ToArray());
                        if (str.Contains('.'))
                            results.Add(str);
                    }
                }
            }
        }

        return results;
    }
        
    private List<string> Load()
    {
        ConcurrentBag<string> lines = new ConcurrentBag<string>();
        var stop = Stopwatch.StartNew();

        var patchChunks = new List<ZiPatchChunk>();
        _patchFileName = Path.GetFileNameWithoutExtension(_patchFilePath).Replace(".patch", "");
        using (var patchFile = ZiPatchFile.FromFileName(_patchFilePath))
        {
            patchChunks = patchFile.GetChunks().ToList();
        }
            
        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024 * 10);
        var ms = new MemoryStream(buffer);
            
        foreach (var chunk in patchChunks)
        {
            if (chunk is SqpkAddData a)
            {
                var baseStream = new MemoryStream(a.BlockData);
                var length = DecompressFileBlock(baseStream, buffer, ms);

                foreach (var path in SearchFile(buffer, length))
                    lines.Add(path);
                Array.Clear(buffer);
            }
        }
            
        var searchTime = stop.ElapsedMilliseconds;
        var sortedLines = lines.ToHashSet().ToList();
        sortedLines.Sort();
        Console.WriteLine("--------------------");
        Console.WriteLine($"Search {TimeSpan.FromMilliseconds(searchTime):c}");
        Console.WriteLine("--------------------");
        Console.WriteLine($"Found {sortedLines.Count} paths.");
            
        return sortedLines;
    }

    protected int DecompressFileBlock(MemoryStream baseStream, byte[] buffer, MemoryStream dest)
    {
        var reader = new BinaryReader(baseStream);

        var hdr = reader.ReadStructure<DatBlockHeader>();
            
        int totalRead = 0;
        if( hdr.DatBlockType == DatBlockType.Uncompressed )
        {
            // fucking .net holy hell
            reader.Read( buffer, (int)dest.Position, (int)hdr.BlockDataSize );
        }
        else
        {
            using var zlibStream = new DeflateStream( baseStream, CompressionMode.Decompress, true );

            while( totalRead < hdr.BlockDataSize )
            {
                var bytesRead = zlibStream.Read( buffer, (int)dest.Position + totalRead, (int)hdr.BlockDataSize - totalRead );
                if( bytesRead == 0 ) { break; }
                totalRead += bytesRead;
            }

            if( totalRead != (int)hdr.BlockDataSize )
            {
                throw new SqPackInflateException(
                    $"failed to inflate block, bytesRead ({totalRead}) != BlockDataSize ({hdr.BlockDataSize})"
                );
            }
        }

        return totalRead;
    }
        
    [StructLayout( LayoutKind.Sequential )]
    struct DatBlockHeader
    {
        public UInt32 Size;
        // always 0?
        public UInt32 unknown1;
        public DatBlockType DatBlockType;
        public UInt32 BlockDataSize;
    };
}