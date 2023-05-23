using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumina;
using Lumina.Data.Files;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4;

struct ItemIcon
{
    public int id;
    public string name;
    public bool isNq;
    public bool isNqHr;
    public bool isHq;
    public bool isHqHr;
    public bool used;
}

public class ItemIconFinder
{
    private GameData lumina;
    private GeneratorConfig _config;
    private ExcelSheet<Item> _item;

    private string itemFormat = "ui/icon/{0:D6}/{1:D6}.tex";
    private string itemFormatHr = "ui/icon/{0:D6}/{1:D6}_hr1.tex";
    private string itemFormatHq = "ui/icon/{0:D6}/hq/{1:D6}.tex";
    private string itemFormatHqHr = "ui/icon/{0:D6}/hq/{1:D6}_hr1.tex";

    private const int ITEM_MIN = 20000;
    private const int ITEM_MAX = 58002;

    private Dictionary<ushort, string> iconNames;
    private List<uint> usedIcons;
    private List<ItemIcon> icons;

    public ItemIconFinder(GameData lumina, GeneratorConfig config)
    {
        this.lumina = lumina;
        _config = config;
        icons = new List<ItemIcon>();
        usedIcons = new List<uint>();

        _item = lumina.Excel.GetSheet<Item>();
        iconNames = new Dictionary<ushort, string>();
        foreach (var s in _item)
        {
            iconNames[s.Icon] = s.Name;
        }
    }

    private void GetUsedIcons()
    {
        var itemsheet = lumina.Excel.GetSheet<Item>();

        foreach (var item in itemsheet)
            usedIcons.Add(item.Icon);
    }

    private void GetIcons()
    {
        for (ushort i = ITEM_MIN; i < ITEM_MAX; i++)
        {
            // if (!usedIcons.Contains(i))
            {
                var nq = string.Format(itemFormat, GetFolderVal(i), i);
                var nqhr = string.Format(itemFormatHr, GetFolderVal(i), i);
                var hq = string.Format(itemFormatHq, GetFolderVal(i), i);
                var hqhr = string.Format(itemFormatHqHr, GetFolderVal(i), i);
                iconNames.TryGetValue(i, out var name);

                ItemIcon ic = new ItemIcon
                {
                    id = i,
                    name = name,
                    isNq = lumina.FileExists(nq),
                    isNqHr = lumina.FileExists(nqhr),
                    isHq = lumina.FileExists(hq),
                    isHqHr = lumina.FileExists(hqhr),
                    used = usedIcons.Contains(i)
                };

                if (ic.isNq || ic.isNqHr || ic.isHq || ic.isHqHr)
                {
                    icons.Add(ic);
                }
            }
        }
    }

    private int GetFolderVal(int val)
    {
        int thousands = val / 1000;
        int hundreds = val - (thousands * 1000);
        return val - hundreds;
    }

    private void WriteIconImage(string folder, int code, bool hqOnly)
    {
        var path = string.Format(itemFormatHr, GetFolderVal(code), code);
        if (!lumina.FileExists(path))
            path = string.Format(itemFormat, GetFolderVal(code), code);

        if (hqOnly)
        {
            path = string.Format(itemFormatHqHr, GetFolderVal(code), code);
            if (!lumina.FileExists(path))
                path = string.Format(itemFormatHq, GetFolderVal(code), code);
        }
                
        var outPath = $"{folder}{code}.png";

        var tex = lumina.GetFile<TexFile>(path);
        if (tex == null) return;

        Util.WriteImage(tex, outPath);
    }

    public void Output()
    {
        GetUsedIcons();
        GetIcons();

        var outlines = new List<string>();

        icons.ForEach(e =>
            outlines.Add($"{e.id} | nq {(e.isNq ? 1 : 0)} | hq {(e.isHq ? 1 : 0)} | {e.name}"));

        var outFolder = _config.OutputFilename.Substring(0, _config.OutputFilename.LastIndexOf('\\')) + "/itemicons/";
        if (!Directory.Exists(outFolder))
            Directory.CreateDirectory(outFolder);
        icons.ForEach(e =>
        {
            if (!e.used)
                WriteIconImage(outFolder, e.id, e.isHq && !e.isNq);
        });
        File.WriteAllLines(_config.OutputFilename, outlines);

        if (_config.OutputToConsole)
            outlines.ForEach(Console.WriteLine);
    }
}