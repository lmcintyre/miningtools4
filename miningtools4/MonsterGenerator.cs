using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lumina.Data.Files;
using Lumina.Data.Parsing;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4
{
    public class MonsterGenerator
    {
        public struct Monster
        {
            public int Id;
            public List<MonsterBase> Bases;
        }

        public struct MonsterBase
        {
            public int Id;
            public List<MonsterVariant> Variants;
        }

        public struct MonsterVariant
        {
            public int Id;
            public bool IsMaterial;
            public bool IsVfx;
            public bool IsUsed;
            public bool IsEmpty;
        }

        public struct CMonster
        {
            public int Id;
            public ConcurrentBag<CMonsterBase> Bases;

            public Monster ToMonster()
            {
                Monster ret = new Monster {Id = Id, Bases = new List<MonsterBase>()};
                while (!Bases.IsEmpty)
                {
                    Bases.TryTake(out var taken);
                    ret.Bases.Add(taken.ToMonsterBase());
                }
                ret.Bases.Sort((v1, v2) => v1.Id.CompareTo(v2.Id));
                return ret;
            }
        }

        public struct CMonsterBase
        {
            public int Id;
            public ConcurrentBag<MonsterVariant> Variants;

            public MonsterBase ToMonsterBase()
            {
                MonsterBase ret = new MonsterBase {Id = Id, Variants = new List<MonsterVariant>()};
                while (!Variants.IsEmpty)
                {
                    Variants.TryTake(out var taken);
                    ret.Variants.Add(taken);
                }
                ret.Variants.Sort((v1, v2) => v1.Id.CompareTo(v2.Id));
                return ret;
            }
        }

        private static string _imcFormat = "chara/monster/m{0:D4}/obj/body/b{1:D4}/b{1:D4}.imc";
        private static string _matFormat = "chara/monster/m{0:D4}/obj/body/b{1:D4}/material/v{2:D4}/mt_m{0:D4}b{1:D4}_a.mtrl";
        private static string _vfxFormat = "chara/monster/m{0:D4}/obj/body/b{1:D4}/vfx/eff/vm{2:D4}.avfx";

        private List<Monster> _monsters;
        private List<Quad> _usedList;
        private Lumina.Lumina _lumina;
        private GeneratorConfig _config;

        public MonsterGenerator(Lumina.Lumina lumina, GeneratorConfig config)
        {
            _lumina = lumina;
            _config = config;
            if (_config.UseSheetsToFindUsed)
                FindUsedMonsters();

            _monsters = _config.UseConcurrency ? FindMonstersConcurrently() : FindMonsters();
            var text = GetOutput();
            
            if (_config.OutputToConsole)
                foreach (var line in text)
                    Console.WriteLine(line);
            if (_config.OutputFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_config.OutputDirectory));
                File.WriteAllLines(_config.OutputDirectory, text);
            }
        }

        private bool PathExists(PathType type, int monsterId, int baseId = 0, int variantId = 0)
        {
            var path = type switch
            {
                PathType.Imc => string.Format(_imcFormat, monsterId, baseId),
                PathType.Material => string.Format(_matFormat, monsterId, baseId, variantId),
                PathType.Vfx => string.Format(_vfxFormat, monsterId, baseId, variantId),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            return _lumina.FileExists(path);
        }

        private bool IsMonsterUsed(int monsterId, int baseId, int variantId)
        {
            return _usedList.Any(_ => _.A == monsterId && _.B == baseId && _.C == variantId);
        }

        private void FindUsedMonsters()
        {
            _usedList = new List<Quad>();

            foreach (var row in _lumina.Excel.GetSheet<ModelChara>())
            {
                Quad modelChara = new Quad();
                modelChara.SetA(row.Model);
                modelChara.SetB(row.Base);
                modelChara.SetC(row.Variant);
                _usedList.Add(modelChara);
            }
        }

        private List<Monster> FindMonsters()
        {
            var monsterList = new List<Monster>();
            for (int monsterId = 1; monsterId < 10000; monsterId++)
            {
                var thisMonster = new Monster {Id = monsterId, Bases = new List<MonsterBase>()};

                for (int baseId = 1; baseId <= 9999; baseId++)
                {
                    // ignore this monster entirely if the imc doesn't exist based on config
                    if (_config.BreakOnImcMissing && !PathExists(PathType.Imc, monsterId, baseId))
                        break;
                    
                    int maxVariant;
                    if (!_config.BreakOnImcMissing)
                        maxVariant = 9999;
                    else
                        maxVariant = _lumina.GetFile<ImcFile>(string.Format(_imcFormat, monsterId, baseId)).Count;
                    
                    var thisBase = new MonsterBase {Id = baseId, Variants = new List<MonsterVariant>()};
                    for (int variantId = 1; variantId <= maxVariant; variantId++)
                    {
                        var thisVariant = new MonsterVariant {Id = variantId};
                        thisVariant.IsMaterial = PathExists(PathType.Material, monsterId, baseId, variantId);
                        thisVariant.IsVfx = PathExists(PathType.Vfx, monsterId, baseId, variantId);
                        if (_config.UseSheetsToFindUsed)
                            thisVariant.IsUsed = IsMonsterUsed(monsterId, baseId, variantId);
                        thisVariant.IsEmpty = !thisVariant.IsMaterial && !thisVariant.IsVfx;
                        thisBase.Variants.Add(thisVariant);
                    }

                    if (thisBase.Variants.Count > 0)
                        thisMonster.Bases.Add(thisBase);
                }

                if (thisMonster.Bases.Count > 0)
                    monsterList.Add(thisMonster);
            }

            return monsterList;
        }

        private List<Monster> FindMonstersConcurrently()
        {
            ConcurrentBag<CMonster> monsterBag = new ConcurrentBag<CMonster>();
            Parallel.For(1, 10000, (monsterId, monsterState) =>
            {
                var thisMonster = new CMonster {Id = monsterId, Bases = new ConcurrentBag<CMonsterBase>()};

                int maxBase = 1;
                if (!_config.BreakOnImcMissing)
                    maxBase = 10000;
                else
                {
                    for (int possibleBase = 1; possibleBase < 10000; possibleBase++)
                    {
                        if (!PathExists(PathType.Imc, monsterId, possibleBase))
                        {
                            // we don't have to add one here because it's exclusive and
                            // we already incremented to find the path we broke at
                            maxBase = possibleBase;
                            break;
                        }
                    }
                }
                
                Parallel.For(1, maxBase, (baseId, baseState) =>
                {
                    int maxVariant;
                    if (!_config.BreakOnImcMissing)
                        maxVariant = 10000;
                    else
                        maxVariant = _lumina.GetFile<ImcFile>(string.Format(_imcFormat, monsterId, baseId)).Count;

                    var thisBase = new CMonsterBase {Id = baseId, Variants = new ConcurrentBag<MonsterVariant>()};
                    Parallel.For(1, maxVariant + 1, (variantId, variantState) =>
                    {
                        var thisVariant = new MonsterVariant {Id = variantId};
                        thisVariant.IsMaterial = PathExists(PathType.Material, monsterId, baseId, variantId);
                        thisVariant.IsVfx = PathExists(PathType.Vfx, monsterId, baseId, variantId);
                        if (_config.UseSheetsToFindUsed)
                            thisVariant.IsUsed = IsMonsterUsed(monsterId, baseId, variantId);
                        thisVariant.IsEmpty = !thisVariant.IsMaterial && !thisVariant.IsVfx;
                        
                        thisBase.Variants.Add(thisVariant);
                    });
                    
                    if (!thisBase.Variants.IsEmpty)
                        thisMonster.Bases.Add(thisBase);
                });
                
                if (!thisMonster.Bases.IsEmpty)
                    monsterBag.Add(thisMonster);
            });
            
            var monsterList = new List<Monster>();
            while (!monsterBag.IsEmpty)
            {
                monsterBag.TryTake(out var taken);
                monsterList.Add(taken.ToMonster());
            }
            monsterList.Sort((v1, v2) => v1.Id.CompareTo(v2.Id));
            return monsterList;
        }

        private List<string> GetOutput()
        {
            var lines = new List<string>();
            foreach (var thisMonster in _monsters) {
                lines.Add(string.Format("Monster {0:D4}", thisMonster.Id));

                foreach (var thisBase in thisMonster.Bases) {
                    lines.Add(string.Format("\tBase {0:D4}", thisBase.Id));

                    foreach (var thisVariant in thisBase.Variants) {
                        lines.Add(string.Format("\t\tVariant {0:D4} | mat {1} | vfx {2} | empty {3} | used {4}",
                            thisVariant.Id,
                            thisVariant.IsMaterial ? 1 : 0,
                            thisVariant.IsVfx ? 1 : 0,
                            thisVariant.IsEmpty ? 1 : 0,
                            thisVariant.IsUsed ? 1 : 0));
                    }
                }
            }
            return lines;
        }
    }
}