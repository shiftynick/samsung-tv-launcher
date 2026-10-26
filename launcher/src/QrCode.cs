using System;
using System.Text;

// Minimal QR encoder for the Phone screen: byte mode, version 3, error
// correction M, mask 0. That holds 42 bytes, enough for the companion
// address plus pairing code. Layout follows ISO/IEC 18004.
public static class QrCode
{
 public const int Version=3, Size=17+4*Version, DataCodewords=44, EccCodewords=26, MaxBytes=42;

 // Returns modules[row,col], true for dark.
 public static bool[,] Encode(string text)
 {
  byte[] bytes=Encoding.UTF8.GetBytes(text);
  if(bytes.Length>MaxBytes)throw new ArgumentException("Text is too long for the QR code.",nameof(text));
  var bits=new System.Collections.Generic.List<bool>();
  void Append(int value,int count){for(int i=count-1;i>=0;i--)bits.Add(((value>>i)&1)!=0);}
  Append(0b0100,4);Append(bytes.Length,8);foreach(byte b in bytes)Append(b,8);
  Append(0,Math.Min(4,DataCodewords*8-bits.Count));
  while(bits.Count%8!=0)bits.Add(false);
  var data=new byte[DataCodewords+EccCodewords];
  for(int i=0;i<bits.Count;i++)if(bits[i])data[i>>3]|=(byte)(0x80>>(i&7));
  for(int i=bits.Count/8,pad=0;i<DataCodewords;i++,pad++)data[i]=(byte)(pad%2==0?0xEC:0x11);
  byte[] ecc=Remainder(data,DataCodewords,Divisor(EccCodewords));
  Array.Copy(ecc,0,data,DataCodewords,EccCodewords);

  var dark=new bool[Size,Size];var function=new bool[Size,Size];
  void Set(int row,int col,bool value){dark[row,col]=value;function[row,col]=true;}
  for(int i=0;i<Size;i++){Set(6,i,i%2==0);Set(i,6,i%2==0);}
  foreach(var (r0,c0) in new[]{(3,3),(3,Size-4),(Size-4,3)})
   for(int dr=-4;dr<=4;dr++)for(int dc=-4;dc<=4;dc++){
    int r=r0+dr,c=c0+dc,d=Math.Max(Math.Abs(dr),Math.Abs(dc));
    if(r>=0&&r<Size&&c>=0&&c<Size)Set(r,c,d!=2&&d!=4);
   }
  for(int dr=-2;dr<=2;dr++)for(int dc=-2;dc<=2;dc++)Set(22+dr,22+dc,Math.Max(Math.Abs(dr),Math.Abs(dc))!=1);
  // Reserve format areas now; their bits are written after masking.
  DrawFormat(Set,0);

  int bit=0;
  for(int right=Size-1;right>=1;right-=2){
   if(right==6)right=5;
   for(int vert=0;vert<Size;vert++)for(int j=0;j<2;j++){
    int col=right-j;bool upward=((right+1)&2)==0;int row=upward?Size-1-vert:vert;
    if(function[row,col])continue;
    if(bit<data.Length*8)dark[row,col]=((data[bit>>3]>>(7-(bit&7)))&1)!=0;
    bit++;
   }
  }
  for(int r=0;r<Size;r++)for(int c=0;c<Size;c++)if(!function[r,c]&&(r+c)%2==0)dark[r,c]=!dark[r,c];
  DrawFormat(Set,0);
  return dark;
 }
 static void DrawFormat(Action<int,int,bool> set,int mask)
 {
  int data=(0b00<<3)|mask; // error correction M
  int rem=data;for(int i=0;i<10;i++)rem=(rem<<1)^((rem>>9)*0x537);
  int bits=((data<<10)|rem)^0x5412;
  bool Bit(int i)=>((bits>>i)&1)!=0;
  for(int i=0;i<=5;i++)set(i,8,Bit(i));
  set(7,8,Bit(6));set(8,8,Bit(7));set(8,7,Bit(8));
  for(int i=9;i<15;i++)set(8,14-i,Bit(i));
  for(int i=0;i<8;i++)set(8,Size-1-i,Bit(i));
  for(int i=8;i<15;i++)set(Size-15+i,8,Bit(i));
  set(Size-8,8,true);
 }
 static byte[] Divisor(int degree)
 {
  var result=new byte[degree];result[degree-1]=1;int root=1;
  for(int i=0;i<degree;i++){
   for(int j=0;j<degree;j++){result[j]=Multiply(result[j],root);if(j+1<degree)result[j]^=result[j+1];}
   root=Multiply(root,2);
  }
  return result;
 }
 static byte[] Remainder(byte[] data,int length,byte[] divisor)
 {
  var result=new byte[divisor.Length];
  for(int k=0;k<length;k++){
   int factor=data[k]^result[0];
   Array.Copy(result,1,result,0,result.Length-1);result[result.Length-1]=0;
   for(int i=0;i<result.Length;i++)result[i]^=Multiply(divisor[i],factor);
  }
  return result;
 }
 static byte Multiply(int x,int y)
 {
  int z=0;for(int i=7;i>=0;i--){z=(z<<1)^((z>>7)*0x11D);z^=((y>>i)&1)*x;}
  return (byte)z;
 }
}
