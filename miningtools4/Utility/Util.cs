using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Data.Structs;

namespace miningtools4;

public static class Util
{
	private static Dictionary<uint, string> _extDict = new()
	{
		{ 0x00616574, "aet" },	// Animation Exchange Table
		{ 0x00424D41, "amb" },	// Ambient Binary
		{ 0x41564658, "avfx" },	// Audio/Visual Effects
		{ 0x00617774, "awt" },	// Animation Work Table
		{ 0x42545543, "cutb" }, // Cutscene Binary
		{ 0x00656964, "eid" },	// Element ID
		{ 0x42564E45, "envb" }, // Environment Binary
		{ 0x42535345, "essb" },	// Environment Sound Set Binary
		{ 0x46445845, "exd" },	// Excel Data
		{ 0x46485845, "exh" },	// Excel Header
		{ 0x544C5845, "exl" },	// Excel List
		{ 0x76736366, "fdt" },	// Font Data Type
		{ 0x64746667, "gfd" },	// Gaiji Font Data
		{ 0x67676420, "ggd" },	// Grass Grid Data
		{ 0x00677A64, "gzd" },	// Grass Zone Data
		{ 0x3142434C, "lcb" },	// Layer Collision Binary
		{ 0x3142474C, "lgb" },	// Layer Group Binary
		{ 0x61754C1B, "luab" },	// Lua Binary
		{ 0x3142564C, "lvb" },	// Level Binary
		{ 0x006D6C74, "mlt" },	// Motion Line Table
		{ 0x4D534246, "msb" },	// ? ? Binary (Music Binary?)
		{ 0x4253424F, "obsb" },	// Object Behavior Set Binary
		{ 0x20706170, "pap" },	// Partial Animation Pack
		{ 0x7079626C, "plt" },	// Pap Load Table
		{ 0x00EE0ACD, "ptbl" },	// Pap Load Table 2
		{ 0x42444553, "scd" },	// Sound Container Data (?)
		{ 0x31424753, "sgb" },	// Shared Group Binary
		{ 0x64436853, "shcd" },	// Shader Code
		{ 0x6B506853, "shpk" },	// Shader Pack
		{ 0x736B6C62, "sklb" },	// Skeleton Binary
		{ 0x736B6C70, "skp" },	// Skeleton Parameter
		{ 0x31425653, "svb" },	// Sky Visibility Binary
		{ 0x424C4D54, "tmb" },	// Timeline Binary
		{ 0x756C6468, "uld" },	// UI ? ?
		{ 0x31425755, "uwb" }	// Underwater Binary
	};
		
	public static string GetExtension(FileResource file)
	{
		var sig = BitConverter.ToUInt32(file.DataSpan[..4]);
		if (!_extDict.TryGetValue(sig, out var ext))
		{
			ext = file.FileInfo.Type switch
			{
				FileType.Model => "mdl",
				FileType.Texture => "tex",
				_ => ""
			};
		}
		return ext;
	}

	public static uint FindExtensionMagic(FileResource file)
	{
		var sig = BitConverter.ToUInt32(file.DataSpan[..4]);
		return sig;
	}

	public static void WriteImage(TexFile tex, string outPath)
	{
		Image image;
		unsafe
		{
			fixed (byte* p = tex.ImageData)
			{
				var ptr = (IntPtr)p;
				using var tempImage = new Bitmap(tex.Header.Width, tex.Header.Height, tex.Header.Width * 4, PixelFormat.Format32bppArgb, ptr);
				image = new Bitmap(tempImage);
			}
		}

		image.Save(outPath, ImageFormat.Png);
	}
	private const string COMMON = "com"; 
	private const string BGCOMMON = "bgc";
	private const string BG = "bg/";
	private const string CUT = "cut";
	private const string CHARA = "cha";
	private const string SHADER = "sha";
	private const string UI = "ui/";
	private const string SOUND = "sou";
	private const string VFX = "vfx";
	private const string UI_SCRIPT = "ui_";
	private const string EXD = "exd";
	private const string GAME_SCRIPT = "gam";
	private const string MUSIC = "mus";
	private const string SQPACK_TEST = "_sq";
	private const string DEBUG = "_de";

