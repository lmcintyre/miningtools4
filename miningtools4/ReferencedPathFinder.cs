using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Data;
using Lumina.Data.Structs;

namespace miningtools4
{
    // Searches all files for references to other 
    public class ReferencedPathFinder
    {
        private Lumina.Lumina _lumina;
        private GeneratorConfig _config;

        private List<string> _lines;

        private string[] _searchTerms = {"common/", "bgcommon/", "bg/", "cut/", "chara/", "shader/", "ui/", "sound/", "vfx/", "ui_script/", "exd/", "game_script/", "music/", "sqpack_test/", "debug/"};
        private string _xivCharsetRegex = "[A-Za-z0-9._-/]";
        
        public ReferencedPathFinder(Lumina.Lumina lumina, GeneratorConfig config)
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
            if (_config.OutputFile)
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
                foreach (var _searchTerm in _searchTerms)
                {
                    if (i > file.Data.Length - _searchTerm.Length)
                        continue;
                    // here we don't want to search if it's part of an existing path
                    // i.e. if a path goes "...ff/eeebg/gjrijije" or something,
                    // we don't care about that bg
                    if (i == 0 || !IsInRange(file.Data[i - 1]))
                    {
                        if (file.DataSpan.Slice(i, _searchTerm.Length).SequenceEqual(Encoding.ASCII.GetBytes(_searchTerm)))
                        {
                            var bytes = new List<byte>();
                            int foundIndex = i;
                            while (IsInRange(file.Data[foundIndex]))
                                bytes.Add(file.Data[foundIndex++]);
                            results.Add(Encoding.ASCII.GetString(bytes.ToArray()));
                        }    
                    }
                }
            }

            return results;
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

            var sortedLines = new List<string>();
            while (!lines.IsEmpty)
            {
                lines.TryTake(out var taken);
                if (!sortedLines.Contains(taken))
                    sortedLines.Add(taken);
            }
            sortedLines.Sort();
            return sortedLines;
        }
    }
}