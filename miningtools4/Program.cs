using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4;

class Program
{
    private static string GameDirectory = @"D:\RE\xiv_game\{0}\game\sqpack";
    private static string OutputDirectory = @"C:\Users\Liam\Desktop\miningtools4\{0}\{1}.txt";
    private static string OutputFormatPath = @"C:\Users\Liam\Desktop\miningtools4\{0}\{1}\";

    static void Main(string[] args)
    {
        string latestPatch = "6.38";
        string lastPatch = "6.35";
        // string olderPatch = "6.08x1";

        LuminaOptions opt = new LuminaOptions
        {
            PanicOnSheetChecksumMismatch = false,
            LoadMultithreaded = true,
        };

        GameData latest = new GameData(string.Format(GameDirectory, latestPatch), opt);
        GameData last = new GameData(string.Format(GameDirectory, lastPatch), opt);
        // GameData older = new GameData(string.Format(GameDirectory, olderPatch), opt);

        var conf = new GeneratorConfig
        {
            OutputPath = true,
            OutputToConsole = false,
            CondensedOutput = true,
            BreakOnImcMissing = true,
            UseSheetsToFindUsed = false
        };

        /*////////////////////////////////////////////////////////////////////////////////////////////
        // ----------------------------------- Sandbox -------------------------------------------- //
        ////////////////////////////////////////////////////////////////////////////////////////////*/
        /* ----------------------------------- Generate ACT resources -----------------------------------  */
        // conf.OutputFilename = string.Format(OutputFormatPath, latestPatch, $"act\\");
        // new ActResourceGenerator(latest, conf).Output();

        /* ----------------------------------- Generate RSFs ----------------------------------- */
        // conf.OutputFilename = @"C:\Users\Liam\Documents\repos\Serverless\Serverless\Data\rsf\";
        // var gen = new RsfGenerator(rsf, conf);
        // gen.Output("");

        /* ----------------------------------- Find RSFs ----------------------------------- */
        conf.OutputToConsole = true;
        new StupidShitPlaceholderFinder(latest, conf).Output();
        conf.OutputToConsole = false;
        
        /* ----------------------------------- Filename Lengths ----------------------------------- */
        // conf.OutputFilename = string.Format(OutputFormatPath, latestPatch, $"index\\");
        // conf.OutputFilename = @"C:\Users\Liam\Desktop\tmp\hashcat\hashcat\index\";
        // new IndexHashLengthCalculator(latest, conf).Output();

        /* ----------------------------------- Old simple sheet diff ----------------------------------- */
        // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, $"{latestPatch}_{lastPatch}_sheetdiff");
        // new SheetDiffer(latest, last, conf).Output();

        /*////////////////////////////////////////////////////////////////////////////////////////////
        // ----------------------------------- Begin actual mining -------------------------------- //
        ////////////////////////////////////////////////////////////////////////////////////////////*/
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "statusicon");
        new StatusIconFinder(latest, conf).Output();
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "itemicon");
        new ItemIconFinder(latest, conf).Output();
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "mapicons");
        new MinimapFinder(latest, conf).Output();
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "monsters");
        new MonsterGenerator(latest, conf).Output();
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "weapons");
        new WeaponGenerator(latest, conf).Output();
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "equipment");
        new EquipmentGenerator(latest, conf).Output();
        
        conf.UseSheetsToFindUsed = true;
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, @"\used\monsters");
        new MonsterGenerator(latest, conf).Output();
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, @"\used\weapons");
        new WeaponGenerator(latest, conf).Output();
        
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, @"\used\equipment");
        new EquipmentGenerator(latest, conf).Output();
        
        conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, latestPatch, "_"));
        new MiscSheetDataGenerator(latest, conf).Output();
        //
        // var top_mobs = new[] { 74u, 103u, 109u, 110u, 114u, 119u, 127u, 205u, 226u, 227u, 272u, 295u, 329u, 334u, 389u, 418u, 420u, 421u, 424u, 425u, 458u, 459u, 460u, 462u, 463u, 471u, 475u, 513u, 514u, 515u, 516u, 524u, 525u, 542u, 548u };
        //
        // conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, latestPatch, "_"));
        // new SpecificMobSearcher(latest, conf, top_mobs).Output();

        /*///////////////////////////////////////////////////////////////////////////////////
        // --------------------------------- Patch Files --------------------------------- //
        ///////////////////////////////////////////////////////////////////////////////////*/
        // conf.OutputToConsole = false;
        // conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, @"\patch\", "a"));
        //
        // var globalPatchBase = @"D:\RE\xiv_game\patch_archive\global\";
        // var krPatchBase = @"D:\RE\xiv_game\patch_archive\kr\";
        // var cnPatchBase = @"D:\RE\xiv_game\patch_archive\cn\";
        //
        // conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, @"\patch\global\", "a"));
        // var globalInspector = new PatchInspector(latest, conf);
        // conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, @"\patch\kr\", "a"));
        // var krInspector = new PatchInspector(latest, conf);
        // conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, @"\patch\cn\", "a"));
        // var cnInspector = new PatchInspector(latest, conf);
        //
        // try
        // {
        //     Parallel.ForEach(Directory.GetFiles(globalPatchBase, "*.patch", SearchOption.AllDirectories), file =>
        //     {
        //         globalInspector.Output(file);
        //     });
        //     
        //     Parallel.ForEach(Directory.GetFiles(krPatchBase, "*.patch", SearchOption.AllDirectories), file =>
        //     {
        //         krInspector.Output(file);
        //     });
        //     
        //     Parallel.ForEach(Directory.GetFiles(cnPatchBase, "*.patch", SearchOption.AllDirectories), file =>
        //     {
        //         cnInspector.Output(file);
        //     });
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e.Message);
        //     Console.WriteLine(e.StackTrace);
        // }

        // One patch
        // new PatchInspector(latest, conf).Output(@"D:\RE\xiv_game\patch_archive\global\game\D2023.01.11.0000.0000.patch");
        
        /* ----------------------------------- Referenced path finder for patch files ----------------------------------- */
        // conf.OutputToConsole = false;
        //
        // foreach (var repoDir in Directory.GetDirectories(@"D:\RE\xiv_game\patch_archive\global"))
        // {
        //     var repo = Path.GetFileName(repoDir);
        //     if (repo == "boot") continue;
        //     
        //     foreach (var patchFile in Directory.GetFiles(repoDir, "*.patch"))
        //     {
        //         Console.WriteLine($"Processing {patchFile}");
        //         var patchFileName = Path.GetFileName(patchFile);
        //         var patchName = Path.GetFileNameWithoutExtension(patchFileName);
        //         conf.OutputFilename = string.Format(OutputDirectory, @"\genpath_patch\", $"{repo}\\{patchName}");
        //         if (File.Exists(conf.OutputFilename))
        //         {
        //             Console.WriteLine("Exists, skipping");
        //             continue;
        //         }
        //                 
        //         try
        //         {
        //             new ReferencedPatchPathFinder2(latest, conf, patchFile).Output();
        //         }
        //         catch (Exception e)
        //         {
        //             Console.WriteLine(e.Message);
        //             Console.WriteLine(e.StackTrace);
        //         }
        //     }
        // }
            
        // One patch
        // var repo = "ex1";
        // var patchFile = "D2015.03.16.0000.0000";
        // conf.OutputFilename = string.Format(OutputDirectory, @"\genpath_patch\", $"{repo}\\{patchFile}.txt");
        // new ReferencedPatchPathFinder2(latest, conf, $@"D:\RE\xiv_game\patch_archive\global\{repo}\{patchFile}.patch").Output();

        /*///////////////////////////////////////////////////////////////////////////////////
        // ---------------------------- Time consuming mining ---------------------------- //
        ///////////////////////////////////////////////////////////////////////////////////*/
        /* ----------------------------------- Search ----------------------------------- */
        /* Est: around 3 mins per search */
        // var terms = new[]{"vfx/monster/m0424/texture/tone005fc.atex"};
        //
        // foreach (var term in terms)
        // {
        //     conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "%VAL%_search");
        //     new AllFileSearcher(latest, conf, term).Output();
        //
        //     conf.OutputFilename = string.Format(OutputDirectory, lastPatch, "%VAL%_search");
        //     new AllFileSearcher(last, conf, term).Output();                
        // }

        /* ----------------------------------- Find full paths referenced in files ----------------------------------- */
        /* Est: around 20 minutes */
        conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "genpaths");
        new ReferencedPathFinder(latest, conf).Output();
        
        /* ----------------------------------- Output a file list of all files in the game ----------------------------------- */
        // /* Est: around 7 minutes */
        // conf.OutputFilename = string.Format(OutputDirectory, latestPatch, "allfiles");
        // new AllPathGenerator(latest, conf).Output();
        
        // conf.OutputFilename = string.Format(OutputDirectory, lastPatch, "allfiles");
        // new AllPathGenerator(last, conf).Output();
    }
}



