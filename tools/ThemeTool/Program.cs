using System;
using System.IO;
using System.Linq;
using System.Text.Json;
public static class ThemeTool
{
 static void Help()=>Console.WriteLine("ThemeTool validate PACK_DIR | list PACK_ROOT [USER_PACK_ROOT] | create ID NAME OUTPUT_DIR [BASE_PACK_DIR]");
 public static int Main(string[] args)
 {
  try{
   if(args.Length==2&&args[0]=="validate"){
    var pack=ThemePack.Load(Path.Combine(args[1],"theme.json"));Console.WriteLine("Valid: "+pack.Id+" — "+pack.Name);return 0;
   }
   if((args.Length==2||args.Length==3)&&args[0]=="list"){
    var catalog=ThemeCatalog.Discover(args.Skip(1).ToArray());
    foreach(var pack in catalog.Items)Console.WriteLine(pack.Id+"\t"+pack.Name);
    foreach(var error in catalog.Errors)Console.Error.WriteLine(error);
    return catalog.Errors.Count==0?0:1;
   }
   if((args.Length==4||args.Length==5)&&args[0]=="create"){
    if(!ThemePack.ValidId(args[1])||string.IsNullOrWhiteSpace(args[2])||args[2].Length>40||args[2].Any(char.IsControl))throw new ArgumentException("Use a lowercase ID and a display name of 1–40 characters.");
    string output=Path.GetFullPath(args[3]);if(Directory.Exists(output)||File.Exists(output))throw new IOException("Output already exists; it will not be overwritten.");
    ThemePack pack=args.Length==5?ThemePack.Load(Path.Combine(args[4],"theme.json")):ThemePack.Neutral();
    var files=pack.Icons.Values.Concat(new[]{pack.Background,pack.Preview,pack.FontFile,pack.FontLicense,pack.License}).Where(p=>p!=null).Distinct().ToArray();
    Directory.CreateDirectory(output);
    foreach(string relative in files){string target=Path.Combine(output,relative);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(Path.Combine(pack.RootDirectory,relative),target,false);}
    pack.Id=args[1];pack.Name=args[2];pack.Order=100;
    File.WriteAllText(Path.Combine(output,"theme.json"),JsonSerializer.Serialize(pack,ThemePack.JsonOptions)+"\n");
    ThemePack.Load(Path.Combine(output,"theme.json"));
    Console.WriteLine("Created: "+output);Console.WriteLine("Edit theme.json and PNG art, then validate. No launcher rebuild is required.");return 0;
   }
   Help();return 2;
  }catch(Exception error){Console.Error.WriteLine(error.Message);return 1;}
 }
}
