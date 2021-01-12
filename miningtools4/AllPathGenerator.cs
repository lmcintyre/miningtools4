using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lumina.Data;
using Lumina.Data.Structs;
using Microsoft.Data.Sqlite;

namespace miningtools4
{
    public class AllPathGenerator
    {
        private Lumina.Lumina _lumina;
        private GeneratorConfig _config;

        private Dictionary<uint, string> extDict;
        private List<string> _lines;

        public AllPathGenerator(Lumina.Lumina lumina, GeneratorConfig config)
        {
            _lumina = lumina;
            _config = config;

            MakeDict();
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

        private void MakeDict()
        {
            extDict = new Dictionary<uint, string>();
            //
            // extDict.Add(0x00616574, "aet");
            // extDict.Add(0x414D4200, "amb");
            // extDict.Add(0x41564658, "avfx");
            // extDict.Add(0x00617774, "awt");
            // extDict.Add(0x42545543, "cutb");
            // extDict.Add(0x00656964, "eid");
            // extDict.Add(0x454E5642, "envb");
            // extDict.Add(0x42535345, "essb");
            // extDict.Add(0x46445845, "exd");
            // extDict.Add(0x46485845, "exh");
            // extDict.Add(0x45584C54, "exl");
            // extDict.Add(0x76736366, "fdt");
            // extDict.Add(0x64746667, "gfd");
            // extDict.Add(0x67676420, "ggd");
            // extDict.Add(0x647A6700, "gzd");
            // extDict.Add(0x4C434231, "lcb");
            // extDict.Add(0x4C474231, "lgb");
            // extDict.Add(0x1B4C7561, "luab");
            // extDict.Add(0x4C564231, "lvb");
            // extDict.Add(0x746C6D00, "mlt");
            // extDict.Add(0x4D534246, "msb");
            // extDict.Add(0x4F425342, "obsb");
            // extDict.Add(0x20706170, "pap");
            // extDict.Add(0x7074626C, "ptbl");
            // extDict.Add(0x42444553, "scd");
            // extDict.Add(0x31424753, "sgb");
            // extDict.Add(0x64436853, "shcd");
            // extDict.Add(0x5368506B, "shpk");
            // extDict.Add(0x626C6B73, "sklb");
            // extDict.Add(0x706C6B73, "skp");
            // extDict.Add(0x31425653, "svb");
            // extDict.Add(0x544D4C42, "tmb");
            // extDict.Add(0x756C6468, "uld");
            // extDict.Add(0x31425755, "uwb");
            
            extDict.Add(0x00616574, "aet");
            extDict.Add(0x414D4200, "amb");
            extDict.Add(0x41564658, "avfx");
            extDict.Add(0x00617774, "awt");
            extDict.Add(0x42545543, "cutb");
            extDict.Add(0x00656964, "eid");
            extDict.Add(0x42564E45, "envb");
            extDict.Add(0x42535345, "essb");
            extDict.Add(0x46445845, "exd");
            extDict.Add(0x46485845, "exh");
            extDict.Add(0x45584C54, "exl");
            extDict.Add(0x76736366, "fdt");
            extDict.Add(0x64746667, "gfd");
            extDict.Add(0x67676420, "ggd");
            extDict.Add(0x647A6700, "gzd");
            extDict.Add(0x3142434C, "lcb");
            extDict.Add(0x3142474C, "lgb");
            extDict.Add(0x1B4C7561, "luab");
            extDict.Add(0x3142564C, "lvb");
            extDict.Add(0x746C6D00, "mlt");
            extDict.Add(0x4D534246, "msb");
            extDict.Add(0x4253424F, "obsb");
            extDict.Add(0x20706170, "pap");
            extDict.Add(0x7074626C, "ptbl");
            extDict.Add(0x42444553, "scd");
            extDict.Add(0x31424753, "sgb");
            extDict.Add(0x64436853, "shcd");
            extDict.Add(0x5368506B, "shpk");
            extDict.Add(0x626C6B73, "sklb");
            extDict.Add(0x706C6B73, "skp");
            extDict.Add(0x31425653, "svb");
            extDict.Add(0x544D4C42, "tmb");
            extDict.Add(0x756C6468, "uld");
            extDict.Add(0x31425755, "uwb");
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

                            var file = cat.DatFiles[index.DataFileId].ReadFile<FileResource>(index.Offset);
                            uint sig = BitConverter.ToUInt32(file.DataSpan.Slice(0, 4));
                            if (!extDict.TryGetValue(sig, out var ext))
                            {
                                ext = file.FileInfo.Type switch
                                {
                                    FileType.Model => "mdl",
                                    FileType.Texture => "tex",
                                    _ => ""
                                };
                            }

                            string outputLine = $"{catId}: {HashDatabaseAccessor.GetFullPath(folderHash, fileHash)} | {ext}";
                            lines.Add(outputLine);
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
                sortedLines.Add(taken);
            }
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
}