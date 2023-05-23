using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
public class ReferencedPatchPathFinder2
{
    private GameData _lumina;
    private GeneratorConfig _config;

    private List<string> _lines;

    private string[] _searchTerms = {"common/", "bgcommon/", "bg/", "cut/", "chara/", "shader/", "ui/", "sound/", "vfx/", "ui_script/", "exd/", "game_script/", "music/", "sqpack_test/", "debug/"};
    private Dictionary<string, byte[]> _searchBytesMap = new Dictionary<string, byte[]>();
    private readonly string _patchFilePath;
    private string _patchFileName;
    private readonly Dictionary<string, SparseMemoryStream> _datStreams = new();

    private readonly Dictionary<uint, MemoryStream> _fileDatStreams = new();
    private readonly Dictionary<uint, MemoryStream> _fileIndexStreams = new();

    public ReferencedPatchPathFinder2(GameData lumina, GeneratorConfig config, string patchFilePath)
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
            File.WriteAllLines(_config.OutputFilename, _lines);
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
    
    void ParseChunk(SqpkAddData chunk)
    {
        chunk.TargetFile.ResolvePath(ZiPatchConfig.PlatformId.Win32);
        if (!_datStreams.TryGetValue(chunk.TargetFile.ToString(), out var stream))
        {
            Debug.WriteLine($"New sparse entry for {chunk.TargetFile}");
            stream = new SparseMemoryStream();
            _datStreams.Add(chunk.TargetFile.ToString(), stream);
        }

        stream.Position = chunk.BlockOffset;
        stream.Write(chunk.BlockData, 0, chunk.BlockData.Length);
    }

    void ParseChunk(SqpkFile chunk)
    {
        var isDat = chunk.TargetFile.RelativePath.Contains(".dat");
        var isIndex = chunk.TargetFile.RelativePath.Contains(".index");
        if (chunk.TargetFile.RelativePath.Contains(".index2")) return;
        if (!isDat && !isIndex) return;

        var tmp = chunk.TargetFile.RelativePath;
        var lastSlash = tmp.LastIndexOf('/');
        var win32Index = tmp.IndexOf(".win32");
        var idStr = tmp[(lastSlash + 1)..win32Index];
        var id = uint.Parse(idStr, NumberStyles.HexNumber);
        
        if (isDat)
        {
            if (!_fileDatStreams.TryGetValue(id, out var stream))
            {
                Debug.WriteLine($"New sparse entry for {chunk.TargetFile}");
                stream = new MemoryStream();
                _fileDatStreams.Add(id, stream);
            }

            stream.Position = chunk.FileOffset;
            foreach (var data in chunk.CompressedData)
                data.DecompressInto(stream);
        }
        else if (isIndex)
        {
            if (!_fileIndexStreams.TryGetValue(id, out var stream))
            {
                Debug.WriteLine($"New sparse entry for {chunk.TargetFile}");
                stream = new MemoryStream();
                _fileIndexStreams.Add(id, stream);
            }

            stream.Position = chunk.FileOffset;
            foreach (var data in chunk.CompressedData)
                data.DecompressInto(stream);
        }
    }

