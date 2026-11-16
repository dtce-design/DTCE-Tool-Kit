using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Reflection;
namespace BeamScheduleFormatter {
static class PdfBrand {
 static Bitmap watermark;static byte[] jpeg;
 // DTCE watermark in both editions (owner decision, 16 Nov 2026).
 const string LogoResource="DtceLogo.png";
 public static Bitmap Watermark{get{if(watermark==null){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResource)){string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,LogoResource);if(stream==null&&!File.Exists(path))throw new Exception("Watermark logo missing: "+LogoResource);using(var source=stream==null?Image.FromFile(path):Image.FromStream(stream)){watermark=new Bitmap(1200,1200);using(var g=Graphics.FromImage(watermark)){g.Clear(Color.White);float scale=1120f/Math.Max(source.Width,source.Height),w=source.Width*scale,h=source.Height*scale;using(var attributes=new ImageAttributes()){var matrix=new ColorMatrix();matrix.Matrix33=(float)ReportWatermarkPattern.Opacity;attributes.SetColorMatrix(matrix);g.DrawImage(source,new Rectangle((int)((1200-w)/2),(int)((1200-h)/2),(int)w,(int)h),0,0,source.Width,source.Height,GraphicsUnit.Pixel,attributes);}}}}}return watermark;}}
 public static byte[] Jpeg{get{if(jpeg==null)using(var stream=new MemoryStream()){using(var quality=new EncoderParameters(1)){quality.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,95L);ImageCodecInfo codec=null;foreach(var candidate in ImageCodecInfo.GetImageEncoders())if(candidate.FormatID==ImageFormat.Jpeg.Guid)codec=candidate;Watermark.Save(stream,codec,quality);}jpeg=stream.ToArray();}return jpeg;}}
 public static void Draw(Graphics g){foreach(var tile in ReportWatermarkPattern.Tiles())g.DrawImage(Watermark,(float)tile[0],(float)tile[1],(float)tile[2],(float)tile[2]);}
 public static string PdfCommands(){var b=new System.Text.StringBuilder();foreach(var tile in ReportWatermarkPattern.Tiles())b.Append("q ").Append(N(tile[2])).Append(" 0 0 ").Append(N(tile[2])).Append(" ").Append(N(tile[0])).Append(" ").Append(N(841.89-tile[1]-tile[2])).Append(" cm /WM Do Q ");return b.ToString();}
 static string N(double value){return value.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);}
}
}
