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
    public class WeaponGenerator
    {
        public struct Weapon
        {
            public int Id;
            public bool HasSkeleton;
            public List<WeaponBase> Bases;

            public Weapon Diff(Weapon m)
            {
                var diffed = new Weapon {Id = Id, HasSkeleton = HasSkeleton, Bases = new List<WeaponBase>()};

                var thisDict = Bases.ToDictionary(wep => wep.Id, wep => wep);
                var oldDict = m.Bases.ToDictionary(wep => wep.Id, wep => wep);

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
                if (!(obj is Weapon))
                    return false;
                var m = (Weapon) obj;
                return Id == m.Id && HasSkeleton == m.HasSkeleton && Bases.SequenceEqual(m.Bases);
            }
        }

        public struct WeaponBase
        {
            public int Id;
            public bool HasModel;
            public List<WeaponVariant> Variants;

            public WeaponBase Diff(WeaponBase b)
            {
                var diffed = new WeaponBase {Id = Id, HasModel = HasModel, Variants = new List<WeaponVariant>()};
                diffed.Variants = Variants.Except(b.Variants).ToList();
                return diffed;
            }

            public override bool Equals(object? obj)
            {
                if (!(obj is WeaponBase))
                    return false;
                var mb = (WeaponBase) obj;
                return Id == mb.Id && HasModel == mb.HasModel && Variants.SequenceEqual(mb.Variants);
            }
        }

        public struct WeaponVariant
        {
            public int Id;
            public bool IsMaterial;
            public bool IsVfx;
            public bool IsUsed;
            public bool IsEmpty;

            public override bool Equals(object? obj)
            {
                if (!(obj is WeaponVariant))
                    return false;
                var mv = (WeaponVariant) obj;
                return Id == mv.Id && IsMaterial == mv.IsMaterial && IsVfx == mv.IsVfx && IsUsed == mv.IsUsed;
            }
        }

        public struct CWeapon
        {
            public int Id;
            public bool HasSkeleton;
            public ConcurrentBag<CWeaponBase> Bases;

            public Weapon ToWeapon()
            {
                Weapon ret = new Weapon {Id = Id, HasSkeleton = HasSkeleton, Bases = new List<WeaponBase>()};
                while (!Bases.IsEmpty)
                {
                    Bases.TryTake(out var taken);
                    ret.Bases.Add(taken.ToWeaponBase());
                }

                ret.Bases.Sort((v1, v2) => v1.Id.CompareTo(v2.Id));
                return ret;
            }
        }

        public struct CWeaponBase
        {
            public int Id;
            public bool HasModel;
            public ConcurrentBag<WeaponVariant> Variants;

            public WeaponBase ToWeaponBase()
            {
                WeaponBase ret = new WeaponBase {Id = Id, HasModel = HasModel, Variants = new List<WeaponVariant>()};
                while (!Variants.IsEmpty)
                {
                    Variants.TryTake(out var taken);
                    ret.Variants.Add(taken);
                }

                ret.Variants.Sort((v1, v2) => v1.Id.CompareTo(v2.Id));
                return ret;
            }
        }

        private static string _imcFormat = "chara/weapon/w{0:D4}/obj/body/b{1:D4}/b{1:D4}.imc";
        private static string _matFormat = "chara/weapon/w{0:D4}/obj/body/b{1:D4}/material/v{2:D4}/mt_w{0:D4}b{1:D4}_a.mtrl";
        private static string _vfxFormat = "chara/weapon/w{0:D4}/obj/body/b{1:D4}/vfx/eff/vw{2:D4}.avfx";
        private static string _sklFormat = "chara/weapon/w{0:D4}/skeleton/base/b{1:D4}/skl_w{0:D4}b{1:D4}.sklb";
        private static string _mdlFormat = "chara/weapon/w{0:D4}/obj/body/b{1:D4}/model/w{0:D4}b{1:D4}.mdl";

        private List<Weapon> _weapons;
        private List<Quad> _usedList;
        private Lumina.Lumina _lumina;
        private GeneratorConfig _config;

        public WeaponGenerator(Lumina.Lumina lumina, GeneratorConfig config)
        {
            _lumina = lumina;
            _config = config;

            Load();
        }

        private void Load()
        {
            if (_weapons != null) return;

            if (_config.UseSheetsToFindUsed)
                FindUsedWeapons();
            _weapons = _config.BreakOnImcMissing ? FindWeapons() : FindWeaponsConcurrently();
        }

        private List<Weapon> GetComparison(WeaponGenerator mg)
        {
            var newList = new List<Weapon>();
            var thisDict = _weapons.ToDictionary(wep => wep.Id, wep => wep);
            var oldDict = mg._weapons.ToDictionary(wep => wep.Id, wep => wep);

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

        public static void OutputComparison(WeaponGenerator latestPatchGenerator, WeaponGenerator lastPatchGenerator, GeneratorConfig config)
        {
            if (latestPatchGenerator._config.UseSheetsToFindUsed || lastPatchGenerator._config.UseSheetsToFindUsed)
                Console.WriteLine("Comparing patches with sheet data enabled can lead to inaccurate results.");

            var diffwep = latestPatchGenerator.GetComparison(lastPatchGenerator);
            var text = GetOutput(diffwep);

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
            if (_weapons == null)
                Load();

            var text = GetOutput(_weapons);

            if (_config.OutputToConsole)
                text.ForEach(Console.WriteLine);
            if (_config.OutputFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_config.OutputFilename));
                File.WriteAllLines(_config.OutputFilename, text);
            }
        }

        private bool PathExists(PathType type, int weaponId, int baseId = 0, int variantId = 0)
        {
            var path = type switch
            {
                PathType.Imc => string.Format(_imcFormat, weaponId, baseId),
                PathType.Material => string.Format(_matFormat, weaponId, baseId, variantId),
                PathType.Vfx => string.Format(_vfxFormat, weaponId, baseId, variantId),
                PathType.Model => string.Format(_mdlFormat, weaponId, baseId),
                PathType.Skeleton => string.Format(_sklFormat, weaponId, baseId),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            return _lumina.FileExists(path);
        }

        private bool IsWeaponUsed(int WeaponId, int baseId, int variantId)
        {
            return _usedList.Any(_ => _.A == WeaponId && _.B == baseId && _.C == variantId);
        }
        
        private bool IsWeaponUsed(int WeaponId, int baseId)
        {
            return _usedList.Any(_ => _.A == WeaponId && _.B == baseId);
        }

        private void FindUsedWeapons()
        {
            _usedList = new List<Quad>();

            foreach (var row in _lumina.Excel.GetSheet<Item>())
            {
                var slot = row.EquipSlotCategory.Row;
                if (slot == 1 || slot == 2 || slot == 13 || slot == 14)
                {
                    var modelMain = new Quad {Data = row.ModelMain};
                    var modelSub = new Quad {Data = row.ModelSub};
                    if (modelMain.Data != 0)
                        _usedList.Add(modelMain);
                    if (modelSub.Data != 0)
                        _usedList.Add(modelSub);    
                }
            }

            foreach (var row in _lumina.Excel.GetSheet<NpcEquip>())
            {
                _usedList.Add(new Quad {Data = row.ModelMainHand});
                _usedList.Add(new Quad {Data = row.ModelOffHand});
            }
            
            foreach (var row in _lumina.Excel.GetSheet<ENpcBase>())
            {
                _usedList.Add(new Quad {Data = row.ModelMainHand});
                _usedList.Add(new Quad {Data = row.ModelOffHand});
            }
            
            foreach (var row in _lumina.Excel.GetSheet<Carry>())
            {
                _usedList.Add(new Quad {Data = row.Model});
            }
            
            // foreach (var row in _lumina.Excel.GetSheet<QuestEquipModel>()){}
            
            foreach (var row in _lumina.Excel.GetSheet<ENpcDressUpDress>())
            {
                _usedList.Add(new Quad {Data = row.ModelMainHand});
                _usedList.Add(new Quad {Data = row.ModelOffHand});
            }
        }

        private List<Weapon> FindWeapons()
        {
            var weaponList = new List<Weapon>();
            for (int weaponId = 1; weaponId < 10000; weaponId++)
            {
                var thisWeapon = new Weapon {Id = weaponId, Bases = new List<WeaponBase>()};
                thisWeapon.HasSkeleton = PathExists(PathType.Skeleton, weaponId, 1);

                if (!thisWeapon.HasSkeleton)
                    continue;

                for (int baseId = 1; baseId <= 9999; baseId++)
                {
                    var thisBase = new WeaponBase {Id = baseId, Variants = new List<WeaponVariant>()};
                    thisBase.HasModel = PathExists(PathType.Model, weaponId, baseId);

                    if (!thisBase.HasModel)
                        continue;
                    
                    // if there's no model OR imc, we don't want to bother with its variants - probly doesn't exist
                    if (!PathExists(PathType.Imc, weaponId, baseId))
                        break;

                    int maxVariant = _lumina.GetFile<ImcFile>(string.Format(_imcFormat, weaponId, baseId)).Count;
                    
                    for (int variantId = 1; variantId <= maxVariant; variantId++)
                    {
                        var thisVariant = new WeaponVariant {Id = variantId};
                        thisVariant.IsMaterial = PathExists(PathType.Material, weaponId, baseId, variantId);
                        thisVariant.IsVfx = PathExists(PathType.Vfx, weaponId, baseId, variantId);
                        if (_config.UseSheetsToFindUsed)
                            thisVariant.IsUsed = IsWeaponUsed(weaponId, baseId, variantId);
                        thisVariant.IsEmpty = !thisVariant.IsMaterial && !thisVariant.IsVfx;
                        thisBase.Variants.Add(thisVariant);

                        if (!_config.BreakOnImcMissing && _config.SpeedUpNonImcBreak && thisVariant.IsEmpty)
                            break;
                    }
                    if (thisBase.Variants.Count > 0)
                        thisWeapon.Bases.Add(thisBase);
                }
                if (thisWeapon.Bases.Count > 0)
                    weaponList.Add(thisWeapon);
            }
            return weaponList;
        }

        private List<Weapon> FindWeaponsConcurrently()
        {
            ConcurrentBag<CWeapon> weaponBag = new ConcurrentBag<CWeapon>();
            Parallel.For(1, 10000, (weaponId, weaponState) =>
            {
                var thisWeapon = new CWeapon {Id = weaponId, Bases = new ConcurrentBag<CWeaponBase>()};
                thisWeapon.HasSkeleton = PathExists(PathType.Skeleton, weaponId, 1);
                // Console.Write($"{weaponId}, ");
        
                Parallel.For(1, 400, (baseId, baseState) =>
                {
                    var thisBase = new CWeaponBase {Id = baseId, Variants = new ConcurrentBag<WeaponVariant>()};
                    thisBase.HasModel = PathExists(PathType.Model, weaponId, baseId);
        
                    if (_config.SpeedUpNonImcBreak &&
                        !PathExists(PathType.Material, weaponId, baseId, 1) &&
                        !PathExists(PathType.Vfx, weaponId, baseId, 1) &&
                        _config.UseSheetsToFindUsed &&
                        !IsWeaponUsed(weaponId, baseId) &&
                        !thisBase.HasModel)
                        return;
                    
                    Parallel.For(1, 200, (variantId, variantState) =>
                    {
                        var thisVariant = new WeaponVariant {Id = variantId};
                        // Console.WriteLine(_matFormat, weaponId, baseId, variantId);
                        thisVariant.IsMaterial = PathExists(PathType.Material, weaponId, baseId, variantId);
                        thisVariant.IsVfx = PathExists(PathType.Vfx, weaponId, baseId, variantId);
                        if (_config.UseSheetsToFindUsed)
                            thisVariant.IsUsed = IsWeaponUsed(weaponId, baseId, variantId);
                        thisVariant.IsEmpty = !thisVariant.IsMaterial && !thisVariant.IsVfx;
                        
                        if (!thisVariant.IsEmpty)
                            thisBase.Variants.Add(thisVariant);
        
                        // if (_config.SpeedUpNonImcBreak && thisVariant.IsEmpty)
                        //     variantState.Break();
                    });
        
                    if (!thisBase.Variants.IsEmpty || thisBase.HasModel)
                        thisWeapon.Bases.Add(thisBase);
                });
        
                if (!thisWeapon.Bases.IsEmpty || thisWeapon.HasSkeleton)
                    weaponBag.Add(thisWeapon);
                    
            });
        
            var WeaponList = new List<Weapon>();
            while (!weaponBag.IsEmpty)
            {
                weaponBag.TryTake(out var taken);
                WeaponList.Add(taken.ToWeapon());
            }
        
            WeaponList.Sort((v1, v2) => v1.Id.CompareTo(v2.Id));
            return WeaponList;
        }

        private static List<string> GetOutput(List<Weapon> Weapons)
        {
            var lines = new List<string>();
            foreach (var thisWeapon in Weapons)
            {
                lines.Add(string.Format("Weapon {0:D4} | skele {1}", thisWeapon.Id, thisWeapon.HasSkeleton ? 1 : 0));

                foreach (var thisBase in thisWeapon.Bases)
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