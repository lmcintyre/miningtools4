using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using Lumina.Data.Parsing.Layer;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;

namespace miningtools4
{
    public class SheetDiffer
    {
        private Lumina.Lumina _latest;
        private Lumina.Lumina _last;
        private GeneratorConfig _config;
        private List<string> _lines;
        
        public SheetDiffer(Lumina.Lumina luminaLatest, Lumina.Lumina luminaLast,  GeneratorConfig config)
        {
            _latest = luminaLatest;
            _last = luminaLast;
            _config = config;

            Load();
        }
        
        public void Output()
        {
            if (_lines == null)
                Load();

            if (_config.OutputToConsole)
                _lines.ForEach(Console.WriteLine);
            if (_config.OutputFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_config.OutputFilename));
                File.WriteAllLines(_config.OutputFilename, _lines);
            }
        }

        private void Load()
        {
            _lines = new List<string>();
            var sheetNames1 = _latest.Excel.SheetNames;
            var sheetNames2 = _last.Excel.SheetNames;
            
            var sheetDefs1 = new Dictionary<string, ExcelColumnDefinition[]>();
            var sheetDefs2 = new Dictionary<string, ExcelColumnDefinition[]>();

            foreach (var sheetName in sheetNames1)
            {
                sheetDefs1.Add(sheetName, _latest.Excel.GetSheetRaw(sheetName).Columns);
                if (!sheetNames2.Contains(sheetName))
                    _lines.Add($"Added: {sheetName}");
                else
                    sheetDefs2.Add(sheetName, _last.Excel.GetSheetRaw(sheetName).Columns);
            }

            foreach (var sheetName in sheetNames2)
            {
                if (!sheetNames1.Contains(sheetName))
                    _lines.Add($"Removed: {sheetName}");
            }

            foreach (var sheetName in sheetNames1)
            {
                if (!sheetDefs1.ContainsKey(sheetName) || !sheetDefs2.ContainsKey(sheetName)) continue;
                GetColumnDiffResults(sheetName, sheetDefs1[sheetName], sheetDefs2[sheetName]);
            }
        }

        private void GetColumnDiffResults(string sheetName, ExcelColumnDefinition[] news, ExcelColumnDefinition[] olds)
        {
            // yeah i don't care
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            for (int i = 0; i < news.Length; i++)
            {
                sb1.Append($"{news[i].Type} ");
            }

            for (int i = 0; i < olds.Length; i++)
            {
                sb2.Append($"{olds[i].Type} ");
            }

            if (sb1.ToString().Equals(sb2.ToString(), StringComparison.InvariantCulture)) return;

            var differ = new Differ();
            var diff = differ.CreateWordDiffs(sb2.ToString(), sb1.ToString(), true, new [] {' '});
            _lines.Add($"{sheetName}: ");
            foreach (var diffs in diff.DiffBlocks)
            {
                StringBuilder sbAdd = new StringBuilder();
                for (int i = diffs.InsertStartB; i < diffs.InsertStartB + diffs.InsertCountB; i++)
                    sbAdd.Append(diff.PiecesNew[i]);
                
                StringBuilder sbRm = new StringBuilder();
                for (int i = diffs.DeleteStartA; i < diffs.DeleteStartA + diffs.DeleteCountA; i++)
                    sbRm.Append(diff.PiecesOld[i]);
                
                if (!string.IsNullOrEmpty(sbRm.ToString()))
                    _lines.Add($"\tRemoved: {sbRm.ToString().Trim()} starting at column {diffs.DeleteStartA / 2}");
                if (!string.IsNullOrEmpty(sbAdd.ToString()))
                    _lines.Add($"\tAdded: {sbAdd.ToString().Trim()} starting at column {diffs.InsertStartB / 2}");
            }
        }
    }
}