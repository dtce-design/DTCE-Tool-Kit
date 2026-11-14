using System;using System.Linq;using System.Text;using System.Security.Cryptography;
namespace BeamScheduleFormatter {
class PdfSecurity {
 static readonly byte[] PaddingBytes={0x28,0xbf,0x4e,0x5e,0x4e,0x75,0x8a,0x41,0x64,0,0x4e,0x56,0xff,0xfa,1,8,0x2e,0x2e,0,0xb6,0xd0,0x68,0x3e,0x80,0x2f,0x0c,0xa9,0xfe,0x64,0x53,0x69,0x7a};
 public readonly byte[] Id,Owner,User;readonly byte[] key;public const int Permissions=unchecked((int)0xfffff8c4);
 static byte[] Random(int size){var b=new byte[size];using(var r=RandomNumberGenerator.Create())r.GetBytes(b);return b;}
 static byte[] Md5(byte[] b){using(var m=MD5.Create())return m.ComputeHash(b);}
 static byte[] Join(params byte[][] arrays){return arrays.SelectMany(x=>x).ToArray();}
 static byte[] Pad(byte[] password){return password.Concat(PaddingBytes).Take(32).ToArray();}
 static byte[] Rc4(byte[] k,byte[] b){int[] s=Enumerable.Range(0,256).ToArray();int j=0;for(int i=0;i<256;i++){j=(j+s[i]+k[i%k.Length])&255;int tmp=s[i];s[i]=s[j];s[j]=tmp;}byte[] result=new byte[b.Length];int x=0;j=0;for(int n=0;n<b.Length;n++){x=(x+1)&255;j=(j+s[x])&255;int tmp=s[x];s[x]=s[j];s[j]=tmp;result[n]=(byte)(b[n]^s[(s[x]+s[j])&255]);}return result;}
 public PdfSecurity(){Id=Random(16);byte[] digest=Md5(Pad(Random(32)));for(int i=0;i<50;i++)digest=Md5(digest);Owner=Rc4(digest,PaddingBytes);for(int i=1;i<=19;i++)Owner=Rc4(digest.Select(v=>(byte)(v^i)).ToArray(),Owner);byte[] p=BitConverter.GetBytes(Permissions);if(!BitConverter.IsLittleEndian)Array.Reverse(p);digest=Md5(Join(PaddingBytes,Owner,p,Id));for(int i=0;i<50;i++)digest=Md5(digest.Take(16).ToArray());key=digest.Take(16).ToArray();byte[] u=Rc4(key,Md5(Join(PaddingBytes,Id)));for(int i=1;i<=19;i++)u=Rc4(key.Select(v=>(byte)(v^i)).ToArray(),u);User=Join(u,new byte[16]);}
 public byte[] Encrypt(byte[] bytes,int objectNumber){byte[] suffix={(byte)objectNumber,(byte)(objectNumber>>8),(byte)(objectNumber>>16),0,0,0x73,0x41,0x6c,0x54};byte[] objectKey=Md5(Join(key,suffix)).Take(16).ToArray();using(var aes=Aes.Create()){aes.Key=objectKey;aes.IV=Random(16);aes.Mode=CipherMode.CBC;aes.Padding=PaddingMode.PKCS7;using(var encryptor=aes.CreateEncryptor())return Join(aes.IV,encryptor.TransformFinalBlock(bytes,0,bytes.Length));}}
 public static string Hex(byte[] bytes){return BitConverter.ToString(bytes).Replace("-","");}
 public string Dictionary(){return "<< /Filter /Standard /V 4 /R 4 /Length 128 /O <"+Hex(Owner)+"> /U <"+Hex(User)+"> /P "+Permissions+" /EncryptMetadata true /CF << /StdCF << /CFM /AESV2 /AuthEvent /DocOpen /Length 16 >> >> /StmF /StdCF /StrF /StdCF >>";}
}
}