	public static uint GetCategoryIdForPath(string gamePathStr)
	{
		return GetCategoryIdForPath(gamePathStr.AsSpan());
	}
	
	public static uint GetCategoryIdForPath(ReadOnlySpan<char> gamePath)
	{
		return gamePath switch
		{
			_ when gamePath.StartsWith(COMMON) => 0x000000,
			_ when gamePath.StartsWith(BGCOMMON) => 0x010000,
			_ when gamePath.StartsWith(BG) => GetBgSubCategoryId(gamePath) | (0x02 << 16),
			_ when gamePath.StartsWith(CUT) => GetNonBgSubCategoryId(gamePath, 4) | (0x03 << 16),
			_ when gamePath.StartsWith(CHARA) => 0x040000,
			_ when gamePath.StartsWith(SHADER) => 0x050000,
			_ when gamePath.StartsWith(UI) => 0x060000,
			_ when gamePath.StartsWith(SOUND) => 0x070000,
			_ when gamePath.StartsWith(VFX) => 0x080000,
			_ when gamePath.StartsWith(UI_SCRIPT) => 0x090000,
			_ when gamePath.StartsWith(EXD) => 0x0A0000,
			_ when gamePath.StartsWith(GAME_SCRIPT) => 0x0B0000,
			_ when gamePath.StartsWith(MUSIC) => GetNonBgSubCategoryId(gamePath, 6) | (0x0C << 16),
			_ when gamePath.StartsWith(SQPACK_TEST) => 0x110000,
			_ when gamePath.StartsWith(DEBUG) => 0x120000,
			_ => 0,
		};
	}

	private static uint GetBgSubCategoryId(ReadOnlySpan<char> gamePath)
	{
		var segmentIdIndex = 3;
		uint expacId = 0;

		// Check if this is an ex* path
		if (gamePath[3] != 'e')
			return 0;

		// Check if our expac ID has one or two digits
		if (gamePath[6] == '/')
		{
			expacId = uint.Parse(gamePath[5..6]) << 8;
			segmentIdIndex = 7;
		}
		else if (gamePath[7] == '/')
		{
			expacId = uint.Parse(gamePath[5..7]) << 8;
			segmentIdIndex = 8;
		}
		else
		{
			expacId = 0;
		}

		// Parse the segment id for this bg path
		var segmentId = uint.Parse(gamePath.Slice(segmentIdIndex, 2));
		
		return expacId + segmentId;
	}

	private static uint GetNonBgSubCategoryId(ReadOnlySpan<char> gamePath, int firstDirLen)
	{
		if (gamePath[firstDirLen] != 'e')
			return 0;

		if (gamePath[firstDirLen + 3] == '/')
			return uint.Parse(gamePath.Slice(firstDirLen + 2, 1)) << 8;

		if (gamePath[firstDirLen + 4] == '/')
			return uint.Parse(gamePath.Slice(firstDirLen + 2, 2)) << 8;

		return 0;
	}

	public static uint CalcFullHash(string pathStr)
	{
		var path = pathStr.AsSpan();
		return Crc32_2.Get(path);
	}
	
	public static (uint folderHash, uint fileHash) CalcHashes(string pathStr)
	{
		var path = pathStr.AsSpan();
		var splitter = path.LastIndexOf('/');
		var folderStr = path[..splitter];
		var fileStr = path[(splitter + 1)..];
		
		var folderHash = Crc32_2.Get(folderStr);
		var fileHash = Crc32_2.Get(fileStr);
		
		return (folderHash, fileHash);
	}

	public static (uint folderHash, uint fileHash, uint fullHash) CalcAllHashes(string path)
	{
		var parts = CalcHashes(path);
		return (parts.folderHash, parts.fileHash, CalcFullHash(path));
	}

	public static Dictionary<ulong, IndexHashTableEntry> LoadIndex(string path)
	{
		var fs = File.Open(path, FileMode.Open);
		return LoadIndex(fs);
	}
	
