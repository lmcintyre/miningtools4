using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lumina;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4;

struct Minimap {
    public string code;
    public int part;
    public bool inTeri;
    public bool inMap;
}
    
public class MinimapFinder {
    private GameData _lumina;
    private GeneratorConfig _config;

    private string minimapFormatBase = "{0}/{1:D2}";
    private string minimapFormat1 = "ui/map/{0}/{1:D2}/{0}{1:D2}_m.tex";
    private string minimapFormat2 = "ui/map/{0}/{1:D2}/{0}{1:D2}m_m.tex";

    private List<string> _mapUsedMaps;
    private List<string> _teriUsedMaps;
    private readonly List<string> _paths;
    private readonly List<Minimap> _maps;

    public MinimapFinder(GameData lumina, GeneratorConfig config) {
        _lumina = lumina;
        _config = config;
        _maps = new List<Minimap>();
        _paths = new List<string>();
    }

    private void GetMaps() {
        var generatedCodes = new List<string>();
            
        for (int a = 0; a < 36; a++) {
            for (int b = 0; b < 36; b++) {
                for (int c = 0; c < 36; c++) {
                    for (int d = 0; d < 36; d++) {
                
                        char[] code = new char[4];
                        code[0] = normalize(a);
                        code[1] = normalize(b);
                        code[2] = normalize(c);
                        code[3] = normalize(d);

                        generatedCodes.Add(new string(code));
                    }
                }
            }    
        }

        // for (int f = 0; f < 50000; f++) {
        // var code = generatedCodes[f];
        // foreach (var code in generatedCodes) {
        Parallel.ForEach(generatedCodes, code => {
                // Console.Write(".");
                for (int i = 0; i < 100; i++) {

                    var path1 = string.Format(minimapFormat1, code, i);
                    var path2 = string.Format(minimapFormat2, code, i);

                    var exists1 = _lumina.FileExists(path1);
                    var exists2 = _lumina.FileExists(path2);

                    if (exists1 || exists2) {
                        if (exists1)
                            _paths.Add(path1);
                        if (exists2)
                            _paths.Add(path2);
                        string formatted = string.Format(minimapFormatBase, code, i);

                        Minimap m = new Minimap();
                        m.code = code;
                        m.part = i;
                        if (_mapUsedMaps.Contains(formatted))
                            m.inMap = true;
                        if (_teriUsedMaps.Contains(formatted))
                            m.inTeri = true;

                        _maps.Add(m);
                    }
                }
            }
        );
            
        // _maps.Sort((m1, m2) => m1.code.CompareTo(m2.code));
    }

    private char normalize(int val) {

        int BASE_ASCII = 97;
        int BASE_NUM = 48;
            
        char vr;
        var v2 = val % 26;

        if (val <= 25)
            vr = (char) (BASE_ASCII + v2);
        else
            vr = (char) (BASE_NUM + v2);

        return vr;
    }

    private void GetUsedMaps() {
        _mapUsedMaps = new List<string>();
        _teriUsedMaps = new List<string>();
            
        var maps = _lumina.GetExcelSheet<Map>();
        var teris = _lumina.GetExcelSheet<TerritoryType>();
            
        foreach (var map in maps) {
            _mapUsedMaps.Add(map.Id);
        }

        foreach (var teri in teris) {
            _teriUsedMaps.Add(teri.Map.Value.Id);
        }
    }

    public void Output() {
        GetUsedMaps();
        GetMaps();
            
        var outLines = new List<string>();
            
        foreach (var map in _maps) {
            var full = string.Format(minimapFormatBase, map.code, map.part);
            outLines.Add($"{full} | teri {(map.inTeri ? 1 : 0)} | map {(map.inMap ? 1 : 0)}");
        }

        outLines.Sort();
        _paths.Sort();
            
        if (_config.OutputToConsole)
            outLines.ForEach(Console.WriteLine);
            
        File.WriteAllLines(_config.OutputFilename, outLines);
        File.WriteAllLines(_config.OutputFilename.Replace(".txt", "_paths.txt"), _paths);
    }

    public void WritePaths(string path, bool output = false) {
        // GetCut();
        // var lines = cut.Where(x => lumina.FileExists(x))
        //     .Select(x => string.Format($"1, {x}"))
        //     .ToList();
        //
        // File.WriteAllLines(path, lines);
        // if (output)
        //     lines.ForEach(Console.WriteLine);
    }
}