using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Lumina;
using Lumina.Data;
using Lumina.Data.Structs;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Extensions;
using ZiPatchLib;
using ZiPatchLib.Chunk;
using ZiPatchLib.Chunk.SqpkCommand;
using ZiPatchLib.Util;

namespace miningtools4;

public class PatchInspector
{
	private GameData lumina;
    private GeneratorConfig _config;
    
    public static Dictionary<uint, string> repoNames = new()
	    {
		    //Boot
		    { 0x2b5cbc63, "ffxivneo/win32/release/boot" },

		    //Global - FFXIVNeo
		    { 0x4e9a232b, "ffxivneo/win32/release/game" },
		    { 0x6b936f08, "ffxivneo/win32/release/ex1" },
		    { 0xf29a3eb2, "ffxivneo/win32/release/ex2" },
		    { 0x859d0e24, "ffxivneo/win32/release/ex3" },
		    { 0x1bf99b87, "ffxivneo/win32/release/ex4" },

		    //KR - Actoz
		    { 0xde199059, "actoz/win32/release_ko/game" },
		    { 0x573d8c07, "actoz/win32/release_ko/ex1" },
		    { 0xce34ddbd, "actoz/win32/release_ko/ex2" },
		    { 0xb933ed2b, "actoz/win32/release_ko/ex3" },
		    { 0x27577888, "actoz/win32/release_ko/ex4" },

		    //CHS - Shanda
		    { 0xc38effbc, "shanda/win32/release_chs/game" },
		    { 0x77420d17, "shanda/win32/release_chs/ex1" },
		    { 0xee4b5cad, "shanda/win32/release_chs/ex2" },
		    { 0x994c6c3b, "shanda/win32/release_chs/ex3" },
		    { 0x0728f998, "shanda/win32/release_chs/ex4" },
	    };

    public PatchInspector(GameData lumina, GeneratorConfig conf)
    {
        this.lumina = lumina;
        _config = conf;
    }

