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
            public bool HasSkeleton;
            public List<MonsterBase> Bases;

            public Monster Diff(Monster m)
            {
                var diffed = new Monster {Id = Id, HasSkeleton = HasSkeleton, Bases = new List<MonsterBase>()};

                var thisDict = Bases.ToDictionary(mon => mon.Id, mon => mon);
                var oldDict = m.Bases.ToDictionary(mon => mon.Id, mon => mon);

                foreach (var key in thisDict.Keys)
                {
                    thisDict.TryGetValue(key, out var thisValue);
                    if (!oldDict.TryGetValue(key, out var oldValue))
                    {
                        diffed.Bases.Add(thisValue);
                        continue;
                    }
                    var result = thisValue.Diff(oldValue);
                    if (result.Variants.Count > 0)
                        diffed.Bases.Add(result);
                }
                return diffed;
            }

            public override bool Equals(object? obj)
            {
                if (!(obj is Monster))
                    return false;
                var m = (Monster) obj;
                return Id == m.Id && HasSkeleton == m.HasSkeleton && Bases.SequenceEqual(m.Bases);
            }
        }

        public struct MonsterBase
        {
            public int Id;
            public bool HasModel;
            public List<MonsterVariant> Variants;

            public MonsterBase Diff(MonsterBase b)
            {
                var diffed = new MonsterBase {Id = Id, HasModel = HasModel, Variants = new List<MonsterVariant>()};
                diffed.Variants = Variants.Except(b.Variants).ToList();
                return diffed;
            }

            public override bool Equals(object? obj)
            {
                if (!(obj is MonsterBase))
                    return false;
                var mb = (MonsterBase) obj;
                return Id == mb.Id && HasModel == mb.HasModel && Variants.SequenceEqual(mb.Variants);
            }
        }

        public struct MonsterVariant
        {
            public int Id;
            public bool IsMaterial;
            public bool IsVfx;
            public bool IsUsed;
            public bool IsEmpty;

            public override bool Equals(object? obj)
            {
                if (!(obj is MonsterVariant))
                    return false;
                var mv = (MonsterVariant) obj;
                return Id == mv.Id && IsMaterial == mv.IsMaterial && IsVfx == mv.IsVfx && IsUsed == mv.IsUsed;
            }
        }

        public struct CMonster
        {
            public int Id;
            public bool HasSkeleton;
            public ConcurrentBag<CMonsterBase> Bases;

            public Monster ToMonster()
            {
                Monster ret = new Monster {Id = Id, HasSkeleton = HasSkeleton, Bases = new List<MonsterBase>()};
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
            public bool HasModel;
            public ConcurrentBag<MonsterVariant> Variants;

            public MonsterBase ToMonsterBase()
            {
                MonsterBase ret = new MonsterBase {Id = Id, HasModel = HasModel, Variants = new List<MonsterVariant>()};
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
        private static string _sklFormat = "chara/monster/m{0:D4}/skeleton/base/b{1:D4}/skl_m{0:D4}b{1:D4}.sklb";
        private static string _mdlFormat = "chara/monster/m{0:D4}/obj/body/b{1:D4}/model/m{0:D4}b{1:D4}.mdl";
        // private static string anusmongle = "chara/monster/m0008/skeleton/base/b0001/skl_m0008b0001.sklb";

        private List<Monster> _monsters;
        private List<Quad> _usedList;
        private Lumina.Lumina _lumina;
        private GeneratorConfig _config;

        public MonsterGenerator(Lumina.Lumina lumina, GeneratorConfig config)
        {
            _lumina = lumina;
            _config = config;

            Load();
        }

        private void Load()
        {
            if (_monsters != null) return;

            if (_config.UseSheetsToFindUsed)
                FindUsedMonsters();
            _monsters = _config.BreakOnImcMissing ? FindMonsters() : FindMonstersConcurrently();
        }

        private List<Monster> GetComparison(MonsterGenerator mg)
        {
            var newList = new List<Monster>();
            var thisDict = _monsters.ToDictionary(mon => mon.Id, mon => mon);
            var oldDict = mg._monsters.ToDictionary(mon => mon.Id, mon => mon);

            foreach (var key in thisDict.Keys)
            {
                thisDict.TryGetValue(key, out var thisValue);
                if (!oldDict.TryGetValue(key, out var oldValue))
                {
                    newList.Add(thisValue);
                    continue;
                }
                var result = thisValue.Diff(oldValue);
                if (result.Bases.Count > 0)
                    newList.Add(result);
            }
            return newList;
        }

        public static void OutputComparison(MonsterGenerator latestPatchGenerator, MonsterGenerator lastPatchGenerator, GeneratorConfig config)
        {
            if (latestPatchGenerator._config.UseSheetsToFindUsed || lastPatchGenerator._config.UseSheetsToFindUsed)
                Console.WriteLine("Comparing patches with sheet data enabled can lead to inaccurate results.");

            var diffMon = latestPatchGenerator.GetComparison(lastPatchGenerator);
            var text = GetOutput(diffMon);

            if (config.OutputToConsole)
                text.ForEach(Console.WriteLine);
            if (config.OutputFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(config.OutputFilename));
                File.WriteAllLines(config.OutputFilename, text);
            }
        }

        public void Output()
        {
            if (_monsters == null)
                Load();

            var text = GetOutput(_monsters);

            if (_config.OutputToConsole)
                text.ForEach(Console.WriteLine);
            if (_config.OutputFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_config.OutputFilename));
                File.WriteAllLines(_config.OutputFilename, text);
            }
        }

        private bool PathExists(PathType type, int monsterId, int baseId = 0, int variantId = 0)
        {
            var path = type switch
            {
                PathType.Imc => string.Format(_imcFormat, monsterId, baseId),
                PathType.Material => string.Format(_matFormat, monsterId, baseId, variantId),
                PathType.Vfx => string.Format(_vfxFormat, monsterId, baseId, variantId),
                PathType.Model => string.Format(_mdlFormat, monsterId, baseId),
                PathType.Skeleton => string.Format(_sklFormat, monsterId, baseId),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            return _lumina.FileExists(path);
        }

        private bool IsMonsterUsed(int monsterId, int baseId, int variantId)
        {
            return _usedList.Any(_ => _.A == monsterId && _.B == baseId && _.C == variantId);
        }
        
        private bool IsMonsterUsed(int monsterId, int baseId)
        {
            return _usedList.Any(_ => _.A == monsterId && _.B == baseId);
        }

        private void FindUsedMonsters()
        {
            _usedList = new List<Quad>();

            foreach (var row in _lumina.Excel.GetSheet<ModelChara>())
            {
                Quad modelChara = new Quad();
                modelChara.U16A = row.Model;
                modelChara.U16B = row.Base;
                modelChara.U16C = row.Variant;
                _usedList.Add(modelChara);
            }
        }

        private List<Monster> FindMonsters()
        {
            var monsterList = new List<Monster>();
            for (int monsterId = 1; monsterId < 10000; monsterId++)
            {
                var thisMonster = new Monster {Id = monsterId, Bases = new List<MonsterBase>()};
                thisMonster.HasSkeleton = PathExists(PathType.Skeleton, monsterId, 1);

                // if (!thisMonster.HasSkeleton)
                //     continue;
                
                for (int baseId = 1; baseId <= 9999; baseId++)
                {
                    var thisBase = new MonsterBase {Id = baseId, Variants = new List<MonsterVariant>()};
                    thisBase.HasModel = PathExists(PathType.Model, monsterId, baseId);

                    if (!thisBase.HasModel)
                        continue;
                    
                    if (!PathExists(PathType.Imc, monsterId, baseId))
                        break;

                    int maxVariant = _lumina.GetFile<ImcFile>(string.Format(_imcFormat, monsterId, baseId)).Count;
                    
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

                    if (thisBase.Variants.Count > 0 || thisBase.HasModel)
                        thisMonster.Bases.Add(thisBase);
                }

                if (thisMonster.Bases.Count > 0 || thisMonster.HasSkeleton)
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
                thisMonster.HasSkeleton = PathExists(PathType.Skeleton, monsterId, 1);

                Parallel.For(1, 50, (baseId, baseState) =>
                {
                    var thisBase = new CMonsterBase {Id = baseId, Variants = new ConcurrentBag<MonsterVariant>()};
                    thisBase.HasModel = PathExists(PathType.Model, monsterId, baseId);
        
                    if (_config.SpeedUpNonImcBreak &&
                        !PathExists(PathType.Material, monsterId, baseId, 1) &&
                        !PathExists(PathType.Vfx, monsterId, baseId, 1) &&
                        _config.UseSheetsToFindUsed &&
                        !IsMonsterUsed(monsterId, baseId) &&
                        !thisBase.HasModel)
                        return;
                    
                    Parallel.For(1, 50, (variantId, variantState) =>
                    {
                        var thisVariant = new MonsterVariant {Id = variantId};
                        thisVariant.IsMaterial = PathExists(PathType.Material, monsterId, baseId, variantId);
                        thisVariant.IsVfx = PathExists(PathType.Vfx, monsterId, baseId, variantId);
                        if (_config.UseSheetsToFindUsed)
                            thisVariant.IsUsed = IsMonsterUsed(monsterId, baseId, variantId);
                        thisVariant.IsEmpty = !thisVariant.IsMaterial && !thisVariant.IsVfx;
                        
                        if (!thisVariant.IsEmpty)
                            thisBase.Variants.Add(thisVariant);
        
                        // if (_config.SpeedUpNonImcBreak && thisVariant.IsEmpty)
                        //     variantState.Break();
                    });
        
                    if (!thisBase.Variants.IsEmpty || thisBase.HasModel)
                        thisMonster.Bases.Add(thisBase);
                });
        
                if (!thisMonster.Bases.IsEmpty || thisMonster.HasSkeleton)
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

        private static List<string> GetOutput(List<Monster> monsters)
        {
            var lines = new List<string>();
            foreach (var thisMonster in monsters)
            {
                lines.Add(string.Format("Monster {0:D4} | skele {1}", thisMonster.Id, thisMonster.HasSkeleton ? 1 : 0));

                foreach (var thisBase in thisMonster.Bases)
                {
                    lines.Add(string.Format("\tBase {0:D4} | model {1}", thisBase.Id, thisBase.HasModel ? 1 : 0));

                    foreach (var thisVariant in thisBase.Variants)
                    {
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