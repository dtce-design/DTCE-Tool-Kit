using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.IO;using System.Net;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class WindColors {
 public static Color Roughness(string value){return value=="B"?Color.FromArgb(35,139,79):value=="C"?Color.FromArgb(208,153,18):value=="D"?Color.FromArgb(32,115,210):Color.Gray;}
 public static Color Cover(int value){return Roughness(WindAssessment.Roughness(value));}
 public const string Legend="B  Trees / built-up (green)    C  Open terrain (gold)    D  Water (blue)    ?  No data (gray)";
}
// Interactive slippy map, fetching only the tiles visible in the current viewport.
partial class WindSiteMap:Control {
 readonly Dictionary<string,Bitmap> tiles=new Dictionary<string,Bitmap>();readonly HashSet<string> loading=new HashSet<string>();
 readonly SemaphoreSlim connections=new SemaphoreSlim(3);readonly CancellationTokenSource lifetime=new CancellationTokenSource();
 readonly System.Windows.Forms.Timer refresh=new System.Windows.Forms.Timer{Interval=220};
 readonly string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DTCE Tool Kit","SiteMap");
 double latitude=12.8,longitude=121.5,siteLat=double.NaN,siteLon;int zoom=5;Point drag;double dragX,dragY;bool dragging,imagery;WindTerrain terrain;Bitmap roughness;double referenceHeight=10;public bool ShowRoughness=true,ShowDirections=true;string message="Enter coordinates to zoom to your project site.";
 public WindSiteMap(){DoubleBuffered=true;ResizeRedraw=true;TabStop=true;Cursor=Cursors.Hand;BackColor=ModernUi.Soft;MinimumSize=new Size(250,250);AccessibleName="Project site map; drag to pan, mouse wheel or plus/minus to zoom";
  refresh.Tick+=delegate{refresh.Stop();RequestTiles();};Resize+=delegate{Queue();};
  MouseDown+=(s,e)=>{Focus();if(e.Button!=MouseButtons.Left)return;if(e.X<46&&e.Y<88){Zoom(e.Y<46?1:-1);return;}if(e.Y>Height-40&&e.X<220){imagery=!imagery;Queue();return;}dragging=true;drag=e.Location;dragX=X(longitude,zoom);dragY=Y(latitude,zoom);Capture=true;};
  MouseMove+=(s,e)=>{if(!dragging)return;longitude=Lon(dragX-e.X+drag.X,zoom);latitude=Lat(dragY-e.Y+drag.Y,zoom);Invalidate();};
  MouseUp+=(s,e)=>{dragging=false;Capture=false;Queue();};MouseWheel+=(s,e)=>Zoom(e.Delta>0?1:-1);
  KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Add||e.KeyCode==Keys.Oemplus){Zoom(1);e.Handled=true;}if(e.KeyCode==Keys.Subtract||e.KeyCode==Keys.OemMinus){Zoom(-1);e.Handled=true;}};
 }
 static double X(double lon,int z){return (lon+180)/360*(256*(1<<z));}
 static double Y(double lat,int z){double p=Math.Max(-85,Math.Min(85,lat))*Math.PI/180;return (1-Math.Log(Math.Tan(p)+1/Math.Cos(p))/Math.PI)/2*(256*(1<<z));}
 static double Lon(double x,int z){return x/(256*(1<<z))*360-180;}
 static double Lat(double y,int z){return Math.Atan(Math.Sinh(Math.PI*(1-2*y/(256*(1<<z)))))*180/Math.PI;}
 void Zoom(int delta){zoom=Math.Max(3,Math.Min(18,zoom+delta));Queue();}
 public void SetSite(double lat,double lon){WindTin.Validate(lat,lon);if(double.IsNaN(siteLat)||Math.Abs(lat-siteLat)>1e-8||Math.Abs(lon-siteLon)>1e-8){latitude=siteLat=lat;longitude=siteLon=lon;zoom=14;terrain=null;message="Loading project-site map…";Queue();}}
 public void SetTerrain(WindTerrain value,double height=10){referenceHeight=height;if(!object.ReferenceEquals(terrain,value)){terrain=value;if(roughness!=null){roughness.Dispose();roughness=null;}if(value!=null&&value.CoverGrid!=null){var grid=value.CoverGrid;roughness=new Bitmap(grid.Size,grid.Size);for(int row=0;row<grid.Size;row++)for(int col=0;col<grid.Size;col++)roughness.SetPixel(col,row,Color.FromArgb(84,WindColors.Cover(grid.Codes[row*grid.Size+col])));latitude=value.Lat;longitude=value.Lon;zoom=Math.Max(3,Math.Min(16,(int)Math.Floor(Math.Log(Math.Max(250,Math.Min(Width,Height))*156543.03392*Math.Cos(latitude*Math.PI/180)/(2.2*value.Radius),2))));Queue();}}Invalidate();}
 public void Recenter(){if(double.IsNaN(siteLat))return;latitude=siteLat;longitude=siteLon;zoom=14;Queue();}
 void Queue(){if(IsDisposed)return;Invalidate();refresh.Stop();refresh.Start();}
 PointF Pixel(double lat,double lon){return new PointF((float)(X(lon,zoom)-X(longitude,zoom)+Width/2.0),(float)(Y(lat,zoom)-Y(latitude,zoom)+Height/2.0));}
 string Key(int z,int x,int y){return (imagery?"imagery/":"street/")+z+"/"+x+"/"+y;}
 void RequestTiles(){if(IsDisposed||!IsHandleCreated)return;double left=X(longitude,zoom)-Width/2.0,top=Y(latitude,zoom)-Height/2.0;int max=1<<zoom;
  for(int row=(int)Math.Floor(top/256);row<=Math.Floor((top+Height)/256);row++)for(int col=(int)Math.Floor(left/256);col<=Math.Floor((left+Width)/256);col++){
   if(row<0||row>=max)continue;int x=((col%max)+max)%max,y=row;string key=Key(zoom,x,y);if(tiles.ContainsKey(key)||loading.Contains(key))continue;loading.Add(key);LoadTile(key,zoom,x,y,imagery);
  }
 }
 async void LoadTile(string key,int z,int x,int y,bool satellite){byte[] data=null;string failure=null;
  try{await connections.WaitAsync(lifetime.Token);try{data=await Task.Run(()=>{
   string file=Path.Combine(cache,key.Replace('/','_')+".tile");if(File.Exists(file)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(file)<TimeSpan.FromDays(7))return File.ReadAllBytes(file);
   string url=satellite?"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/"+z+"/"+y+"/"+x:"https://tile.openstreetmap.org/"+z+"/"+x+"/"+y+".png";
   ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(url);request.UserAgent="DTCE-Tool-Kit/"+Engine.Version+" (desktop site map; developer@dtce.online)";request.Timeout=request.ReadWriteTimeout=12000;
   using(var cancel=lifetime.Token.Register(()=>request.Abort()))using(var response=request.GetResponse())using(var input=response.GetResponseStream())using(var output=new MemoryStream()){
    var buffer=new byte[8192];int count;while((count=input.Read(buffer,0,buffer.Length))>0){output.Write(buffer,0,count);if(output.Length>2000000)throw new IOException("Map tile too large.");}var bytes=output.ToArray();
    Directory.CreateDirectory(cache);string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,bytes);File.Copy(temp,file,true);}finally{if(File.Exists(temp))File.Delete(temp);}return bytes;
   }
  });}finally{connections.Release();}}
  catch(Exception ex){failure=ex is OperationCanceledException?null:"Map unavailable. Cached tiles remain usable; try Street / Satellite.";}
  if(IsDisposed)return;loading.Remove(key);
  if(data!=null)try{using(var stream=new MemoryStream(data))using(var original=Image.FromStream(stream)){tiles[key]=new Bitmap(original);}message="";}catch{message="Map tile could not be decoded.";}
  else if(failure!=null)message=failure;
  // Bound memory while retaining visible tiles. Disk cache survives app updates.
  if(tiles.Count>140){foreach(var old in new List<string>(tiles.Keys)){if(old==key)continue;tiles[old].Dispose();tiles.Remove(old);if(tiles.Count<=90)break;}}
  Invalidate();
 }
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);DrawMap(e.Graphics,new Rectangle(0,0,Width,Height),true);}
 public Bitmap Snapshot(){var image=new Bitmap(Math.Max(1,Width),Math.Max(1,Height));using(var g=Graphics.FromImage(image)){g.Clear(BackColor);DrawMap(g,new Rectangle(0,0,Width,Height),false);}return image;}
 void DrawMap(Graphics g,Rectangle bounds,bool controls){g.SmoothingMode=SmoothingMode.AntiAlias;double left=X(longitude,zoom)-Width/2.0,top=Y(latitude,zoom)-Height/2.0;int max=1<<zoom,drawn=0;
  for(int row=(int)Math.Floor(top/256);row<=Math.Floor((top+Height)/256);row++)for(int col=(int)Math.Floor(left/256);col<=Math.Floor((left+Width)/256);col++){Bitmap tile;int x=((col%max)+max)%max;if(tiles.TryGetValue(Key(zoom,x,row),out tile)){g.DrawImage(tile,(float)(col*256-left),(float)(row*256-top),256,256);drawn++;}}
  DrawWindOverlay(g);
  if(!double.IsNaN(siteLat)){var p=Pixel(siteLat,siteLon);using(var pen=new Pen(Color.White,5))g.DrawEllipse(pen,p.X-7,p.Y-7,14,14);using(var brush=new SolidBrush(PowTheme.Teal))g.FillEllipse(brush,p.X-6,p.Y-6,12,12);TextRenderer.DrawText(g,"PROJECT SITE",ModernUi.Strong,new Point((int)p.X+12,(int)p.Y-12),Color.FromArgb(32,43,53),Color.White);}
  if(drawn==0||message!="")TextRenderer.DrawText(g,message,ModernUi.Body,new Rectangle(60,18,Math.Max(100,Width-80),70),Color.FromArgb(32,43,53),Color.White,TextFormatFlags.WordBreak);
  if(controls){using(var b=new SolidBrush(Color.White))g.FillRectangle(b,10,10,32,72);TextRenderer.DrawText(g,"+",ModernUi.Heading,new Rectangle(12,10,30,35),Color.FromArgb(32,43,53));TextRenderer.DrawText(g,"−",ModernUi.Heading,new Rectangle(12,46,30,35),Color.FromArgb(32,43,53));TextRenderer.DrawText(g,"Street / Satellite  ·  zoom "+zoom,ModernUi.Body,new Rectangle(8,Height-36,220,26),Color.FromArgb(32,43,53),Color.White);}
  TextRenderer.DrawText(g,imagery?"Imagery © Esri, Maxar, Earthstar Geographics":"© OpenStreetMap contributors",ModernUi.Body,new Rectangle(6,Height-19,Math.Max(150,Width-12),18),Color.Black,Color.White,TextFormatFlags.Right|TextFormatFlags.EndEllipsis);
 }
 protected override void Dispose(bool disposing){if(disposing){lifetime.Cancel();refresh.Dispose();foreach(var tile in tiles.Values)tile.Dispose();tiles.Clear();if(roughness!=null)roughness.Dispose();}base.Dispose(disposing);}
}
}