	public static Dictionary<uint, Index2HashTableEntry> LoadIndex2(string path)
	{
		var fs = File.Open(path, FileMode.Open);
		return LoadIndex2(fs);
	}

	public static Dictionary<ulong, IndexHashTableEntry> LoadIndex(Stream s)
	{
		using var br = new LuminaBinaryReader(s);

		// skip og header
		var header = SqPackHeader.Read(br);
		s.Position = header.size;

		// read index hdr
		var header2 = br.ReadStructure<SqPackIndexHeader>();

		// read hashtable entries
		s.Position = header2.index_data_offset;
		var entryCount = header2.index_data_size / Marshal.SizeOf(typeof(IndexHashTableEntry));

		return br
			.ReadStructures<IndexHashTableEntry>((int) entryCount)
			.ToDictionary(k => k.hash, v => v);
	}

	public static Dictionary<uint, Index2HashTableEntry> LoadIndex2(Stream s)
	{
		using var br = new LuminaBinaryReader(s);

		// skip og header
		var header = SqPackHeader.Read(br);
		s.Position = header.size;

		// read index hdr
		var header2 = br.ReadStructure<SqPackIndexHeader>();

		// read hashtable entries
		s.Position = header2.index_data_offset;
		var entryCount = header2.index_data_size / Marshal.SizeOf(typeof(Index2HashTableEntry));

		return br
			.ReadStructures<Index2HashTableEntry>((int) entryCount)
			.ToDictionary(k => k.hash, v => v);
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WeaponModel
	{
		public int skeletonId;
		public int patternId;
		public int imageChangeId;
		public int stainingId;

		public WeaponModel(ushort sk, ushort pa, ushort imc, ushort st)
		{
			skeletonId = sk;
			patternId = pa;
			imageChangeId = imc;
			stainingId = st;
		}

		public ulong Pack()
		{
			return Util.PackWeapon((ushort)skeletonId, (ushort)patternId, (ushort)imageChangeId, (ushort)stainingId);
		}

		public override string ToString() => $"{skeletonId} {patternId} {imageChangeId} {stainingId}";
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct EquipmentModel
	{
		public int patternId;
		public int imageChangeId;
		public int stainingId;

		public EquipmentModel(ushort pa, byte imc, byte st)
		{
			patternId = pa;
			imageChangeId = imc;
			stainingId = st;
		}

		public uint Pack()
		{
			var packed = Util.PackEquipment((ushort)patternId, (byte)imageChangeId, (byte)stainingId);
			// PluginLog.Log($"Packing {patternId}, {imageChangeId}, {stainingId} into {packed}");
			return packed;
		}

		public override string ToString() => $"{patternId} {imageChangeId} {stainingId}";
	}

	public static ulong PackWeapon(ushort skeletonId, ushort patternId, ushort imageChangeId, ushort stainingId)
	{
		ulong result = 0;
		result |= skeletonId;
		result |= (ulong)patternId << 16;
		result |= (ulong)imageChangeId << 32;
		result |= (ulong)stainingId << 48;
		return result;
	}

	public static uint PackEquipment(ushort patternId, byte imageChangeId, byte stainingId)
	{
		uint result = 0;
		result |= patternId;
		result |= (uint)imageChangeId << 16;
		result |= (uint)stainingId << 24;
		return result;
	}

	public static WeaponModel UnpackWeapon(ulong model)
	{
		return new WeaponModel
		{
			skeletonId = (ushort)(model & 0xFFFF),
			patternId = (ushort)(model >> 16 & 0xFFFF),
			imageChangeId = (ushort)(model >> 32 & 0xFFFF),
			stainingId = (ushort)(model >> 48 & 0xFFFF),
		};
	}

	public static EquipmentModel UnpackEquipment(uint model)
	{
		return new EquipmentModel
		{
			patternId = (ushort)(model & 0xFFFF),
			imageChangeId = (byte)(model >> 16 & 0xFF),
			stainingId = (byte)(model >> 24 & 0xFF),
		};
	}
}