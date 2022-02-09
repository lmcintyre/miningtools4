// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using SaintCoinach;
// using SaintCoinach.Xiv;
//
// namespace miningtools4 {
//     class CutFinder {
//
//         private ARealmReversed realm;
//
//         private string cutbFormat = "cut/{0}.cutb";
//         private List<string> cut;
//
//         public CutFinder(ARealmReversed realm) {
//             this.realm = realm;
//             cut = new List<string>();
//         }
//
//         private void GetCut() {
//             IXivSheet<IXivRow> cutscene = realm.GameData.GetSheet("Cutscene");
//             IXivSheet<IXivRow> bmCutscene = realm.GameData.GetSheet("BenchmarkCutSceneTable");
//
//             foreach (var xivRow in cutscene)
//                 cut.Add(string.Format(cutbFormat, xivRow[0]));
//
//             foreach (var xivRow in bmCutscene)
//                 cut.Add(string.Format(cutbFormat, xivRow[0]));
//         }
//
//         public void WritePaths(string path, bool output = false) {
//             GetCut();
//             var lines = cut.Where(x => realm.Packs.FileExists(x))
//                 .Select(x => string.Format($"1, {x}"))
//                 .ToList();
//
//             File.WriteAllLines(path, lines);
//             if (output)
//                 lines.ForEach(Console.WriteLine);
//         }
//     }
// }
