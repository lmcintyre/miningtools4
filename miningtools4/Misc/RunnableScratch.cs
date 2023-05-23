using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Data.Structs;
using Newtonsoft.Json;

namespace miningtools4;

public class RunnableScratch
{
	private static string GameDirectory = @"D:\RE\xiv_game\{0}\game\sqpack";
	private static string OutputDirectory = @"C:\Users\Liam\Desktop\miningtools4\{0}\{1}.txt";
	private static string OutputFormatPath = @"C:\Users\Liam\Desktop\miningtools4\{0}\{1}\";

	private const string glob = @"C:\Users\Liam\Desktop\tmp\act\project\FFXIV_ACT_Plugin\Machina\Machina.FFXIV\Headers\Opcodes\Global_docs.txt";

	private const string AutoPath = @"C:\Users\Liam\Documents\repos\Whoops\data\";

	public static void StupidImageExtractShit()
	{
		var luminaOld = new GameData(@"D:\RE\xiv_game\6.28x1\game\sqpack");
		var luminaNew = new GameData(@"D:\RE\xiv_game\6.3\game\sqpack");

		var outOldList = @"C:\Users\Liam\Desktop\tmp\top\shit_image\outOld.txt";
		var outNewList = @"C:\Users\Liam\Desktop\tmp\top\shit_image\outNew.txt";
		var outImage = @"C:\Users\Liam\Desktop\tmp\top\shit_image\images\";

		var oldDict = new Dictionary<byte[], string>();
		var newDict = new Dictionary<byte[], string>();
		
		var md5 = MD5.Create();
		
		ConcurrentBag<string> oldLines = new ConcurrentBag<string>();
		ConcurrentBag<string> newLines = new ConcurrentBag<string>();
		var stop = Stopwatch.StartNew();

		foreach (var cat in luminaOld.Repositories["ffxiv"].Categories[4])
		{
			foreach (var (hash, index) in cat.IndexHashTableEntries)
			{
				try
				{
					int folderHash = (int)((hash & 0xFFFFFFFF00000000) >> 32);
					int fileHash = (int)(hash & 0x00000000FFFFFFFF);
					
					var meta = cat.DatFiles[index.DataFileId].GetFileMetadata(index.Offset);
					if (meta.Type != FileType.Texture) continue;

					var file = cat.DatFiles[index.DataFileId].ReadFile<FileResource>(index.Offset);
					var dataHash = md5.ComputeHash(file.Data);
					
					var name = HashDatabaseAccessor.GetFullPath(folderHash, fileHash);
					oldLines.Add($"{name}: {Convert.ToHexString(dataHash)}");
				}
				catch (Exception e)
				{
					Console.WriteLine("Error reading file.");
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
			}
		}
		
		var searchTime = stop.ElapsedMilliseconds;
		var oldLinesSet = oldLines.ToHashSet();
		var sortedOldLines = oldLinesSet.ToList();
		sortedOldLines.Sort();
		File.WriteAllLines(outOldList, sortedOldLines);
		Console.WriteLine("--------------------");
		Console.WriteLine($"Stupid shit images {TimeSpan.FromMilliseconds(searchTime):c}");
		Console.WriteLine("--------------------");
		Console.WriteLine($"Wrote {sortedOldLines.Count} paths.");
		stop.Restart();
		
		foreach (var cat in luminaNew.Repositories["ffxiv"].Categories[4])
		{
			foreach (var (hash, index) in cat.IndexHashTableEntries)
			{
				try
				{
					int folderHash = (int)((hash & 0xFFFFFFFF00000000) >> 32);
					int fileHash = (int)(hash & 0x00000000FFFFFFFF);
					
					var meta = cat.DatFiles[index.DataFileId].GetFileMetadata(index.Offset);
					if (meta.Type != FileType.Texture) continue;

					var file = cat.DatFiles[index.DataFileId].ReadFile<TexFile>(index.Offset);
					var dataHash = md5.ComputeHash(file.Data);
					
					var name = HashDatabaseAccessor.GetFullPath(folderHash, fileHash);
					var line = $"{name}: {Convert.ToHexString(dataHash)}";
					newLines.Add(line);
					if (!oldLinesSet.Contains(line))
					{
						var sanitizedName = name
							.Replace("/", "")
							.Replace("<", "")
							.Replace(">", "")
							.Replace(":", "")
							.Replace("\"", "")
							.Replace("|", "")
							.Replace("?", "")
							.Replace("*", "");
						Util.WriteImage(file, Path.Combine(outImage, sanitizedName + ".png"));
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Error reading file.");
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
			}
		}
		searchTime = stop.ElapsedMilliseconds;
		var sortedNewLines = newLines.ToHashSet().ToList();
		sortedNewLines.Sort();
		File.WriteAllLines(outNewList, sortedNewLines);
		Console.WriteLine("--------------------");
		Console.WriteLine($"Stupid shit images {TimeSpan.FromMilliseconds(searchTime):c}");
		Console.WriteLine("--------------------");
		Console.WriteLine($"Wrote {sortedNewLines.Count} paths.");
	}

	static void Main28(string[] args)
	{
		string latestPatch = "6.08x1";
		LuminaOptions opt = new LuminaOptions
		{
			PanicOnSheetChecksumMismatch = false
		};
		GameData latest = new GameData(string.Format(GameDirectory, latestPatch), opt);

		var o = @"C:\Users\Liam\Desktop\vfxtmp\";

		var folder1Str = "vfx/monster/gimmick4/eff";
		var folder2Str = "vfx/monster/gimmick4/texture";

		uint folder1 = Lumina.Misc.Crc32.Get(folder1Str);
		uint folder2 = Lumina.Misc.Crc32.Get(folder2Str);

		// foreach (var repo in latest.Repositories)
		foreach (var cat in latest.Repositories["ffxiv"].Categories.Where(x => x.Key == 8).SelectMany(x => x.Value))
		foreach (var (hash, index) in cat.IndexHashTableEntries)
		{
			try
			{
				string catId = $"{cat.CategoryId:D2}{cat.Expansion:D2}{cat.Chunk:D2}";
				var folderHash = ((hash & 0xFFFFFFFF00000000) >> 32);
				var fileHash = (int)(hash & 0x00000000FFFFFFFF);

				if (folderHash == folder1 || folderHash == folder2)
				{
					var prep = "";
					if (folderHash == folder1)
						prep = folder1Str;
					else
						prep = folder2Str;
					var file = cat.DatFiles[index.DataFileId].ReadFile<FileResource>(index.Offset);
					var fileName = HashDatabaseAccessor.GetFilename(fileHash);
					var fold = Path.Combine(o, prep);
					Directory.CreateDirectory(fold);
					Console.WriteLine(fileName);
					var path = Path.Combine(fold, fileName);
					File.WriteAllBytes(path, file.Data);
				}
			}
			catch (Exception e)
			{
				// Console.WriteLine("Error reading file.");
				// Console.WriteLine(e.Message);
				// Console.WriteLine(e.StackTrace);
			}
		}
	}

	static void Main142(string[] args)
	{
		string latestPatch = "6.1";
		LuminaOptions opt = new LuminaOptions
		{
			PanicOnSheetChecksumMismatch = false
		};
		GameData latest = new GameData(string.Format(GameDirectory, latestPatch), opt);

		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("bg/ffxiv/est_e1/ind/e1i1/level/envl/evl7008262.amb")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("exd/root.exl")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("bg/ffxiv/fst_f1/dun/f1d1/grass/grass_zone_data.gzd")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("game_script/content/deepdungeon2achievement.luab")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("chara/xls/animation/motionlinetable.mlt")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("chara/xls/animation/paploadtable.plt")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("shader/shpk/3dui.shpk")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("bg/ffxiv/wil_w1/fld/w1f2/skeleton/skl_w1f2_l1_mach1.sklb")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("chara/demihuman/d0001/skeleton/base/b0001/skl_d0001b0001.skp")));
		Console.WriteLine("{0:X}", Util.FindExtensionMagic(latest.GetFile("bgcommon/world/air/shared/timelines/for_bg/tlbg_w_air_001_01a_close.tmb")));

		// Console.WriteLine(Util.FindExtensionMagic(latest.GetFile("")));
	}

	static void Main876(string[] args)
	{
		var filename = @"C:\Users\Liam\Desktop\thing.csv";
		var text = File.ReadAllLines(filename);
		foreach (var line in text)
		{
			var lineSplit = line.Split(",");
			// var full = int.Parse(lineSplit[2]);

			if (int.TryParse(lineSplit[0], out var folder) && int.TryParse(lineSplit[1], out var file))
			{
				Console.WriteLine(HashDatabaseAccessor.GetFullPath(folder, file));
			}

			if (int.TryParse(lineSplit[2], out var full))
			{
				Console.WriteLine(HashDatabaseAccessor.GetFullPath(full));
			}
		}
	}

	static void Main4345(string[] args)
	{
		string[] patches =
		{
			"6.0", "6.08x1", "6.1"
		};

		foreach (var patch in patches)
		{
			var data = new GameData(string.Format(GameDirectory, patch), options: new LuminaOptions
			{
				PanicOnSheetChecksumMismatch = false
			});
			var conf = new GeneratorConfig
			{
				OutputPath = true,
				OutputToConsole = false,
				CondensedOutput = true,
				BreakOnImcMissing = true,
				UseSheetsToFindUsed = false
			};

			conf.OutputFilename = string.Format(OutputDirectory, patch, "mapicons");
			new MinimapFinder(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, "statusicon");
			new StatusIconFinder(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, "itemicon");
			new ItemIconFinder(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, "genpaths");
			new ReferencedPathFinder(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, "allfiles");
			new AllPathGenerator(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, "monsters");
			new MonsterGenerator(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, "weapons");
			new WeaponGenerator(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, "equipment");
			new EquipmentGenerator(data, conf).Output();

			conf.UseSheetsToFindUsed = true;

			conf.OutputFilename = string.Format(OutputDirectory, patch, @"\used\monsters");
			new MonsterGenerator(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, @"\used\weapons");
			new WeaponGenerator(data, conf).Output();

			conf.OutputFilename = string.Format(OutputDirectory, patch, @"\used\equipment");
			new EquipmentGenerator(data, conf).Output();

			conf.OutputFilename = Path.GetDirectoryName(string.Format(OutputDirectory, patch, "_"));
			new MiscSheetDataGenerator(data, conf).Output();
		}
	}

	public static void main()
	{
		var dir = @"D:\RE\xiv_game\copies_from_hez";

		var storage = new Dictionary<string, Dictionary<string, string>>();

		foreach (var file in Directory.GetFiles(dir, "*.ver", SearchOption.AllDirectories))
		{
			var ver = File.ReadAllText(file);

			string exeVer = "";
			string dx11Ver = "";

			if (file.EndsWith("ffxivgame.ver"))
			{
				var exe = file.Replace("ffxivgame.ver", "ffxiv.exe");
				var dx11exe = file.Replace("ffxivgame.ver", "ffxiv_dx11.exe");

				exeVer = GetBuild(exe);
				dx11Ver = GetBuild(dx11exe);
			}

			var repo = Path.GetFileNameWithoutExtension(file);
			var gameDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file)));
			gameDir = file
					.Replace("D:\\RE\\xiv_game\\copies_from_hez\\", "")
					.Replace("ex1\\", "")
					.Replace("ex2\\", "")
					.Replace("ex3\\", "")
					.Replace("ex4\\", "")
					.Replace("game\\", "")
					.Replace("sqpack\\", "")
					.Replace("ex1.ver", "")
					.Replace("ex2.ver", "")
					.Replace("ex3.ver", "")
					.Replace("boot\\", "")
					.Replace("ffxivboot.ver", "")
					.Replace("ffxivgame.ver", "")
					.Replace("FINAL FANTASY XIV - A Realm Reborn", "")
					.Replace("\\\\", "")
					.Replace("\\", "")
				;
			if (repo == "ffxivgame")
				repo = "ffxiv";

