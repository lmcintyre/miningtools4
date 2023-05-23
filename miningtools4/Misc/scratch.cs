// // Directory.CreateDirectory(@"C:\Users\Liam\Desktop\icons");
// // var formathq = "ui/icon/{0:D6}/en/{1:D6}_hr1.tex";
// // var format = "ui/icon/{0:D6}/en/{1:D6}.tex";
// // for (int i = 120000; i < 130000; i++)
// // {
// //     var id = i;
// //     var folder = (i / 1000) * 1000;
// //     var pathhq = string.Format(formathq, folder, id);
// //     var path = string.Format(format, folder, id);
// //     TexFile? data = null;
// //     try
// //     {
// //         data = latest.GetFile<TexFile>(pathhq) ?? latest.GetFile<TexFile>(path);    
// //     } catch (Exception) {}
// //     
// //     if (data == null) continue;
// //     
// //     Util.WriteImage(data, $@"C:\Users\Liam\Desktop\icons\{i}.png");    
// // }
//
// // var csvPath = @"D:\downloads\export (1).csv";
// // var outPath = @"C:\Users\Liam\Desktop\icons\uld\";
// // foreach (var line in File.ReadAllLines(csvPath))
// // {
// //     var lineSplit = line.Split(',');
// //     var path = lineSplit[2];
// //     if (!path.StartsWith("ui/uld") || !path.EndsWith(".tex")) continue;
// //     var fileName = path[(path.LastIndexOf('/') + 1)..];
// //     var file = latest.GetFile<TexFile>(path);
// //     if (file == null) continue;
// //     Util.WriteImage(file, $@"C:\Users\Liam\Desktop\icons\uld\{fileName}.png");
// // }
//
// var sheet = latest.GetExcelSheet<Status>().ToList();
// var iconNames = new SortedDictionary<ushort, List<String>>();
// foreach (var status in sheet)
// {
//     if (!iconNames.TryGetValue(status.Icon, out var nameList))
//     {
//         iconNames[status.Icon] = new List<string> { status.Name.RawString };
//     }
//     else
//     {
//         nameList.Add(status.Name.RawString);
//     }
// }
//
//
// foreach (var iconInfo in iconNames)
// {
//     Console.Write($"{iconInfo.Key}: ");
//     var length = iconInfo.Value.Count;
//     for (int i = 0; i < length; i++)
//     {
//         Console.Write(iconInfo.Value[i]);                    
//         if (i != length - 1)
//             Console.Write(", ");
//     }
//     Console.WriteLine();
// }

// var statusSheet = latest.GetExcelSheet<Status>();
// var root = @"C:\Users\Liam\Desktop\a\";
// var formathq = "ui/icon/{0:D6}/{1:D6}_hr1.tex";
// var format = "ui/icon/{0:D6}/{1:D6}.tex";
// foreach (var statusId in status)
// {
//     var id = statusSheet.GetRow(statusId).Icon;
//     var folder = (id / 1000) * 1000;
//     var pathhq = string.Format(formathq, folder, id);
//     var path = string.Format(format, folder, id);
//     TexFile? data = null;
//     try
//     {
//         data = latest.GetFile<TexFile>(pathhq) ?? latest.GetFile<TexFile>(path);    
//     } catch (Exception) {}
//     
//     if (data == null) continue;
//     
//     Util.WriteImage(data, $@"C:\Users\Liam\Desktop\a\{statusId}.png");    
// }

// var skl = latest.GetFile("chara/monster/m0790/skeleton/base/b0001/skl_m0790b0001.sklb");
// File.WriteAllBytes(@"C:\Users\Liam\Desktop\a\skl_m0790b0001.sklb",skl.Data);