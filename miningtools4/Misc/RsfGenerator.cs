using System;
using System.Diagnostics;
using System.IO;
using Lumina;

namespace miningtools4;

public class RsfGenerator
{
	private GameData _lumina;
	private GeneratorConfig _config;

	public RsfGenerator(GameData lumina, GeneratorConfig config)
	{
		_lumina = lumina;
		_config = config;
	}

	public void Output(string path)
	{
		var fr = _lumina.GetFile(path);
		if (fr == null)
		{
			Console.WriteLine($"File '{path}' not found.");
			return;
		}

		var tmp1 = Path.GetTempFileName();
		var tmp2 = Path.GetTempFileName();

		File.WriteAllBytes(tmp1, fr.Data);
		Process.Start(new ProcessStartInfo
		{
			FileName = @"C:\Users\Liam\AppData\Local\Python\Python38\python.exe",
			WorkingDirectory = Environment.CurrentDirectory,
			Arguments = $"comp.py {tmp1} {tmp2}",
			UseShellExecute = true,
			CreateNoWindow = true,
		}).WaitForExit();
			
		var folderpath = path[..path.LastIndexOf('/')];
		var filepath = path[(path.LastIndexOf('/') + 1)..];
		var file = Lumina.Misc.Crc32.Get(filepath);
		var folder = Lumina.Misc.Crc32.Get(folderpath);

		var buffer = File.ReadAllBytes(tmp2);
		for (int j = 0; j < 64; j++)
			Console.Write($"{buffer[j]:X2} ");	
		Console.WriteLine();
			
		File.Copy(tmp2, Path.Combine(_config.OutputFilename, $"{folder:X}{file:X}.dat"), true);

		File.Delete(tmp1);
		File.Delete(tmp2);
	}
}