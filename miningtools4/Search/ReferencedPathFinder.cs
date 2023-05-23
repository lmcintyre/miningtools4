using System;
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

// Searches all files for references to other 
public class ReferencedPathFinder
{
    private GameData _lumina;
    private GeneratorConfig _config;

    private List<string> _lines;

    private string[] _searchTerms = {"common/", "bgcommon/", "bg/", "cut/", "chara/", "shader/", "ui/", "sound/", "vfx/", "ui_script/", "exd/", "game_script/", "music/", "sqpack_test/", "debug/"};
    private Dictionary<string, byte[]> _searchBytesMap = new Dictionary<string, byte[]>();

    public ReferencedPathFinder(GameData lumina, GeneratorConfig config)
    {
        _lumina = lumina;
        _config = config;

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

    private List<string> SearchFile(FileResource file)
    {
        var results = new List<string>();
        for (int i = 0; i < file.Data.Length; i++)
        {
            // here we don't want to search if it's part of an existing path
            // i.e. if a path goes "...ff/eeebg/gjrijije" or something,
            // we don't care about that bg
            if (i == 0 || !IsInRange(file.Data[i - 1]))
            {
                foreach (var _searchTerm in _searchTerms)
                {
                    if (i > file.Data.Length - _searchTerm.Length)
                        continue;
                    if (file.DataSpan.Slice(i, _searchTerm.Length).SequenceEqual(_searchBytesMap[_searchTerm]))
                    {
                        var bytes = new List<byte>();
                        int foundIndex = i;
                        while (IsInRange(file.Data[foundIndex]))
                            bytes.Add(file.Data[foundIndex++]);
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
        Parallel.ForEach(_lumina.Repositories, (repo) =>
        {
            Parallel.ForEach(repo.Value.Categories.SelectMany(x => x.Value), (cat) =>
            {
                foreach (var (hash, index) in cat.IndexHashTableEntries)
                {
                    try
                    {
                        var meta = cat.DatFiles[index.DataFileId].GetFileMetadata(index.Offset);
                        if (meta.Type == FileType.Texture) continue;
                            
                        var file = cat.DatFiles[index.DataFileId].ReadFile<FileResource>(index.Offset);
                        foreach (var path in SearchFile(file))
                            lines.Add(path);
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
        Console.WriteLine($"Found {sortedLines.Count} paths.");
            
        return sortedLines;
    }
}