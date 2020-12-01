using System;
using Lumina;
using Lumina.Data.Parsing;

namespace miningtools4
{
    class Program
    {
        private static string GameDirectory = @"F:\RE\xiv_game\{0}\game\sqpack";
        private static string OutputDirectory = @"C:\Users\Liam\Desktop\miningtools4\{0}\{1}.txt";
        
        static void Main(string[] args)
        {
            string patch = "5.35";

            LuminaOptions opt = new LuminaOptions();
            opt.PanicOnSheetChecksumMismatch = false;
            Lumina.Lumina lumina = new Lumina.Lumina(string.Format(GameDirectory, patch), opt);

            var conf = new GeneratorConfig {OutputToConsole = true, UseConcurrency = true, BreakOnImcMissing = true, UseSheetsToFindUsed = true};
            conf.OutputFile = true;
            conf.OutputDirectory = string.Format(OutputDirectory, patch, "monsters");
            var mg = new MonsterGenerator(lumina, conf);
        }
    }
}