using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4
{
    public class MiscSheetDataGenerator
    {
        enum BaseParam
        {
            Strength = 1,
            Dexterity,
            Vitality,
            Intelligence,
            Mind,
            Piety,
            Tenacity = 19,
            DirectHit = 22,
            CriticalHit = 27,
            Determination = 44,
            SkillSpeed,
            SpellSpeed,
            Craftsmanship = 70,
            Control,
            Gathering,
            Perception
        }

        enum CraftType
        {
            Woodworking,
            Smithing,
            Armorcraft,
            Goldsmithing,
            Leatherworking,
            Clothcraft,
            Alchemy,
            Cooking
        }
        
        private const string ItemHeader =
            "Item Id,Type,Name,Description";

        private const string EquipHeader =
            "Item Id,Name,iLvl,Class,Weapon Damage (Phys),Weapon Damage (Mag),Delay (ms)," +
            "Strength,Dexterity,Vitality,Intelligence,Mind,Piety,Tenacity,Direct Hit,Critical Hit," +
            "Determination,Skill Speed,Spell Speed,Craftsmanship,Control,Gathering,Perception";

        private const string RecipeHeader =
            "Craft Type,Level Req,Stars,Required Craft.,Required Control,Difficulty,Quality,Durability,Can Be HQ,Result,Result Amount," +
            "Ingredient 0,Amount 0,Ingredient 1,Amount 1,Ingredient 2,Amount 2,Ingredient 3,Amount 3,Ingredient 4,Amount 4," +
            "Ingredient 5,Amount 5,Ingredient 6,Amount 6,Ingredient 7,Amount 7,Ingredient 8,Amount 8,Ingredient 9,Amount 9," +
            "Master Book,Is Specialist";

        private static string _itemSheetFilename = "items.csv";
        private static string _equipSheetFilename = "equipment.csv";
        private static string _recipeSheetFilename = "recipes.csv";

        private Lumina.Lumina _lumina;
        private GeneratorConfig _config;

        private List<string> _itemsOut;
        private List<string> _equipmentOut;
        private List<string> _recipesOut;
        
        private static Dictionary<uint, string> _itemUICategoryDict;
        private static Dictionary<uint, string> _secretRecipeBookDict;
        private static Dictionary<uint, string> _itemNames;
        private static Dictionary<uint, RecipeLevelTable> _recipeLevels;

        public MiscSheetDataGenerator(Lumina.Lumina lumina, GeneratorConfig config)
        {
            _lumina = lumina;
            _config = config;

            Load();
        }
        
        private void Load()
        {
            LoadItemUICategory();
            LoadSecretRecipeBook();
            LoadItemNames();
            LoadRecipeLevels();

            _itemsOut = Items(_lumina).Prepend(ItemHeader).ToList();
            _equipmentOut = Equipment(_lumina).Prepend(EquipHeader).ToList();
            _recipesOut = Recipes(_lumina).Prepend(RecipeHeader).ToList();
        }

        public void Output()
        {
            if (_config.OutputToConsole)
            {
                _itemsOut.ForEach(Console.WriteLine);
                _equipmentOut.ForEach(Console.WriteLine);
                _recipesOut.ForEach(Console.WriteLine);
            }
                
            if (_config.OutputFile)
            {
                Directory.CreateDirectory(_config.OutputFilename);

                var itemFileOut = Path.Join(_config.OutputFilename, _itemSheetFilename);
                var equipFileOut = Path.Join(_config.OutputFilename, _equipSheetFilename);
                var recipeFileOut = Path.Join(_config.OutputFilename, _recipeSheetFilename);
                
                File.WriteAllLines(itemFileOut, _itemsOut);
                File.WriteAllLines(equipFileOut, _equipmentOut);
                File.WriteAllLines(recipeFileOut, _recipesOut);
            }
        }

        private void LoadRecipeLevels()
        {
            var rlevels = _lumina.GetExcelSheet<RecipeLevelTable>();
            _recipeLevels = new Dictionary<uint, RecipeLevelTable>();

            foreach (var rlevel in rlevels)
                _recipeLevels[rlevel.RowId] = rlevel;
        }

        private void LoadItemUICategory()
        {
            var uicat = _lumina.GetExcelSheet<ItemUICategory>();
            _itemUICategoryDict = new Dictionary<uint, string>();

            foreach (var cat in uicat)
                _itemUICategoryDict[cat.RowId] = SanitizeText(cat.Name);
        }

        private void LoadSecretRecipeBook()
        {
            var srb = _lumina.GetExcelSheet<SecretRecipeBook>();
            _secretRecipeBookDict = new Dictionary<uint, string>();

            foreach (var b in srb)
                _secretRecipeBookDict[b.RowId] = SanitizeText(b.Name);
        }

        private void LoadItemNames()
        {
            var items = _lumina.GetExcelSheet<Item>();
            _itemNames = new Dictionary<uint, string>();

            foreach (var item in items)
                _itemNames[item.RowId] = SanitizeText(item.Name);
        }

        private List<string> Equipment(Lumina.Lumina lumina)
        {
            var equipment = lumina.GetExcelSheet<Item>();
            var equipList = new List<string>();

            foreach (var row in equipment)
            {
                if (row.EquipSlotCategory.Row == 0)
                    continue;
                var sanitizedName = SanitizeText(row.Name);
                var ilvl = row.LevelItem.Row;
                var clazz = _itemUICategoryDict[row.ItemUICategory.Row];
                var physWep = row.DamagePhys;
                var magWep = row.DamageMag;
                var delay = row.Delayms;

                var paramString = MakeParamsString(row);

                if (!string.IsNullOrEmpty(sanitizedName))
                    equipList.Add(
                        MakeCsvRow(
                            row.RowId.ToString(),
                            sanitizedName,
                            ilvl.ToString(),
                            clazz,
                            physWep.ToString(),
                            magWep.ToString(),
                            delay.ToString(),
                            paramString
                        ));
            }

            return equipList;
        }

        private string MakeParamsString(Item item)
        {
            string ret = "";

            foreach (BaseParam possibleParam in Enum.GetValues(typeof(BaseParam)))
            {
                int thisParamValue = 0;
                
                for (int i = 0; i < item.UnkStruct60.Length; i++)
                {
                    int param = item.UnkStruct60[i].BaseParam;
                    int value = item.UnkStruct60[i].BaseParamValue;

                    // if (IsInRange(param))
                    if (possibleParam == (BaseParam) param)
                        thisParamValue = value;
                }

                if (thisParamValue != 0)
                    ret += thisParamValue.ToString();
                ret += ",";
            }

            return ret.Substring(0, ret.Length - 1);
        }

        private List<string> Items(Lumina.Lumina lumina)
        {
            var items = lumina.GetExcelSheet<Item>();
            var itemsList = new List<string>();

            foreach (var item in items)
            {
                if (item.EquipSlotCategory.Row != 0)
                    continue;

                var type = _itemUICategoryDict[item.ItemUICategory.Row];
                var sanitizedName = SanitizeText(item.Name);
                var sanitizedDesc = SanitizeText(item.Description);

                if (!string.IsNullOrEmpty(sanitizedName))
                    itemsList.Add(MakeCsvRow(item.RowId.ToString(), type, sanitizedName, "\"" + sanitizedDesc + "\""));
            }

            return itemsList;
        }

        private List<string> Recipes(Lumina.Lumina lumina)
        {
            var recipes = lumina.GetExcelSheet<Recipe>();
            var recipeList = new List<string>();

            foreach (var recipe in recipes)
            {
                // first recipe is garbo
                if (recipe.RowId == 0)
                    continue;

                RecipeLevelTable level = _recipeLevels[recipe.RecipeLevelTable.Row];
                var craftType = recipe.CraftType.Value.Name.ToString();

                var canBeHq = recipe.CanHq ? "Yes" : "No";
                var reqLevel = level.ClassJobLevel;
                var stars = level.Stars;
                var reqCraft = recipe.RequiredCraftsmanship.ToString();
                var reqControl = recipe.RequiredControl.ToString();
                var difficulty = level.Difficulty * (recipe.DifficultyFactor / 100);
                var quality = level.Quality * (recipe.QualityFactor / 100);
                var dura = level.Durability;

                var result = _itemNames[recipe.ItemResult.Row];
                var resultAmt = recipe.AmountResult;

                string ingredience = "";
                for (int i = 0; i < recipe.UnkStruct5.Length; i++)
                {
                    ingredience +=
                        _itemNames.GetValueOrDefault((uint) recipe.UnkStruct5[i].ItemIngredient, "") + "," +
                        recipe.UnkStruct5[i].AmountIngredient + ",";
                }

                ingredience = ingredience.Substring(0, ingredience.Length - 1);

                var masterbook = _secretRecipeBookDict[recipe.SecretRecipeBook.Row];
                var specialist = recipe.IsSpecializationRequired ? "Yes" : "No";

                recipeList.Add(MakeCsvRow(
                    craftType, reqLevel.ToString(), stars.ToString(), reqCraft, reqControl,
                    difficulty.ToString(), quality.ToString(), dura.ToString(), canBeHq,
                    result, resultAmt.ToString(), ingredience, masterbook, specialist
                ));
            }

            // "CraftType,Result,ResultAmount," +
            //     "Ingredient0,Amount0,Ingredient1,Amount1,Ingredient2,Amount2,Ingredient3,Amount3,Ingredient4,Amount4," +
            //     "Ingredient5,Amount5,Ingredient6,Amount6,Ingredient7,Amount7,Ingredient8,Amount8,Ingredient9,Amount9,Ingredient10,Amount10," +
            //     "RequiredCraftsmanship,RequiredControl,MasterBookIfApplicable,IsSpecialistRecipe";

            return recipeList;
        }
        
        private string MakeCsvRow(params string[] values)
        {
            return values.Aggregate("", (current, val) => current + val + ",");
        }

        private string SanitizeText(string text)
        {
            string newText = Regex.Replace(text, @"[^\u0020-\u007F]+", string.Empty);

            newText = newText.Replace("HI", " ");
            newText = newText.Replace("IH", " ");

            return newText;
        }
    }
}