    public void Output(string patchPath)
    {
	    var patchIn = new FileInfo(patchPath);

        // Shitty exists check 1
        foreach (var file in Directory.GetFiles(_config.OutputFilename, "*.txt", SearchOption.AllDirectories))
        {
	        if ((file.Contains(@"\game\") && patchPath.Contains(@"game")) ||
	            (file.Contains(@"\ex1") && patchPath.Contains(@"\ex1")) ||
	            (file.Contains(@"\ex2") && patchPath.Contains(@"\ex2")) ||
	            (file.Contains(@"\ex3") && patchPath.Contains(@"\ex3")) ||
	            (file.Contains(@"\ex4") && patchPath.Contains(@"\ex4")))
	        {
		        var ver = patchIn.Name.Replace(".patch", "");
		        var vers = ver.Split("_");
		        ver = vers[^1];
		        if (file.Contains(ver))
		        {
			        Console.WriteLine($"Patch already processed: {patchIn.Name}");
			        return;
		        }
	        }
        }
        
        var outLines = new List<string>();
        var modFileDict = new Dictionary<uint, ModFileInfo>();
        var diskFileList = new SortedSet<string>();
        var indexDict = new Dictionary<uint, IndexHasher>();
        
        var patchApplyPath = Path.GetTempFileName();
        // var patchApplyPath = Path.Combine(Path.GetTempPath(), patchIn.Name.Replace(".patch", ""));
        File.Delete(patchApplyPath);
        Directory.CreateDirectory(patchApplyPath);

        var repoName = "unknown";
        var outFileName = "unknown";
        var exists = false;
        
        using (var patchFile = ZiPatchFile.FromFileName(patchIn.FullName))
		{
			var patchChunks = patchFile.GetChunks().ToList();
			var patchName = patchIn.Name.Replace(".patch", "");

			outLines.Add("PATCH: " + patchName);

			using (var store = new SqexFileStreamStore())
			{
				var config = new ZiPatchConfig(patchApplyPath)
				{
					Store = store,
					IgnoreMissing = true,
					IgnoreOldMismatch = true,
					OnlyFiles = true,
				};
				
				foreach (var chunk in patchChunks)
				{
					if (chunk.ChunkType == FileHeaderChunk.Type)
					{
						var fileHeader = (FileHeaderChunk)chunk;

						if (!repoNames.TryGetValue(fileHeader.RepositoryName, out repoName))
							repoName = "unknown";
						repoName = $"{fileHeader.RepositoryName:x8}_{repoName}";
						repoName = repoName.Replace("/", "_");
						outFileName = repoName + "_" + patchIn.Name.Replace(".patch", ".txt");
						outFileName = Path.Combine(_config.OutputFilename, outFileName);
						
						// Shitty exists check 2
						if (File.Exists(outFileName))
						{
							exists = true;
							break;
						}

						outLines.Add("===== FILE HEADER =====");
						outLines.Add("Version: " + fileHeader.Version);
						outLines.Add("PatchType: " + fileHeader.PatchType);
						outLines.Add("EntryFiles: " + fileHeader.EntryFiles);
						outLines.Add("AddDirectories: " + fileHeader.AddDirectories);
						outLines.Add("DeleteDirectories: " + fileHeader.DeleteDirectories);
						outLines.Add("DeleteDataSize: " + fileHeader.DeleteDataSize);
						outLines.Add("MinorVersion: " + fileHeader.MinorVersion);
						outLines.Add("RepositoryName: " + repoName);
						outLines.Add("Commands: " + fileHeader.Commands);
						outLines.Add("SqpkAddCommands: " + fileHeader.SqpkAddCommands);
						outLines.Add("SqpkDeleteCommands: " + fileHeader.SqpkDeleteCommands);
						outLines.Add("SqpkExpandCommands: " + fileHeader.SqpkExpandCommands);
						outLines.Add("SqpkHeaderCommands: " + fileHeader.SqpkHeaderCommands);
						outLines.Add("SqpkFileCommands: " + fileHeader.SqpkFileCommands);
						outLines.Add("===== END FILE HEADER =====");

						try
						{
							foreach (var patchChunk in patchChunks)
							{
								if (patchChunk is SqpkFile f)
								{
									var isExe = f.TargetFile.ToString().Contains("ffxiv.exe");
									var isdx11Exe = f.TargetFile.ToString().Contains("ffxiv_dx11.exe"); 
									
									// if (f.Operation == SqpkFile.OperationKind.AddFile && (f.TargetFile.ToString().Contains("index") || isExe || isdx11Exe))
										patchChunk.ApplyChunk(config);

									var op = f.Operation.ToString();
									var path = f.TargetFile.ToString();

									// Do exes later because we want build info
									if (!isExe && !isdx11Exe)
										diskFileList.Add($"{op}: {path}");
								}

								if (patchChunk is SqpkAddData a )
								{
									var info = new ModFileInfo(a);
									modFileDict[info.BlockOffset] = info;
								}

								if (patchChunk is SqpkDeleteData d)
								{
									var info = new ModFileInfo(d);
									modFileDict[info.BlockOffset] = info;
								}
							}
						}
						catch (Exception e)
						{
							Console.WriteLine(e.Message);
							Console.WriteLine(e.StackTrace);
						}
					}
				}
			}
		}

        if (exists)
        {
	        Console.WriteLine($"Patch already processed: {patchIn.Name}");
	        Directory.Delete(patchApplyPath, true);
	        return;
        }

        try
        {
	        foreach (var file in Directory.GetFiles(patchApplyPath, "ffxiv*.exe", SearchOption.AllDirectories))
	        {
		        var path = new FileInfo(file);
		        var build = GetBuild(file);
		        diskFileList.Add($"AddFile: {path.Name} {build}");
	        }
        }
        catch (Exception e)
        {
	        Console.WriteLine(e.Message);
	        Console.WriteLine(e.StackTrace);
        }
        
        try
        {
	        foreach (var file in Directory.GetFiles(patchApplyPath, "*.index", SearchOption.AllDirectories))
	        {
		        var fileInfo = new FileInfo(file);
		        var hasher = new IndexHasher(fileInfo);
		        indexDict[hasher.IndexId] = hasher;
	        }
        }
        catch (Exception e)
        {
	        Console.WriteLine(e.Message);
	        Console.WriteLine(e.StackTrace);
        }

        var errorsList = new SortedSet<string>();
        var modFileList = new SortedSet<string>();
        foreach (var modFile in modFileDict)
        {
	        var modFileInfo = modFile.Value;
	        if (!indexDict.TryGetValue(modFileInfo.IndexId, out var indexHasher))
	        {
		        errorsList.Add($"Error: Index not found in patch: {modFileInfo.IndexId:x6}");
		        continue;
	        }
	        
	        if (!indexHasher.HashTable.TryGetValue(modFileInfo.BlockOffset, out var hashEntry))
		        continue;

	        var path = HashDatabaseAccessor.GetFullPath((int)hashEntry.Folder, (int)hashEntry.FileName);
	        var type = modFileInfo.IsAdd ? "SqpkAdd" : "SqpkDelete";
	        modFileList.Add($"{type} ({modFileInfo.IndexId:x6}): {path}");
        }
        
        outLines.AddRange(errorsList);
        outLines.AddRange(diskFileList);
        outLines.AddRange(modFileList);

        Directory.Delete(patchApplyPath, true);

        Directory.CreateDirectory(_config.OutputFilename);
        File.WriteAllLines(Path.Combine(_config.OutputFilename, outFileName), outLines);
        
        if (_config.OutputToConsole)
            outLines.ForEach(Console.WriteLine);
    }

    private static string GetBuild(string path)
    {
	    byte[] bytes = {0x2F, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x66, 0x66, 0x31, 0x34, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x2A, 0x72, 0x65, 0x76};
	    var data = File.ReadAllBytes(path);
	    var stringBytes = new List<byte>();
	    for(int i = 0; i < data.Length - bytes.Length; i++) {
		    if (data.AsSpan().Slice(i, bytes.Length).SequenceEqual(bytes))
		    {
			    i += 16;
			    for (int j = 0; data[i + j] != '*'; j++) {
				    stringBytes.Add(data[i + j]);
			    }
			    break;
		    }
	    }
	    return Encoding.ASCII.GetString(stringBytes.ToArray());
    }
}

struct ModFileInfo
{
	public uint IndexId;
	public uint BlockOffset;
	public bool IsAdd;

	public ModFileInfo(SqpkAddData data)
	{
		var target = data.TargetFile.ToString();
		var start = target.LastIndexOf('/') + 1;
		var end = start + 6;
		var indexIdStr = target[start..end];
		var datIdStr = target[^1].ToString();
		var datId = uint.Parse(datIdStr, NumberStyles.HexNumber);

		BlockOffset = (uint) (data.BlockOffset / 0x08) | (datId << 1);
		IndexId = uint.Parse(indexIdStr, NumberStyles.HexNumber);
		IsAdd = true;
	}
	
	public ModFileInfo(SqpkDeleteData data)
	{
		var target = data.TargetFile.ToString();
		var start = target.LastIndexOf('/') + 1;
		var end = start + 6;
		var indexIdStr = target[start..end];
		var datIdStr = target[^1].ToString();
		var datId = uint.Parse(datIdStr, NumberStyles.HexNumber);

		BlockOffset = (uint) (data.BlockOffset / 0x08) | (datId << 1);
		IndexId = uint.Parse(indexIdStr, NumberStyles.HexNumber);
		IsAdd = false;
	}
}

class IndexHasher
{
    public class HashEntry
    {
        public uint Folder;
        public uint FileName;

        public override string ToString()
        {
            return $"Folder: {Folder:X8}; FileName: {FileName:X8};";
        }
    }

    private SqPackHeader SqIndexHeader { get; set; }
    private SqPackIndexHeader IndexHeader { get; set; }
    private Dictionary<ulong, IndexHashTableEntry> HashTableEntries { get; set; }
    public Dictionary<uint, HashEntry> HashTable { get; set; } = new();
    private FileInfo Index { get; set; }
    public uint IndexId { get; set; }
    
    public IndexHasher(FileInfo index)
    {
        Index = index;
        var indexIdStr = index.Name[..6];
        IndexId = uint.Parse(indexIdStr, NumberStyles.HexNumber);

        using var ss = new SqPackStream(Index);
        SqIndexHeader = ss.GetSqPackHeader();
        LoadIndex();
        MakeOffsetDict();
    }
    
    private void LoadIndex()
    {
        using var fs = Index.OpenRead();
        using var br = new BinaryReader(fs);

        // skip og header
        fs.Position = SqIndexHeader.size;

        // read index hdr
        IndexHeader = br.ReadStructure<SqPackIndexHeader>();

        // read hashtable entries
        fs.Position = IndexHeader.index_data_offset;
        var entryCount = IndexHeader.index_data_size / Marshal.SizeOf(typeof(IndexHashTableEntry));

        HashTableEntries = br
            .ReadStructures<IndexHashTableEntry>((int) entryCount)
            .ToDictionary(k => k.hash, v => v);
    }
    
    internal void MakeOffsetDict()
    {
	    foreach (var (hash, entry) in HashTableEntries)
	    {
		    var hashEntry = new HashEntry
		    {
			    FileName = (uint) (hash >> 0),
			    Folder = (uint) (hash >> 32)
		    };

		    HashTable.Add(entry.data, hashEntry);
	    }
    }
}