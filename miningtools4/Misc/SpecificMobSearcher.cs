using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumina;

namespace miningtools4;

public class SpecificMobSearcher
{
	private const int MaxA = 150;
	private const int MaxSp = 50;

	private static readonly string[] _animationFormats =
	{
		// "chara/monster/m{0:D4}/animation/a{1:D4}/bt_common/resident/monster.pap",
		// "chara/monster/m{0:D4}/animation/a{1:D4}/bt_common/resident/mount.pap",
		"chara/monster/m{0:D4}/animation/a{1:D4}/bt_common/mon_sp/m{0:D4}/mon_sp{2:D3}.pap",
		"chara/monster/m{0:D4}/animation/a{1:D4}/bt_common/mon_sp/m{0:D4}/gc_sp{2:D3}.pap",
		"chara/monster/m{0:D4}/animation/a{1:D4}/bt_common/mon_sp/m{0:D4}/show/mon_sp{2:D3}.pap",
		"chara/monster/m{0:D4}/animation/a{1:D4}/bt_common/mon_sp/m{0:D4}/hide/mon_sp{2:D3}.pap",
		"chara/demihuman/d{0:D4}/animation/a{1:D4}/bt_common/mon_sp/d{0:D4}/hide/mon_sp{2:D3}.pap",
		"chara/demihuman/d{0:D4}/animation/a{1:D4}/bt_common/mon_sp/d{0:D4}/mon_sp{2:D3}.pap",
		"chara/demihuman/d{0:D4}/animation/a{1:D4}/bt_common/mon_sp/d{0:D4}/show/mon_sp{2:D3}.pap",
		// "chara/monster/m{0:D4}/animation/a{1:D4}/bt_common/mount_sp/m{0:D4}/mon_sp{2:D3}.pap"
	};
	
	private GameData _lumina;
	private GeneratorConfig _config;

	private List<string> _lines;
	private uint[] _mobs;

	public SpecificMobSearcher(GameData lumina, GeneratorConfig config, uint[] mobs)
	{
		_lumina = lumina;
		_config = config;
		_mobs = mobs;

		_lines = Load();
	}

	public void Output()
	{
		if (_lines == null) Load();

		if (_config.OutputToConsole)
			_lines.ForEach(Console.WriteLine);
		if (_config.OutputPath)
		{
			Directory.CreateDirectory(_config.OutputFilename);
			var fileName = Path.Combine(_config.OutputFilename, "specificmob_" + string.Join("_", _mobs) + ".txt");
			File.WriteAllLines(fileName, _lines);
		}
	}

	private List<string> Load()
	{
		var lines = new HashSet<string>();

		foreach (var id in _mobs)
		{
			foreach (var path in GetMovePaths(id))
			{
				lines.Add(path);
			}
		}

		var sortedLines = lines.ToList();
		sortedLines.Sort();
		return sortedLines;
	}
	
	private HashSet<string> GetMovePaths(uint model)
	{
		var movePaths = new HashSet<string>();

		for (var i = 0; i < MaxA; i++)
		{
			for (var j = 0; j < MaxSp; j++)
			{
				foreach (var format in _animationFormats)
				{
					var path = string.Format(format, model, i, j);
					if (!_lumina.FileExists(path)) continue;
					var file = _lumina.GetFile<PapFile>(path);
					var sid = file.SkeletonId;
					var bid = file.BaseId;
					movePaths.Add(path + $" | {sid} {bid}");
				}
			}
		}

		return movePaths;
	}
}