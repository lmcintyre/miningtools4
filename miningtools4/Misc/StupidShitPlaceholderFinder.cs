using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lumina;
using Lumina.Data.Structs;

namespace miningtools4;

public class StupidShitPlaceholderFinder
{
    private GameData _lumina;
    private GeneratorConfig _config;

    private Dictionary<uint, string> extDict;
    private List<string> _lines;

    public StupidShitPlaceholderFinder(GameData lumina, GeneratorConfig config)
    {
        _lumina = lumina;
        _config = config;

        _lines = Load();
    }

    public void Output()
    {
        if (_lines == null) Load();
            
        if (_config.OutputToConsole)
            _lines.ForEach(Console.WriteLine);
        // if (_config.OutputPath)
        // {
        //     Directory.CreateDirectory(Path.GetDirectoryName(_config.OutputFilename));
        //     File.WriteAllLines(_config.OutputFilename, _lines);
        // }
    }

    private List<string> Load()
    {
        ConcurrentBag<string> lines = new ConcurrentBag<string>();
        Parallel.ForEach(_lumina.Repositories, (repo) =>
        {
            Parallel.ForEach(repo.Value.Categories.SelectMany(x => x.Value), (cat) =>
            {
                foreach (var (hash, index) in cat.IndexHashTableEntries)
                {
                    try
                    {
                        string catId = $"{cat.CategoryId:D2}{cat.Expansion:D2}{cat.Chunk:D2}";
                        int folderHash = (int) ((hash & 0xFFFFFFFF00000000) >> 32);
                        int fileHash = (int) (hash & 0x00000000FFFFFFFF);
                            
                        var meta = cat.DatFiles[index.DataFileId].GetFileMetadata(index.Offset);
                        if (meta.Type == FileType.Empty && meta.RawFileSize != 0)
                        {
                            string outputLine = $"{catId}: {HashDatabaseAccessor.GetFullPath(folderHash, fileHash)}";
                            lines.Add(outputLine);
                        }
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

        var sortedLines = lines.ToHashSet().ToList();
        sortedLines.Sort();
        return sortedLines;
    }
}

/*
 * foreach( var repo in lumina.Repositories )
    {
        foreach( var cat in repo.Value.Categories.SelectMany(x => x.Value) )
        {
            foreach( var (hash, index) in cat.IndexHashTableEntries )
            {
                var file = cat.DatFiles[ index.DataFileId ].ReadFile< FileResource >( index.Offset );
            }
        }
    }
 */