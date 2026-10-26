using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed class PairedPhone
{
 public string Id {get;set;}
 public string Name {get;set;}
 public string TokenHash {get;set;}
 public string Created {get;set;}
}

// Pairing and bearer tokens for the phone companion. A code exists only while
// the Phone screen is open on the TV, so pairing needs someone at the remote.
// Tokens are stored as SHA-256 hashes in the launcher's own data directory.
public sealed class CompanionAccess
{
 public const int MaxPhones=8, MaxAttempts=5;
 readonly object gate=new object();
 readonly string path;
 List<PairedPhone> phones=new List<PairedPhone>();
 string code;
 int attempts;
 public string LastPaired {get;private set;}="";
 public CompanionAccess(string path){this.path=path;phones=Load(path);}
 public string Code{get{lock(gate)return code;}}
 public bool Locked{get{lock(gate)return code==null&&attempts>=MaxAttempts;}}
 public int PhoneCount{get{lock(gate)return phones.Count;}}
 public string NewCode(){lock(gate){code=RandomNumberGenerator.GetInt32(0,1000000).ToString("D6");attempts=0;return code;}}
 public void ClearCode(){lock(gate){code=null;attempts=0;}}
 // Returns a new token, or null when the code is wrong, expired or exhausted.
 public string Pair(string offered,string name)
 {
  lock(gate){
   if(code==null||offered==null)return null;
   if(!FixedTimeEquals(offered.Trim(),code)){if(++attempts>=MaxAttempts)code=null;return null;}
   code=null;attempts=0;
   name=CleanName(name);
   string token=Base64Url(RandomNumberGenerator.GetBytes(32));
   var next=phones.ToList();
   while(next.Count>=MaxPhones)next.RemoveAt(0);
   next.Add(new PairedPhone{Id=Base64Url(RandomNumberGenerator.GetBytes(6)),Name=name,TokenHash=Hash(token),Created=DateTime.UtcNow.ToString("o")});
   Save(path,next);phones=next;LastPaired=name;
   return token;
  }
 }
 public PairedPhone Authenticate(string token)
 {
  if(string.IsNullOrEmpty(token)||token.Length>128)return null;
  string hash=Hash(token);
  lock(gate){PairedPhone match=null;foreach(var phone in phones)if(FixedTimeEquals(phone.TokenHash,hash))match=phone;return match;}
 }
 public bool Forget(string id){lock(gate){var next=phones.Where(p=>p.Id!=id).ToList();if(next.Count==phones.Count)return false;Save(path,next);phones=next;return true;}}
 public void ForgetAll(){lock(gate){Save(path,new List<PairedPhone>());phones=new List<PairedPhone>();LastPaired="";}}
 public static string CleanName(string name)
 {
  name=Regex.Replace(name??"","[^A-Za-z0-9 .'_-]","").Trim();
  if(name.Length>32)name=name.Substring(0,32).Trim();
  return name.Length==0?"Phone":name;
 }
 static string Hash(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
 static string Base64Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
 static bool FixedTimeEquals(string a,string b)=>CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a),Encoding.UTF8.GetBytes(b));
 static List<PairedPhone> Load(string path)
 {
  try{
   if(!File.Exists(path)||new FileInfo(path).Length>65536)return new List<PairedPhone>();
   var list=JsonSerializer.Deserialize<List<PairedPhone>>(File.ReadAllText(path))??new List<PairedPhone>();
   return list.Where(p=>p!=null&&p.Id!=null&&p.TokenHash!=null&&Regex.IsMatch(p.TokenHash,@"\A[0-9A-F]{64}\z")).Select(p=>{p.Name=CleanName(p.Name);return p;}).TakeLast(MaxPhones).ToList();
  }catch(JsonException){return new List<PairedPhone>();}
  catch(IOException){return new List<PairedPhone>();}
  catch(UnauthorizedAccessException){return new List<PairedPhone>();}
 }
 static void Save(string path,List<PairedPhone> list)
 {
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
  string staging=path+".new";
  try{File.WriteAllText(staging,JsonSerializer.Serialize(list));File.Move(staging,path,true);}
  finally{if(File.Exists(staging))File.Delete(staging);}
 }
}