			if (!storage.TryGetValue(gameDir, out var repoDict))
			{
				repoDict = new Dictionary<string, string>();
				storage.Add(gameDir, repoDict);
			}
			repoDict.Add(repo, ver);

			if (!string.IsNullOrEmpty(exeVer))
				repoDict.Add("ffxiv.exe", exeVer);

			if (!string.IsNullOrEmpty(dx11Ver))
				repoDict.Add("ffxiv_dx11.exe", dx11Ver);

			// Console.WriteLine($"{gameDir} : {repo} : {ver}");
		}

		foreach (var (ver, repos) in storage)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append($"{ver}");

			// if (repos.TryGetValue("ffxivboot", out var bootVer))
			// {
			//     sb.Append($"boot: {bootVer} ");
			// }

			if (repos.TryGetValue("ffxiv.exe", out var ffxivExeVer))
			{
				sb.Append($",{ffxivExeVer}");
			}

			if (repos.TryGetValue("ffxiv_dx11.exe", out var ffxivDx11Ver))
			{
				sb.Append($",{ffxivDx11Ver}");
			}

			if (repos.TryGetValue("ffxiv", out var ffxivVer))
			{
				sb.Append($",{ffxivVer}");
			}

			if (repos.TryGetValue("ex1", out var ex1Ver))
			{
				sb.Append($",{ex1Ver}");
			}

			if (repos.TryGetValue("ex2", out var ex2Ver))
			{
				sb.Append($",{ex2Ver}");
			}

			if (repos.TryGetValue("ex3", out var ex3Ver))
			{
				sb.Append($",{ex3Ver}");
			}
			else
			{
				sb.Append($",N/A");
			}

			Console.WriteLine(sb.ToString());
		}
	}

	private static string GetBuild(string path)
	{
		byte[] bytes =
		{
			0x2F, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x66, 0x66, 0x31, 0x34, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x72, 0x65, 0x76
		};
		var data = File.ReadAllBytes(path);
		var stringBytes = new List<byte>();
		for (int i = 0; i < data.Length - bytes.Length; i++)
		{
			if (data.AsSpan().Slice(i, bytes.Length).SequenceEqual(bytes))
			{
				i += 16;
				for (int j = 0; data[i + j] != '*'; j++)
				{
					stringBytes.Add(data[i + j]);
				}
				break;
			}
		}
		return Encoding.ASCII.GetString(stringBytes.ToArray());
	}

	static void Main32131(string[] args)
	{
		// var lumina = new Cyalume(@"D:\RE\xiv_game\6.0\game\sqpack");
		Console.WriteLine("Hello world!");
		var lumina = new GameData(@"/mnt/d/RE/xiv_game/6.0/game/sqpack");
		Console.WriteLine("Lumina loaded!");

		var start = 1;
		var end = 4000;

		var outPath = new DirectoryInfo("out");

		if (!outPath.Exists)
			outPath.Create();

		for (var i = start; i < end; i++)
		{
			var icon = GetIcon(lumina, i);

			if (icon == null)
			{
				Console.WriteLine("File not found$" + $"-> {i:D6}");
				continue;
			}

			Console.WriteLine($"-> {i:D6}");
			var folder = outPath.CreateSubdirectory($"{i / 1000:D3}000");

			//GetImage(icon).Save(Path.Combine(folder.FullName, $"{i:D6}.png"), ImageFormat.Png);
			GetImage(icon).Save(Path.Combine(folder.FullName, $"{i:D6}_hr1.png"), ImageFormat.Png);

		}
	}

	private enum ClientLanguage
	{
		Japanese,
		English,
		German,
		French
	}

	/// <summary>
	/// Get a <see cref="TexFile"/> containing the icon with the given ID.
	/// </summary>
	/// <param name="iconId">The icon ID.</param>
	/// <returns>The <see cref="TexFile"/> containing the icon.</returns>
	private static TexFile GetIcon(GameData lumina, int iconId)
	{
		return GetIcon(lumina, ClientLanguage.English, iconId);
	}

	/// <summary>
	/// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given language.
	/// </summary>
	/// <param name="iconLanguage">The requested language.</param>
	/// <param name="iconId">The icon ID.</param>
	/// <returns>The <see cref="TexFile"/> containing the icon.</returns>
	private static TexFile GetIcon(GameData lumina, ClientLanguage iconLanguage, int iconId)
	{
		var type = iconLanguage switch
		{
			ClientLanguage.Japanese => "ja/",
			ClientLanguage.English => "en/",
			ClientLanguage.German => "de/",
			ClientLanguage.French => "fr/",
			_ => throw new ArgumentOutOfRangeException(nameof(iconLanguage),
				"Unknown Language: " + iconLanguage)
		};

		return GetIcon(lumina, type, iconId);
	}

	//private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}.tex";

	private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}_hr1.tex";
	/// <summary>
	/// Get a <see cref="TexFile"/> containing the icon with the given ID, of the given type.
	/// </summary>
	/// <param name="type">The type of the icon (e.g. 'hq' to get the HQ variant of an item icon).</param>
	/// <param name="iconId">The icon ID.</param>
	/// <returns>The <see cref="TexFile"/> containing the icon.</returns>
	private static TexFile GetIcon(GameData lumina, string type, int iconId)
	{
		try
		{
			type ??= string.Empty;
			if (type.Length > 0 && !type.EndsWith("/"))
				type += "/";

			var filePath = string.Format(IconFileFormat, iconId / 1000, type, iconId);
			var file = lumina.GetFile<TexFile>(filePath);

			if (file != default(TexFile) || type.Length <= 0) return file;

			// Couldn't get specific type, try for generic version.
			filePath = string.Format(IconFileFormat, iconId / 1000, string.Empty, iconId);
			file = lumina.GetFile<TexFile>(filePath);
			return file;
		}
		catch (Exception e)
		{
			return null;
		}
	}

	private static unsafe Image GetImage(TexFile tex)
	{
		// this is terrible please find something better or get rid of .net imaging altogether
		Image image;
		fixed (byte* p = tex.ImageData)
		{
			var ptr = (IntPtr)p;
			using var tempImage = new Bitmap(tex.Header.Width, tex.Header.Height, tex.Header.Width * 4, PixelFormat.Format32bppArgb, ptr);
			image = new Bitmap(tempImage);
		}

		return image;
	}

	public static void Main123213(string[] args)
	{
		var stringThing = @"PlayerSetup = 0x03CB, // updated 6.11a
UpdateHpMpTp = 0x0231, // updated 6.11a
UpdateClassInfo = 0x02D1, // updated 6.11a
PlayerStats = 0x036E, // updated 6.11a
ActorControl = 0x024B, // updated 6.11a
ActorControlSelf = 0x0334, // updated 6.11a
ActorControlTarget = 0x0370, // updated 6.11a
Playtime = 0x039D, // updated 6.11a
UpdateSearchInfo = 0x0251, // updated 6.11a
ExamineSearchInfo = 0x0236, // updated 6.11a
ActorCast = 0x03DF, // updated 6.11a
CurrencyCrystalInfo = 0x00D8, // updated 6.11a
InitZone = 0x0086, // updated 6.11a
ActorMove = 0x0132, // updated 6.11a
PlayerSpawn = 0x0336, // updated 6.11a
ActorSetPos = 0x01D9, // updated 6.11a
PrepareZoning = 0x02A0, // updated 6.11a
ContainerInfo = 0x0288, // updated 6.11a
ItemInfo = 0x038D, // updated 6.11a
PlaceFieldMarker = 0x0137, // updated 6.11a
PlaceFieldMarkerPreset = 0x015E, // updated 6.11a
EffectResult = 0x012A, // updated 6.11a
EventStart = 0x0156, // updated 6.11a
EventFinish = 0x026B, // updated 6.11a
SomeDirectorUnk4 = 0x01D4, // updated 6.11a
DesynthResult = 0x030D, // updated 6.11a
FreeCompanyInfo = 0x010C, // updated 6.11a
FreeCompanyDialog = 0x039E, // updated 6.11a
MarketBoardSearchResult = 0x01DE, // updated 6.11a
MarketBoardItemListingCount = 0x03B7, // updated 6.11a
MarketBoardItemListingHistory = 0x00E1, // updated 6.11a
MarketBoardItemListing = 0x039A, // updated 6.11a
MarketBoardPurchase = 0x0180, // updated 6.11a
UpdateInventorySlot = 0x021E, // updated 6.11a
InventoryActionAck = 0x014B, // updated 6.11a
InventoryTransaction = 0x028F, // updated 6.11a
InventoryTransactionFinish = 0x0397, // updated 6.11a
ResultDialog = 0x027A, // updated 6.11a
RetainerInformation = 0x035A, // updated 6.11a
NpcSpawn = 0x026D, // updated 6.11a
ItemMarketBoardInfo = 0x00BE, // updated 6.11a
ObjectSpawn = 0x0305, // updated 6.11a
Effect = 0x00B5, // updated 6.11a
StatusEffectList = 0x032E, // updated 6.11a
ActorGauge = 0x01C2, // updated 6.11a
CFNotify = 0x01D2, // updated 6.11a
AoeEffect8 = 0x014F, // updated 6.11a
AirshipTimers = 0x0356, // updated 6.11a
SubmarineTimers = 0x00DE, // updated 6.11a
AirshipStatusList = 0x03A1, // updated 6.11a
AirshipStatus = 0x01B3, // updated 6.11a
AirshipExplorationResult = 0x0188, // updated 6.11a
SubmarineProgressionStatus = 0x01BC, // updated 6.11a
SubmarineStatusList = 0x03AC, // updated 6.11a
SubmarineExplorationResult = 0x01B1, // updated 6.11a
EventPlay = 0x85, // updated 6.11a
EventPlay4 = 0x2F4, // updated 6.11a
EventPlay8 = 0x176, // updated 6.11a
EventPlay16 = 0x2D4, // updated 6.11a
EventPlay32 = 0x2FF, // updated 6.11a
EventPlay64 = 0x289, // updated 6.11a
EventPlay128 = 0x3A5, // updated 6.11a
EventPlay255 = 0xD9, // updated 6.11a
";

		var diffPath = @"D:\downloads\6.15.diff.json";
		var model = JsonConvert.DeserializeObject<List<OpcodeDiffElement>>(File.ReadAllText(diffPath));
		var map = new Dictionary<uint, uint>();
		foreach (var element in model)
		{
			if (element.nnew.Count != element.old.Count)
			{
				Console.Error.WriteLine("oh no!");
				continue;
			}

			var size = element.nnew.Count;
			for (int i = 0; i < size; i++)
			{
				var oold = uint.Parse(element.old[i].Replace("0x", ""), NumberStyles.HexNumber);
				var nnew = uint.Parse(element.nnew[i].Replace("0x", ""), NumberStyles.HexNumber);
				map[oold] = nnew;
			}

		}

		// var oldVer = "6.11a";
		var newVer = "6.15";

		var reg = new Regex(@"(?<opcodeName>.*?) = (?<opcode>.*?), \/\/ updated (?<ver>.*?)\n");
		var matches = reg.Matches(stringThing);
		foreach (var m in matches)
		{
			if (m is not Match match) continue;
			var name = match.Groups["opcodeName"].Value;
			var op = uint.Parse(match.Groups["opcode"].Value.Replace("0x", ""), NumberStyles.HexNumber);
			var newOp = map[op];
			Console.WriteLine($"{name} = 0x{newOp:X}, // updated {newVer}");
		}
	}
	
	class OpcodeDiffElement
	{
		[JsonProperty("new")]
		public List<string> nnew;
		public List<string> old;
	}
}

