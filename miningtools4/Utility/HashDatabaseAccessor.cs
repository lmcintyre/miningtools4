﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace miningtools4;

// Static, global access to a hash database file
public class HashDatabaseAccessor
{
    private static string _hashDbPath = @"C:\Users\Liam\Documents\repos\ffxiv-explorer-fork\hashlist.db";

    public static Dictionary<long, string> _foldersById;
    public static Dictionary<long, string> _filesById;
        
    public static Dictionary<int, string> _folders;
    public static Dictionary<int, string> _files;
    private static Dictionary<int, string> _fullpaths;
        
    static HashDatabaseAccessor()
    {
        var s = Stopwatch.StartNew();
        _folders = new Dictionary<int, string>();
        _files = new Dictionary<int, string>();
        _fullpaths = new Dictionary<int, string>();
        _foldersById = new Dictionary<long, string>();
        _filesById = new Dictionary<long, string>();
            
        using (var connection = new SqliteConnection($@"Data Source={_hashDbPath}"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"SELECT id, text FROM folders";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetValue(0) == DBNull.Value) continue;
                if (reader.GetValue(1) == DBNull.Value) continue;
                long hash = reader.GetInt64(0);

                string text = reader.GetString(1);
                    
                _foldersById.Add(hash, text);
                _folders.Add((int)Crc32_2.Get(text), text);
            }
                
            command = connection.CreateCommand();
            command.CommandText = @"SELECT id, text FROM filenames";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetValue(0) == DBNull.Value) continue;
                if (reader.GetValue(1) == DBNull.Value) continue;
                long hash = reader.GetInt64(0);

                string text = reader.GetString(1);
                    
                _filesById.Add(hash, text);
                _files[(int) Crc32_2.Get(text)] = text;
            }
                
            command = connection.CreateCommand();
            command.CommandText = @"SELECT fullhash, folderhash, filehash, folder, file FROM fullpaths";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetValue(0) == DBNull.Value) continue;
                if (reader.GetValue(1) == DBNull.Value) continue;
                if (reader.GetValue(2) == DBNull.Value) continue;
                if (reader.GetValue(3) == DBNull.Value) continue;
                if (reader.GetValue(4) == DBNull.Value) continue;
                    
                long hash = reader.GetInt64(0);

                if (hash > int.MaxValue || hash < int.MinValue) continue;

                long folderId = reader.GetInt64(3);
                long fileId = reader.GetInt64(4);

                string folder = _foldersById.GetValueOrDefault(folderId);
                string file = _filesById.GetValueOrDefault(fileId);
                    
                if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(file)) continue;
                var str = $"{folder}/{file}";
                _fullpaths[(int) hash] = str;
            }
        }
        s.Stop();
        Console.WriteLine($"Hash database loaded in {s.ElapsedMilliseconds}ms.");
    }

    public static string GetFilename(int hash)
    {
        if (!_files.TryGetValue(hash, out var ret))
            ret = $"~{hash:X}";
        return ret.ToLower();
    }
        
    public static string GetFilename(uint hash)
    {
        return GetFilename(unchecked((int)hash));
    }
        
    public static string GetFolder(int hash)
    {
        if (!_folders.TryGetValue(hash, out var ret))
            ret = $"~{hash:X}";
        return ret.ToLower();
    }
        
    public static string GetFolder(uint hash)
    {
        return GetFolder(unchecked((int)hash));
    }

    public static string GetFullPath(int folderHash, int fileHash)
    {
        return $"{GetFolder(folderHash)}/{GetFilename(fileHash)}";
    }

    public static string GetFullPath(int fullHash)
    {
        if (!_fullpaths.TryGetValue(fullHash, out var ret))
            ret = $"~{fullHash:X}";
        return ret;
    }

    public static bool FileExists(int hash)
    {
        return _files.ContainsKey(hash);
    }
}