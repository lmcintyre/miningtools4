﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina;
using Lumina.Data;
using Lumina.Data.Structs;

namespace miningtools4;

public class AllFileSearcher
{
    private GameData _lumina;
    private GeneratorConfig _config;

    private List<string> _lines;

    private string _searchTerm = "";

    private byte[] _searchTermBytes;
    // private string _xivCharsetRegex = "[A-Za-z0-9._-/]";

    public AllFileSearcher(GameData lumina, GeneratorConfig config, string searchTerm)
    {
        _lumina = lumina;
        _config = config;
        _searchTerm = searchTerm;
        _searchTermBytes = Encoding.ASCII.GetBytes(_searchTerm);

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
                
            string sanitizedTerm = _searchTerm
                .Replace("/", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace(":", "")
                .Replace("\"", "")
                .Replace("|", "")
                .Replace("?", "")
                .Replace("*", "");

            string realOut = _config.OutputFilename.Replace("%VAL%", sanitizedTerm);
                    
            File.WriteAllLines(realOut, _lines);
        }
    }

    private bool IsInRange(byte b)
    {
        return (b >= 45 && b <= 57) || (b >= 65 && b <= 90) || (b >= 97 && b <= 122) || b == 95;
    }

    private List<string> SearchFile(FileResource file)
    {
        var results = new List<string>();
        for (int i = 0; i < file.Data.Length - _searchTerm.Length; i++)
        {
            if (file.DataSpan.Slice(i, _searchTerm.Length).SequenceEqual(_searchTermBytes))
                // if (Encoding.ASCII.GetString(file.DataSpan.Slice(i, _searchTerm.Length)).ToLower() == _searchTerm)
            {
                var bytes = new List<byte>();

                int backIndex = i - 1;
                while (IsInRange(file.Data[backIndex]))
                    bytes.Add(file.Data[backIndex--]);
                bytes.Reverse();
                    
                int foundIndex = i;
                while (IsInRange(file.Data[foundIndex]))
                    bytes.Add(file.Data[foundIndex++]);
                i += foundIndex - i;
                results.Add(Encoding.ASCII.GetString(bytes.ToArray()));
            }
        }

        return results;
    }

    private List<string> Load()
    {
        ConcurrentBag<string> lines = new ConcurrentBag<string>();
        var stop = Stopwatch.StartNew();
        Parallel.ForEach(_lumina.Repositories, (repo) =>
        {
            Parallel.ForEach(repo.Value.Categories.SelectMany(x => x.Value), (cat) =>
            {
                foreach (var (hash, index) in cat.IndexHashTableEntries)
                {
                    try
                    {
                        string catId = $"{cat.CategoryId:X2}{cat.Expansion:X2}{cat.Chunk:X2}";
                        int folderHash = (int) ((hash & 0xFFFFFFFF00000000) >> 32);
                        int fileHash = (int) (hash & 0x00000000FFFFFFFF);
                            
                        var meta = cat.DatFiles[index.DataFileId].GetFileMetadata(index.Offset);
                        if (meta.Type == FileType.Texture) continue;
                            
                        var file = cat.DatFiles[index.DataFileId].ReadFile<FileResource>(index.Offset);
                            
                        foreach (var path in SearchFile(file))
                            lines.Add($"{catId}: {HashDatabaseAccessor.GetFullPath(folderHash, fileHash)} | {path}");
                    }
                    catch (Exception e)
                    {
                        // Console.WriteLine("Error reading file.");
                        // Console.WriteLine(e.Message);
                        // Console.WriteLine(e.StackTrace);
                    }
                }
            });
        });

        var searchTime = stop.ElapsedMilliseconds;
        var sortedLines = lines.ToHashSet().ToList();
        sortedLines.Sort();
        Console.WriteLine("--------------------");
        Console.WriteLine($"Search {TimeSpan.FromMilliseconds(searchTime):c}");
        Console.WriteLine("--------------------");
        Console.WriteLine($"Found {sortedLines.Count} results.");
            
        return sortedLines;
    }
}