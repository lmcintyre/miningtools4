using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumina.Data.Files;
using Lumina.Data.Parsing;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4
{
    public class EquipmentGenerator
    {
        private static int[] charaKeys = new[] {
            101, // Hyur Male
            104, // Hyur Male (Child)
            201, // Hyur Female
            301, // Highlander Male
            401, // Highlander Female
            501, // Elezen Male
            504, // Elezen Male (Child)
            601, // Elezen Female
            604, // Elezen Female (Child)
            701, // Miqo'te Male
            801, // Miqo'te Female
            804, // Miqo'te Female (Child)
            901, // Roegadyn Male
            1001, // Roegadyn Female
            1101, // Lalafell Male
            1201, // Lalafell Female
            1301, // Au Ra Male
            1401, // Au Ra Female
            1501, // Hrothgar Male
            1801, // Viera Female
            9104, // Padjal Male
            9204, // Padjal Female
        };
        
        private static string[] lsuffix = new[] {
            "met", // head
            "top", // body
            "glv", // hands
            // no waist
            "dwn", // pants
            "sho", // boots
        };

        public struct Equipment
        {
            public int Id;
            public List<EquipmentVariant> Bases;

            public Equipment Diff(Equipment m)
            {
                var diffed = new Equipment {Id = Id, Bases = new List<EquipmentVariant>()};
                diffed.Bases = Bases.Except(m.Bases).ToList();
                return diffed;
            }

            public override bool Equals(object? obj)
            {
                if (!(obj is Equipment))
                    return false;
                var m = (Equipment) obj;
                return Id == m.Id && Bases.SequenceEqual(m.Bases);
            }
        }

        public struct EquipmentVariant
        {
            public int Id;
            public int CharaSkeleId;
            public string Slot;
            public bool HasSkeleton;
            public bool HasVfx;
            public bool HasModel;
            public bool HasMaterial;
            public bool IsUsed;

            public override bool Equals(object? obj)
            {
                if (!(obj is EquipmentVariant))
                    return false;
                var mb = (EquipmentVariant) obj;
                return Id == mb.Id && 
                        CharaSkeleId == mb.CharaSkeleId &&
                        Slot == mb.Slot &&
                        HasSkeleton == mb.HasSkeleton &&
                        IsUsed == mb.IsUsed &&
                        HasModel == mb.HasModel &&
                        HasVfx == mb.HasVfx &&
                        HasMaterial == mb.HasMaterial;
            }

            public bool IsEmpty()
            {
                return !HasSkeleton && !HasVfx && !HasModel && !HasMaterial && !IsUsed;
            }
        }

        private static string _imcFormat = "chara/equipment/e{0:D4}/e{0:D4}.imc";
        private static string _mdlFormat = "chara/equipment/e{0:D4}/model/c{1:D4}e{0:D4}_{2}.mdl";
        private static string _matFormat = "chara/equipment/e{0:D4}/material/v{1:D4}/mt_c{2:D4}e{0:D4}_{3}_a.mtrl";
        private static string _vfxFormat = "chara/equipment/e{0:D4}/vfx/eff/ve{1:D4}.avfx";
        private static string _sklFormat = "chara/human/c{0:D4}/skeleton/{1}/{2}{3:D4}/skl_c{0:D4}{2}{3:D4}.sklb";
        
        private List<Equipment> _equipment;
        private List<Quad> _usedList;
        private Lumina.Lumina _lumina;
        private GeneratorConfig _config;

        public EquipmentGenerator(Lumina.Lumina lumina, GeneratorConfig config)
        {
            _lumina = lumina;
            _config = config;

            Load();
        }

        private void Load()
        {
            if (_equipment != null) return;

            if (_config.UseSheetsToFindUsed)
                FindUsedEquipment();
            _equipment = FindEquipment();
        }

        private List<Equipment> GetComparison(EquipmentGenerator mg)
        {
            var newList = new List<Equipment>();
            var thisDict = _equipment.ToDictionary(wep => wep.Id, wep => wep);
            var oldDict = mg._equipment.ToDictionary(wep => wep.Id, wep => wep);

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

        public static void OutputComparison(EquipmentGenerator latestPatchGenerator, EquipmentGenerator lastPatchGenerator, GeneratorConfig config)
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
            if (_equipment == null)
                Load();

            var text = _config.CondensedOutput ? GetOutputCondensed(_equipment) : GetOutput(_equipment);

            if (_config.OutputToConsole)
                text.ForEach(Console.WriteLine);
            if (_config.OutputFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_config.OutputFilename));
                File.WriteAllLines(_config.OutputFilename, text);
            }
        }
        
        private bool PathExists(PathType type, int equipmentId, int variantId = 0, int charaId = 0, string slot = "")
        {
            var path = type switch
            {
                PathType.Imc => string.Format(_imcFormat, equipmentId),
                PathType.Vfx => string.Format(_vfxFormat, equipmentId, variantId),
                PathType.Model => string.Format(_mdlFormat, equipmentId, charaId, slot),
                PathType.Material => string.Format(_matFormat, equipmentId, variantId, charaId, slot),
                PathType.Skeleton => string.Format(_sklFormat, charaId, slot, slot.Substring(0, 1), equipmentId),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            return _lumina.FileExists(path);
        }

        private static string RaceDescriptionFromSkeleId(int skeleId)
        {
            return skeleId switch
            {
                101 => "Hyur Male",
                104 => "Hyur Male (Child)",
                201 => "Hyur Female",
                301 => "Highlander Male",
                401 => "Highlander Female",
                501 => "Elezen Male",
                504 => "Elezen Male (Child)",
                601 => "Elezen Female",
                604 => "Elezen Female (Child)",
                701 => "Miqo'te Male",
                801 => "Miqo'te Female",
                804 => "Miqo'te Female (Child)",
                901 => "Roegadyn Male",
                1001 => "Roegadyn Female",
                1101 => "Lalafell Male",
                1201 => "Lalafell Female",
                1301 => "Au Ra Male",
                1401 => "Au Ra Female",
                1501 => "Hrothgar Male",
                1801 => "Viera Female",
                9104 => "Padjal Male",
                9204 => "Padjal Female",
            };
        }
        
        private bool IsEquipmentUsed(int equipmentId, int variantId)
        {
            return _usedList.Any(_ => _.A == equipmentId && _.B == variantId);
        }

        private void FindUsedEquipment()
        {
            _usedList = new List<Quad>();

            // foreach (var row in _lumina.Excel.GetSheet<Item>())
            // {
            //     var slot = row.EquipSlotCategory.Row;
            //     if (slot == 1 || slot == 2 || slot == 13 || slot == 14)
            //     {
            //         var modelMain = new Quad {Data = row.ModelMain};
            //         var modelSub = new Quad {Data = row.ModelSub};
            //         if (!modelMain.IsEmpty())
            //             _usedList.Add(modelMain);
            //         if (!modelSub.IsEmpty())
            //             _usedList.Add(modelSub);    
            //     }
            // }
            //
            // foreach (var row in _lumina.Excel.GetSheet<NpcEquip>())
            // {
            //     _usedList.Add(new Quad {Data = row.ModelMainHand});
            //     _usedList.Add(new Quad {Data = row.ModelOffHand});
            // }
            //
            // foreach (var row in _lumina.Excel.GetSheet<ENpcBase>())
            // {
            //     _usedList.Add(new Quad {Data = row.ModelMainHand});
            //     _usedList.Add(new Quad {Data = row.ModelOffHand});
            // }
            //
            // foreach (var row in _lumina.Excel.GetSheet<Carry>())
            // {
            //     _usedList.Add(new Quad {Data = row.Model});
            // }
            //
            // // foreach (var row in _lumina.Excel.GetSheet<QuestEquipModel>()){}
            //
            // foreach (var row in _lumina.Excel.GetSheet<ENpcDressUpDress>())
            // {
            //     _usedList.Add(new Quad {Data = row.ModelMainHand});
            //     _usedList.Add(new Quad {Data = row.ModelOffHand});
            // }
        }

        private List<Equipment> FindEquipment()
        {
            var equipmentList = new List<Equipment>();
            for (int equipmentId = 1; equipmentId < 10000; equipmentId++)
            {
                var thisEquipment = new Equipment {Id = equipmentId, Bases = new List<EquipmentVariant>()};

                if (!PathExists(PathType.Imc, equipmentId, 0))
                    continue;
                int maxVariant = _lumina.GetFile<ImcFile>(string.Format(_imcFormat, equipmentId)).Count;
                
                for (int variantId = 1; variantId <= maxVariant; variantId++)
                {
                    // vfx only depends on variant, so calculate it once for everything
                    var globalVfx = PathExists(PathType.Vfx, equipmentId, variantId);

                    foreach (var skeleId in charaKeys)
                    {
                        foreach (var suffix in lsuffix)
                        {
                            var thisVariant = new EquipmentVariant {Id = variantId, HasVfx = globalVfx, CharaSkeleId = skeleId, Slot = suffix};

                            if (thisVariant.Slot == "met" || thisVariant.Slot == "top")
                                thisVariant.HasSkeleton = PathExists(PathType.Skeleton, equipmentId, variantId, skeleId, suffix);
                            
                            thisVariant.HasModel = PathExists(PathType.Model, equipmentId, variantId, skeleId, suffix);
                            thisVariant.HasMaterial = PathExists(PathType.Material, equipmentId, variantId, skeleId, suffix);

                            if (!thisVariant.IsEmpty())
                                thisEquipment.Bases.Add(thisVariant);
                        }
                    }
                }

                thisEquipment.Bases.Sort((b1, b2) => b1.CharaSkeleId.CompareTo(b2.CharaSkeleId));
                
                if (thisEquipment.Bases.Count > 0)
                    equipmentList.Add(thisEquipment);
            }
            // equipmentList.Sort((e1, e2) => e1.Id.CompareTo(e2.Id));
            return equipmentList;
        }

        private static List<string> GetOutput(List<Equipment> equipment)
        {
            var lines = new List<string>();
            foreach (var thisEquipment in equipment)
            {
                lines.Add(string.Format("Equipment {0:D4}", thisEquipment.Id));

                // no clue
                for (int i = 0; i < 10000; i++)
                {
                    var vars = thisEquipment.Bases.FindAll(v => v.Id == i);
                    if (vars.Count == 0) continue;
                    lines.Add(string.Format("\tVariant {0:D4} | vfx {1}", i, vars[0].HasVfx ? 1 : 0));
                    
                    foreach (var charaSkeleId in charaKeys)
                    {
                        var variantsWithBase = vars.FindAll(b => b.CharaSkeleId == charaSkeleId);
                        if (variantsWithBase.Count == 0) continue;
                        // lines.Add(string.Format("\tVariant {0:D4}", variantsWithBase[0].Id));
                        lines.Add(string.Format("\t\t{0}\t{1}skl\tmdl\tmtrl",
                            RaceDescriptionFromSkeleId(variantsWithBase[0].CharaSkeleId), charaSkeleId == 401 ? "" : "\t"));
                        variantsWithBase.Sort((v1, v2) =>
                        {
                            var findex = lsuffix.Select((v, i) => new {val = v, index = i}).First(v => v.val == v1.Slot).index;
                            var sindex = lsuffix.Select((v, i) => new {val = v, index = i}).First(v => v.val == v2.Slot).index;
                            return findex.CompareTo(sindex);
                        });
                        
                        foreach (var thisVariant in variantsWithBase)
                        {
                            // lines.Add(string.Format("\t\t\t{0}", thisVariant.Slot));
                            lines.Add(string.Format("\t\t\t{0}\t\t\t\t{1}\t{2}\t{3}",
                                                        thisVariant.Slot,
                                                        thisVariant.HasSkeleton ? 1 : 0,
                                                        thisVariant.HasModel ? 1 : 0,
                                                        thisVariant.HasMaterial ? 1 : 0));
                        }    
                    }
                }
            }
            return lines;
        }

        private static List<string> GetOutputCondensed(List<Equipment> equipment)
        {
            var lines = new List<string>();
            foreach (var thisEquipment in equipment)
            {
                lines.Add(string.Format("Equipment {0:D4}", thisEquipment.Id));

                // no clue
                for (int i = 0; i < 10000; i++)
                {
                    var vars = thisEquipment.Bases.FindAll(v => v.Id == i);
                    if (vars.Count == 0) continue;
                    lines.Add(string.Format("\tVariant {0:D4}", i));
                }
            }
            return lines;
        }
    }
}