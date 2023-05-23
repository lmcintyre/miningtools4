﻿// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using Lumina.Data;
// using Lumina.Data.Structs;
// using ZiPatchLib;
// using ZiPatchLib.Chunk;
// using ZiPatchLib.Chunk.SqpkCommand;
//
// namespace miningtools4;
//
// class PatchFileSearcher
// {
// 	static Dictionary<string, SparseMemoryStream> DatStreams = new();
//
// 	void ParseChunk(SqpkAddData chunk)
// 	{
// 		chunk.TargetFile.ResolvePath(ZiPatchConfig.PlatformId.Win32);
// 		if (!DatStreams.TryGetValue(chunk.TargetFile.ToString(), out var stream))
// 		{
// 			Console.WriteLine($"New sparse entry for {chunk.TargetFile}");
// 			stream = new SparseMemoryStream();
// 			DatStreams.Add(chunk.TargetFile.ToString(), stream);
// 		}
//
// 		stream.Position = chunk.BlockOffset;
// 		stream.Write(chunk.BlockData, 0, chunk.BlockData.Length);
// 	}
//
// 	void ParseChunk(ZiPatchChunk chunk)
// 	{
// 	}
//
// 	public static void Main2()
// 	{
// 		
// 		foreach (var chunk in f.GetChunks())
// 		{
// 			ParseChunk((dynamic)chunk);
// 		}
//
// 		foreach (var (file, stream) in DatStreams)
// 		{
// 			Console.WriteLine($"File: {file}");
// 			Console.WriteLine($"Blocks: {string.Join(", ", stream.GetPopulatedChunks())}");
//
// 			foreach (var (offset, subStream) in stream.ChunkDictionary)
// 			{
// 				using var sqStream = new SqPackStream(subStream, PlatformId.Win32);
// 				var meta = sqStream.GetFileMetadata(0);
// 				switch (meta.Type)
// 				{
// 					case FileType.Empty:
// 						Console.WriteLine($"{file}:{offset:X08}:Retarded Shit File");
// 						continue; // Skip this shit
// 					case FileType.Model:
// 						break;
// 					case FileType.Texture:
// 						Console.WriteLine($"{file}:{offset:X08}:{meta.Type}");
// 						continue; // Skip this shit too
// 					case FileType.Standard:
// 						break; // Interesting case
// 				}
//
// 				var fileResource = sqStream.ReadFile<FileResource>(0);
// 				
// 			}
// 		}
// 	}
//
//
// 	public class SparseMemoryStream : Stream
// 	{
// 		public override bool CanRead { get; }
// 		public override bool CanSeek { get; }
// 		public override bool CanWrite { get; }
// 		public override long Length { get; }
// 		public override long Position { get; set; }
// 		
// 		public Dictionary<long, MemoryStream> ChunkDictionary = new Dictionary<long, MemoryStream>();
// 		public long StartPosition { get; set; }
//
// 		public IEnumerable<long> GetPopulatedChunks()
// 		{
// 			return ChunkDictionary.Keys;
// 		}
//
// 		public override void Flush()
// 		{
// 		}
//
// 		public override int Read(byte[] buffer, int offset, int count)
// 		{
// 			if (!ChunkDictionary.TryGetValue(Position, out var stream))
// 				return 0;
//
// 			var r = stream.Read(buffer, offset, count);
// 			Position += count;
//
// 			return r;
// 		}
//
// 		public override long Seek(long offset, SeekOrigin origin)
// 		{
// 			switch (origin)
// 			{
// 				case SeekOrigin.Begin:
// 					Position = offset;
// 					break;
// 				case SeekOrigin.Current:
// 					Position += offset;
// 					break;
// 				default:
// 					throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
// 			}
//
// 			return Position;
// 		}
//
// 		public override void SetLength(long value)
// 		{
// 		}
//
// 		public override void Write(byte[] buffer, int offset, int count)
// 		{
// 			if (!ChunkDictionary.TryGetValue(Position, out var stream))
// 			{
// 				stream = new MemoryStream();
// 				ChunkDictionary.Add(Position, stream);
// 			}
//
// 			stream.Write(buffer, offset, count);
// 			Position += count;
// 		}
// 	}
//
// }