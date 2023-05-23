using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lumina;
using Lumina.Data.Files;
using Lumina.Data.Parsing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using Lumina.Text.Payloads;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace miningtools4;

public class ActResourceGenerator
{
    private ExcelSheet<Action> _actions;
    private ExcelSheet<ContentFinderCondition> _contentFinders;
    // private Dictionary<string, ContentFinderCondition> _contentFinders;
    private ExcelSheet<Map> _maps;
    private ExcelSheet<Status> _status;
    private ExcelSheet<TerritoryType> _territories;

    private const string ActionCategoryFilename = "ActionCategoryList_English";
    private const string ActionListFilename = "ActionList_English";
    private const string ContentFinderListFilename = "ContentFinderList_English";
    private const string MapListFilename = "MapList_English";
    private const string StatusListFilename = "StatusList_English";
    private const string TerritoryListFilename = "TerritoryList_English";

    private bool _loaded = false;

    private GameData _lumina;
    private GeneratorConfig _config;

    public ActResourceGenerator(GameData lumina, GeneratorConfig config)
    {
        _lumina = lumina;
        _config = config;
            
        _actions = _lumina.Excel.GetSheet<Action>();
        _contentFinders = _lumina.Excel.GetSheet<ContentFinderCondition>();
        _maps = _lumina.Excel.GetSheet<Map>();
        _status = _lumina.Excel.GetSheet<Status>();
        _territories = _lumina.Excel.GetSheet<TerritoryType>();
    }

    public void Output()
    {
        Directory.CreateDirectory(_config.OutputFilename);
        WriteActionCategory();
        WriteAction();
        // WriteContentFinder();
        // WriteMaps();
        WriteStatus();
        WriteTerritory();
    }

    private void Write(List<string> lines, string filename)
    {
        File.WriteAllLines(Path.Combine(_config.OutputFilename, $"{filename}.txt"), lines);
    }

    private void WriteActionCategory()
    {
        var lines = new List<string>();
        foreach (var row in _actions)
            lines.Add($"{row.RowId:x}|{row.ActionCategory.Row}");
        Write(lines, ActionCategoryFilename);
    }

    private void WriteAction()
    {
        var lines = new List<string>();
        foreach (var row in _actions)
        {
            if (string.IsNullOrEmpty(row.Name))
                continue;
            lines.Add($"{row.RowId:x}|{row.Name}");
        }
        Write(lines, ActionListFilename);
    }

    private string FixText(SeString text)
    {
        var hyphenBytes = new byte[] { 0x2, 0x1F, 0x1, 0x3 };
        // var emphasisStartBytes = new byte[] { 0x2, 0x1A, 0x2, 0x2, 0x3 };
        // var emphasisEndBytes = new byte[] { 0x2, 0x1A, 0x2, 0x1, 0x3 };
            
        var payloads = text.Payloads;
        string newStr = "";
        foreach (var payload in payloads)
        {
            if (payload is TextPayload txt)
                newStr += txt.RawString;
            else
            {
                if (payload.Data.SequenceEqual(hyphenBytes))
                    newStr += "–";
            }
        }
        return newStr;
    }

    // private void WriteContentFinder()
    // {
    //     var lines = new List<string>();
    //     foreach (var row in _territories)
    //     {
    //         var cfc = row.ContentFinderCondition.Value?.Name;
    //         if (string.IsNullOrEmpty(cfc))
    //         {
    //             if (!_contentFinders.TryGetValue(row.Name.RawString, out var cfc2))
    //                 continue;
    //             cfc = cfc2.Name;
    //         }
    //         var txt = FixText(cfc);
    //         lines.Add($"{row.RowId}|{txt}");
    //     }
    //     Write(lines, ContentFinderListFilename);
    // }

    private void WriteContentFinder()
    {
        var lines = new Dictionary<uint, string>();
        foreach (var row in _contentFinders)
        {
            var cfc = row.Name;
            if (string.IsNullOrEmpty(cfc) || lines.ContainsKey(row.RowId))
                continue;
            var txt = FixText(cfc);
            lines.Add(row.RowId, txt);
        }

        var lines2 = lines.Select(x => $"{x.Key}|{x.Value}").ToList();
        Write(lines2, ContentFinderListFilename);
    }
        
    private void WriteMaps()
    {
        var lines = new List<string>();
        foreach (var row in _maps)
        {
            lines.Add($"{row.RowId:x}|{row.PlaceNameRegion.Value.Name}|{row.PlaceName.Value.Name}|{row.PlaceNameSub.Value.Name}");
        }
        Write(lines, MapListFilename);
    }
        
    private void WriteStatus()
    {
        var lines = new List<string>();
        foreach (var row in _status)
        {
            if (!string.IsNullOrEmpty(row.Name))
                lines.Add($"{row.RowId:x}|{row.Name}");
        }
        Write(lines, StatusListFilename);
    }
        
    private void WriteTerritory()
    {
        var lines = new List<string>();
        foreach (var row in _territories)
        {
            if (!string.IsNullOrEmpty(row.PlaceName.Value.Name))
                lines.Add($"{row.RowId}|{row.PlaceName.Value.Name}");
        }
        Write(lines, TerritoryListFilename);
    }
}