    private List<string> Load()
    {
        ConcurrentBag<string> lines = new ConcurrentBag<string>();
        var stop = Stopwatch.StartNew();

        var patchChunks = new List<ZiPatchChunk>();
        _patchFileName = Path.GetFileNameWithoutExtension(_patchFilePath).Replace(".patch", "");
        if (_patchFileName.StartsWith("H"))
            return new List<string>();

        using (var patchFile = ZiPatchFile.FromFileName(_patchFilePath))
        {
            patchChunks = patchFile.GetChunks().ToList();
        }
        
        foreach (var chunk in patchChunks)
        {
            if (chunk is SqpkAddData addData)
                ParseChunk(addData);
            if (chunk is SqpkFile file)
                ParseChunk(file);
        }

        Parallel.ForEach(_datStreams, pair =>
        {
            var (file, stream) = pair;
            Debug.WriteLine($"File: {file}");
            Debug.WriteLine($"Blocks: {string.Join(", ", stream.GetPopulatedChunks())}");

            foreach (var (offset, subStream) in stream.ChunkDictionary)
            {
                using var sqStream = new SqPackStream(subStream, PlatformId.Win32);
                var meta = sqStream.GetFileMetadata(0);
                switch (meta.Type)
                {
                    case FileType.Empty:
                        Debug.WriteLine($"{file}:{offset:X08}:Retarded Shit File");
                        continue; // Skip this shit
                    case FileType.Model:
                        break;
                    case FileType.Texture:
                        Debug.WriteLine($"{file}:{offset:X08}:{meta.Type}");
                        continue; // Skip this shit too
                    case FileType.Standard:
                        break; // Interesting case
                }

                var fileResource = sqStream.ReadFile<FileResource>(0);
                foreach (var path in SearchFile(fileResource.Data, fileResource.Data.Length))
                    lines.Add(path);
            }
        });
        
        Parallel.ForEach(_fileDatStreams, pair =>
        // foreach (var pair in _fileDatStreams)
        {
            var (id, stream) = pair;
            Debug.WriteLine($"File: {id:X6} ({id})");
            if (!_fileIndexStreams.TryGetValue(id, out var indexStream))
                return;
            indexStream.Position = 0;
            var index = Util.LoadIndex(indexStream);
            Debug.WriteLine("Loaded index");

            var sq = new SqPackStream(stream, PlatformId.Win32);
            
            foreach (var entry in index)
            {
                var meta = sq.GetFileMetadata(entry.Value.Offset);
                Debug.WriteLine($"{entry.Key:X}:{entry.Value.Offset:X08}:{meta.Type}");

                switch (meta.Type)
                {
                    case (FileType)0:
                        Console.WriteLine($"Ran into a 0 file: {entry.Key:X}");
                        continue;
                    case FileType.Empty:
                        // Debug.WriteLine($"{entry.Key:X}:{entry.Value.Offset:X08}:Retarded Shit File");
                        continue; // Skip this shit
                    case FileType.Model:
                        break;
                    case FileType.Texture:
                        // Debug.WriteLine($"{entry.Key:X}:{entry.Value.Offset:X08}:{meta.Type}");
                        continue; // Skip this shit too
                    case FileType.Standard:
                        break; // Interesting case
                }
                
                var fileResource = sq.ReadFile<FileResource>(entry.Value.Offset);
                foreach (var path in SearchFile(fileResource.Data, fileResource.Data.Length))
                    lines.Add(path);
            }
        }
        );

        var searchTime = stop.ElapsedMilliseconds;
        var sortedLines = lines.ToHashSet().ToList();
        sortedLines.Sort();
        Console.WriteLine("--------------------");
        Console.WriteLine($"Search {TimeSpan.FromMilliseconds(searchTime):c}");
        Console.WriteLine("--------------------");
        Console.WriteLine($"Found {sortedLines.Count} paths.");
            
        return sortedLines;
    }
}

public class SparseMemoryStream : Stream
{
    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override long Length { get; }
    public override long Position { get; set; }
		
    public Dictionary<long, MemoryStream> ChunkDictionary = new Dictionary<long, MemoryStream>();
    public long StartPosition { get; set; }

    public IEnumerable<long> GetPopulatedChunks()
    {
        return ChunkDictionary.Keys;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!ChunkDictionary.TryGetValue(Position, out var stream))
            return 0;

        var r = stream.Read(buffer, offset, count);
        Position += count;

        return r;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        return Position;
    }

    public override void SetLength(long value)
    {
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!ChunkDictionary.TryGetValue(Position, out var stream))
        {
            stream = new MemoryStream();
            ChunkDictionary.Add(Position, stream);
        }

        stream.Write(buffer, offset, count);
        Position += count;
    }
}