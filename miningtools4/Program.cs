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
    }
}






















