using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Data.Structs;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4
{
    class Program
    {
        private static string GameDirectory = @"D:\RE\xiv_game\{0}\game\sqpack";
        private static string OutputDirectory = @"C:\Users\Liam\Desktop\miningtools4\{0}\{1}.txt";

        static void Main(string[] args)
        {
            string latestPatch = "5.41";
            string lastPatch = "5.4";
        
            LuminaOptions opt = new LuminaOptions();
            opt.PanicOnSheetChecksumMismatch = false;
        
            Lumina.Lumina luminaLatest = new Lumina.Lumina(string.Format(GameDirectory, latestPatch), opt);
            Lumina.Lumina luminaLast = new Lumina.Lumina(string.Format(GameDirectory, lastPatch), opt);
        
            var conf = new GeneratorConfig
            {
                OutputFile = true,
                OutputToConsole = false,
                CondensedOutput = true,
                BreakOnImcMissing = true,
                UseSheetsToFindUsed = true
            };
        
            // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $@"\search\{latestPatch}_allfile_%VAL%_search");
            // var afs = new AllFileSearcher(luminaLatest, conf, "bgm_ex3_ban_11");
            // afs.Output();
        
            // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $"{latestPatch}_{lastPatch}_sheetdiff");
            // var sd = new SheetDiffer(luminaLatest, luminaLast, conf);
            // sd.Output();
            
            // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $@"{latestPatch}_monsters");
            // var mg = new MonsterGenerator(luminaLatest, conf);
            // mg.Output();
            
            // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $@"{latestPatch}_weapons");
            // var wg = new WeaponGenerator(luminaLatest, conf);
            // wg.Output();
            
            // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $@"\used\{latestPatch}_equipment");
            // var eg = new EquipmentGenerator(luminaLatest, conf);
            // eg.Output();
        
            // conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, latestPatch, "_"));
            // var msdg = new MiscSheetDataGenerator(luminaLatest, conf);
            // msdg.Output();
        
            // conf.OutputFilename = string.Format(OutputDirectory, lastPatch, $"{lastPatch}_allfiles");
            // new AllPathGenerator(luminaLast, conf).Output();
            //
            // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $"{latestPatch}_allfiles");
            // new AllPathGenerator(luminaLatest, conf).Output();
        
            // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $"{latestPatch}_genpaths");
            // new ReferencedPathFinder(luminaLatest, conf).Output();
            // conf.OutputFilename = string.Format(OutputDirectory, lastPatch, $"{lastPatch}_genpaths");
            // new ReferencedPathFinder(luminaLast, conf).Output();
        }

        public static void Main2()
        {
            string latestPatch = "5.4";
            string lastPatch = "5.35x2";
        
            LuminaOptions opt = new LuminaOptions();
            opt.PanicOnSheetChecksumMismatch = false;
        
            Lumina.Lumina luminaLatest = new Lumina.Lumina(string.Format(GameDirectory, latestPatch), opt);
            Lumina.Lumina luminaLast = new Lumina.Lumina(string.Format(GameDirectory, lastPatch), opt);
        
            var conf = new GeneratorConfig
            {
                OutputFile = true,
                OutputToConsole = false,
                CondensedOutput = true,
                BreakOnImcMissing = true,
                UseSheetsToFindUsed = true
            };

            // var orchExtractFolder = @"C:\Users\Liam\Desktop\extract\mus\music\ffxiv\orchestrion";
            // var files = Directory.GetFiles(orchExtractFolder);
            // var orchPathDict = luminaLatest.GetExcelSheet<OrchestrionPath>()
            //     .Where(row => row.RowId != 0 && !string.IsNullOrEmpty(row.File.RawString))
            //     .ToDictionary(row => row.File.ToString().Substring(row.File.ToString().LastIndexOf("/") + 1).ToLower(), row => row.RowId);
            // var orchNameDict = luminaLatest.GetExcelSheet<Orchestrion>()
            //     .Where(row => row.RowId != 0)
            //     .ToDictionary(row => row.RowId, row => row.Name.ToString());
            //
            // foreach (var file in files)
            // {
            //     var baseName = Path.GetFileNameWithoutExtension(file);
            //     if (!orchPathDict.TryGetValue(baseName, out var id))
            //         continue;
            //     if (!orchNameDict.TryGetValue(id, out var realName))
            //         continue;
            //     foreach (var character in Path.GetInvalidFileNameChars())
            //         realName = realName.Replace(character, '_');
            //     Console.WriteLine($"{baseName} => {realName}");
            //     File.Move(orchExtractFolder + "\\" + baseName + ".ogg", orchExtractFolder + "\\" + realName + ".ogg");
            // }

            // var bgmExtractFolder = @"C:\Users\Liam\Desktop\extract\mus\music\music";
            // var files = Directory.GetFiles(bgmExtractFolder);
            // var bgmPathDict = luminaLatest.GetExcelSheet<BGM>()
            //     .Where(row => row.RowId != 0 && !string.IsNullOrEmpty(row.File.RawString))
            //     .GroupBy(row => row.File.ToString())
            //     .ToDictionary(output => output.Key.Substring(output.Key.LastIndexOf('/') + 1).ToLower(), output => output.ToList());
            // foreach (var file in files)
            // {
            //     var baseName = Path.GetFileNameWithoutExtension(file);
            //     if (!bgmPathDict.TryGetValue(baseName, out var bgms))
            //         continue;
            //     bgms.Sort((bgm1, bgm2) => bgm1.RowId.CompareTo(bgm2.RowId));
            //     string ids = "";
            //     foreach (var bgm in bgms)
            //         ids += bgm.RowId + ",";
            //     var newName = $"{ids.Substring(0, ids.Length - 1)} - {baseName}";
            //     Console.WriteLine($"{baseName} => {newName}");
            //     File.Move(bgmExtractFolder + "\\" + baseName + ".ogg", bgmExtractFolder + "\\" + newName.Replace(".scd", ".ogg"));
            // }

            // var ttype = luminaLatest.GetExcelSheet<TerritoryType>().ToDictionary(row => row.RowId, row => row);
            // var bgmSwitch = luminaLatest.GetExcelSheet<BGMSwitch>()
            //     .GroupBy()
            // var bgm = luminaLatest.GetExcelSheet<BGM>().ToDictionary(row => row.RowId, row => row);
            // var bgmSituation = luminaLatest.GetExcelSheet<BGMSituation>(row => row.RowId, row => row);
            // foreach (var terri in ttype)
            // {
            //     
            // }
        }
    }
}






















