// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using SaintCoinach;
// using SaintCoinach.Xiv;
//
// namespace Pathfinder3 {
//
//     struct Minimap {
//         public string code;
//         public int part;
//         public bool inTeri;
//         public bool inMap;
//     }
//     
//     public class MinimapFinder {
//         private ARealmReversed realm;
//
//         private string minimapFormatBase = "{0}/{1:D2}";
//         private string minimapFormat1 = "ui/map/{0}/{1:D2}/{0}{1:D2}_m.tex";
//         private string minimapFormat2 = "ui/map/{0}/{1:D2}/{0}{1:D2}m_m.tex";
//
//         private List<string> mapUsedMaps;
//         private List<string> teriUsedMaps;
//         private List<string> paths;
//         private List<Minimap> maps;
//
//         public MinimapFinder(ARealmReversed realm) {
//             this.realm = realm;
//             maps = new List<Minimap>();
//             paths = new List<string>();
//         }
//
//         private void GetMaps() {
//             var generatedCodes = new List<string>();
//             
//             for (int a = 0; a < 36; a++) {
//                 for (int b = 0; b < 36; b++) {
//                     for (int c = 0; c < 36; c++) {
//                         for (int d = 0; d < 36; d++) {
//                 
//                             char[] code = new char[4];
//                             code[0] = normalize(a);
//                             code[1] = normalize(b);
//                             code[2] = normalize(c);
//                             code[3] = normalize(d);
//
//                             generatedCodes.Add(new string(code));
//
//                         }
//                     }
//                 }    
//             }
//
//             // for (int f = 0; f < 50000; f++) {
//                 // var code = generatedCodes[f];
//             foreach (var code in generatedCodes) {
//             // Parallel.ForEach(generatedCodes, code => {
//                 // Console.Write(".");
//                 for (int i = 0; i < 100; i++) {
//
//                     var path1 = string.Format(minimapFormat1, code, i);
//                     var path2 = string.Format(minimapFormat2, code, i);
//
//                     var exists1 = realm.Packs.FileExists(path1);
//                     var exists2 = realm.Packs.FileExists(path2);
//
//                     if (exists1 || exists2) {
//                         if (exists1)
//                             paths.Add(path1);
//                         if (exists2)
//                             paths.Add(path2);
//                         string formatted = string.Format(minimapFormatBase, code, i);
//
//                         Minimap m = new Minimap();
//                         m.code = code;
//                         m.part = i;
//                         if (mapUsedMaps.Contains(formatted))
//                             m.inMap = true;
//                         if (teriUsedMaps.Contains(formatted))
//                             m.inTeri = true;
//
//                         maps.Add(m);
//                     }
//                 }
//             }
//             // );
//             
//             maps.Sort((m1, m2) => m1.code.CompareTo(m2.code));
//         }
//
//         private char normalize(int val) {
//
//             int BASE_ASCII = 97;
//             int BASE_NUM = 48;
//             
//             char vr;
//             var v2 = val % 26;
//
//             if (val <= 25)
//                 vr = (char) (97 + v2);
//             else
//                 vr = (char) (48 + v2);
//
//             return vr;
//         }
//
//         private void GetUsedMaps() {
//             mapUsedMaps = new List<string>();
//             teriUsedMaps = new List<string>();
//             
//             var maps = (IXivSheet<Map>) realm.GameData.GetSheet("Map");
//             var teris = realm.GameData.GetSheet<TerritoryType>();
//             
//             foreach (var map in maps) {
//                 mapUsedMaps.Add(map.Id);
//             }
//
//             foreach (var teri in teris) {
//                 teriUsedMaps.Add(teri.Map.Id);
//             }
//         }
//
//         public void WriteDescriptive(string path, bool output = false) {
//             GetUsedMaps();
//             GetMaps();
//             
//             var outLines = new List<string>();
//             
//             foreach (var map in maps) {
//                 var full = string.Format(minimapFormatBase, map.code, map.part);
//                 outLines.Add(string.Format("{0} | teri {1} | map {2}", full, map.inTeri ? 1 : 0, map.inMap ? 1 : 0));
//             }
//
//             foreach (var line in outLines) {
//                 Console.WriteLine(line);
//                 if (File.Exists(path))
//                     File.Delete(path);
//                 File.WriteAllLines(path, outLines);
//                 File.WriteAllLines(path + "_paths", paths);
//             }
//         }
//
//         public void WritePaths(string path, bool output = false) {
//             // GetCut();
//             // var lines = cut.Where(x => lumina.FileExists(x))
//             //     .Select(x => string.Format($"1, {x}"))
//             //     .ToList();
//             //
//             // File.WriteAllLines(path, lines);
//             // if (output)
//             //     lines.ForEach(Console.WriteLine);
//         }
//     }
// }