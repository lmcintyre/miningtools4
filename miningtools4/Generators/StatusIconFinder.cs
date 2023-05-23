using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumina;
using Lumina.Data.Files;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace miningtools4;

struct Icon
{
    public ushort Id;
    public string Name;
    public bool Hr;
    public bool Used;
}

public class StatusIconFinder
{
    private GameData lumina;
    private ExcelSheet<Status> _status;

    private string iconFormat = "ui/icon/{0:D6}/{1:D6}.tex";
    private string iconFormatHr = "ui/icon/{0:D6}/{1:D6}_hr1.tex";

    private const int STATUS_MIN = 10000;
    private const int STATUS_MAX = 19999;

    private Dictionary<ushort, string> names;
    private List<int> usedIcons;
    private List<Icon> icons;
    private GeneratorConfig _config;

    public StatusIconFinder(GameData lumina, GeneratorConfig conf)
    {
        this.lumina = lumina;
        _config = conf;
        usedIcons = new List<int>();
        icons = new List<Icon>();
            
        _status = lumina.Excel.GetSheet<Status>();
        names = new Dictionary<ushort, string>();
        foreach (var s in _status)
        {
            names[s.Icon] = s.Name;
        }
    }

    private void GetUsedIcons()
    {
        foreach (var s in _status)
            usedIcons.Add(s.Icon);
    }

    private void GetIcons()
    {
        // int min = usedIcons.Min();
        // int max = usedIcons.Max();

        for (ushort i = STATUS_MIN; i < STATUS_MAX; i++)
        {
            // if (!usedIcons.Contains(i))
            {
                var norm = string.Format(iconFormat, GetFolderVal(i), i);
                var hr = string.Format(iconFormatHr, GetFolderVal(i), i);

                bool hasNorm = false, hasHr = false, hasName = false, used = false;

                if (lumina.FileExists(norm))
                    hasNorm = true;
                if (lumina.FileExists(hr))
                    hasHr = true;
                if (names.TryGetValue(i, out var name))
                    hasName = true;
                if (usedIcons.Contains(i))
                    used = true;

                if (hasNorm || hasHr || hasName)
                {
                    Icon ic = new Icon { Id = i, Name = name, Hr = hasHr, Used = used };
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

    private void WriteIconImage(string folder, int code)
    {
        var path = string.Format(iconFormatHr, GetFolderVal(code), code);
        if (!lumina.FileExists(path))
            path = string.Format(iconFormat, GetFolderVal(code), code);

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

        icons.ForEach(e => outlines.Add($"{e.Id} | {e.Name} | {e.Hr} | {e.Used}"));

        var outFolder = _config.OutputFilename.Substring(0, _config.OutputFilename.LastIndexOf('\\')) + "/statusicons/";
        if (!Directory.Exists(outFolder))
            Directory.CreateDirectory(outFolder);
        icons.ForEach(e =>
        {
            // if (!e.Used) 
            WriteIconImage(outFolder, e.Id);
        });
        File.WriteAllLines(_config.OutputFilename, outlines);


        if (_config.OutputToConsole)
            outlines.ForEach(Console.WriteLine);
    }
}