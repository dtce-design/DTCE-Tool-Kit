using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.IO;using System.Net;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class WindColors {
 public static Color Roughness(string value){return value=="B"?Color.FromArgb(35,139,79):value=="C"?Color.FromArgb(208,153,18):value=="D"?Color.FromArgb(32,115,210):Color.Gray;}
 public static Color Cover(int value){return Roughness(WindAssessment.Roughness(value));}
 public const string Legend="Mapped roughness: B trees/built-up (green) · C open terrain (gold) · D water (blue) · ? no data (gray). Direction labels show suggested exposure separately.";
}
// Interactive slippy map, fetching only the tiles visible in the current viewport.
partial class WindSiteMap:Control {
 readonly Dictionary<string,Bitmap> tiles=new Dictionary<string,Bitmap>();readonly HashSet<string> loading=new HashSet<string>();
 readonly SemaphoreSlim connections=new SemaphoreSlim(3);readonly CancellationTokenSource lifetime=new CancellationTokenSource();
 readonly System.Windows.Forms.Timer refresh=new System.Windows.Forms.Timer{Interval=220};
 readonly string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DTCE Tool Kit","SiteMap");
 double latitude=12.8,longitude=121.5,siteLat=double.NaN,siteLon;int zoom=5;Point drag;double dragX,dragY;bool dragging,imagery;WindTerrain terrain;Bitmap roughness;double referenceHeight=10;public bool ShowRoughness=true,ShowDirections=true;/* owner (24 November 2026, "hindi makita yung color"): the roughness zones were drawn at 13% and blurred to pale tan / blue; now about 30% with crisp cells, the map still readable underneath */const int RoughnessAlpha=78;string message="Enter coordinates to zoom to your project site.";
 public WindSiteMap(){DoubleBuffered=true;ResizeRedraw=true;TabStop=true;Cursor=Cursors.Hand;BackColor=ModernUi.Soft;MinimumSize=new Size(250,250);AccessibleName="Project site map; drag to pan, mouse wheel or plus/minus to zoom";
  refresh.Tick+=delegate{refresh.Stop();RequestTiles();};Resize+=delegate{Queue();};
  MouseDown+=(s,e)=>{Focus();if(e.Button!=MouseButtons.Left)return;if(e.X<46&&e.Y<88){Zoom(e.Y<46?1:-1);return;}if(e.Y>VH-40&&e.X<220){imagery=!imagery;Queue();return;}if(OnHandle(e.Location)){rotating=true;Capture=true;return;}dragging=true;drag=e.Location;dragX=X(longitude,zoom);dragY=Y(latitude,zoom);Capture=true;};
  MouseMove+=(s,e)=>{if(rotating){RotateTo(e.Location,ModifierKeys);return;}Cursor=OnHandle(e.Location)?Cursors.SizeAll:Cursors.Hand;if(!dragging)return;longitude=Lon(dragX-e.X+drag.X,zoom);latitude=Lat(dragY-e.Y+drag.Y,zoom);Invalidate();};
  MouseUp+=(s,e)=>{if(rotating){rotating=false;Capture=false;return;}dragging=false;Capture=false;Queue();};MouseWheel+=(s,e)=>Zoom(e.Delta>0?1:-1);
  KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Add||e.KeyCode==Keys.Oemplus){Zoom(1);e.Handled=true;}if(e.KeyCode==Keys.Subtract||e.KeyCode==Keys.OemMinus){Zoom(-1);e.Handled=true;}};
 }
 static double X(double lon,int z){return (lon+180)/360*(256*(1<<z));}
 static double Y(double lat,int z){double p=Math.Max(-85,Math.Min(85,lat))*Math.PI/180;return (1-Math.Log(Math.Tan(p)+1/Math.Cos(p))/Math.PI)/2*(256*(1<<z));}
 static double Lon(double x,int z){return x/(256*(1<<z))*360-180;}
 static double Lat(double y,int z){return Math.Atan(Math.Sinh(Math.PI*(1-2*y/(256*(1<<z)))))*180/Math.PI;}
 void Zoom(int delta){zoom=Math.Max(3,Math.Min(18,zoom+delta));Queue();}
 public void SetSite(double lat,double lon){WindTin.Validate(lat,lon);if(double.IsNaN(siteLat)||Math.Abs(lat-siteLat)>1e-8||Math.Abs(lon-siteLon)>1e-8){latitude=siteLat=lat;longitude=siteLon=lon;zoom=14;terrain=null;message="Loading project-site map…";Queue();}}
 /* the building outline at the site, turned to the ridge alignment, so it can be matched with the satellite image (owner, 24 November
    2026: "magulo yung ridge alignment? pano ang way para maorient yung building sa windload") */
 /* the ridge azimuth in degrees (any value, owner 25 November 2026); NaN or below 0 = no outline */double buildingWidth,buildingLength,ridgeAzimuth=double.NaN;
 public void SetBuilding(double width,double length,double ridge){if(buildingWidth==width&&buildingLength==length&&(ridgeAzimuth==ridge||double.IsNaN(ridgeAzimuth)&&double.IsNaN(ridge)))return;buildingWidth=width;buildingLength=length;ridgeAzimuth=ridge;Invalidate();}
 /* the building is turned on the map itself (owner, 28 November 2026: "mejo magulo yung pag input ng rotation ng building"; chose "azimuth based
    sa map"): drag the round handle at the ridge end to set the ridge azimuth (whole degrees, Shift = 5° steps); the +X / +Z arrows of the STAAD
    model follow the chosen +X */
 public event Action<double> RidgeChanged;public double StaadXBearing=double.NaN;bool rotating;PointF handle=new PointF(-999,-999);
 public void SetStaadX(double bearing){if(StaadXBearing==bearing||double.IsNaN(StaadXBearing)&&double.IsNaN(bearing))return;StaadXBearing=bearing;Invalidate();}
 bool OnHandle(Point p){return buildingWidth>0&&buildingLength>0&&!double.IsNaN(siteLat)&&Math.Abs(p.X-handle.X)<=11&&Math.Abs(p.Y-handle.Y)<=11;}
 void RotateTo(Point p,Keys keys){var c=Pixel(siteLat,siteLon);double dx=p.X-c.X,dy=p.Y-c.Y;if(dx*dx+dy*dy<16)return;double az=Math.Atan2(dx,-dy)*180/Math.PI;az=((az%180)+180)%180;double step=(keys&Keys.Shift)!=0?5:1;az=Math.Round(az/step)*step%180;
  if(az==ridgeAzimuth)return;ridgeAzimuth=az;Invalidate();var h=RidgeChanged;if(h!=null)h(az);}
 void DrawBuilding(Graphics g){if(!(buildingWidth>0)||!(buildingLength>0))return;bool unset=!(ridgeAzimuth>=0);double az0=unset?0:ridgeAzimuth;var c=Pixel(siteLat,siteLon);double mpp=156543.03392*Math.Cos(siteLat*Math.PI/180)/Math.Pow(2,zoom)/mapScale;
  double k=Math.Max(1/mpp,46/Math.Max(buildingLength,buildingWidth));bool enlarged=k>1/mpp+1e-12;double th=az0*Math.PI/180;
  Func<double,double,PointF> P=(x,y)=>new PointF(c.X+(float)((x*Math.Cos(th)+y*Math.Sin(th))*k),c.Y-(float)((-x*Math.Sin(th)+y*Math.Cos(th))*k));
  double w=buildingWidth/2,l=buildingLength/2;var outline=new[]{P(-w,-l),P(w,-l),P(w,l),P(-w,l)};
  using(var fill=new SolidBrush(Color.FromArgb(150,255,255,255)))g.FillPolygon(fill,outline);using(var edge=new Pen(Color.FromArgb(32,43,53),2f))g.DrawPolygon(edge,outline);
  using(var ridge=new Pen(Color.FromArgb(200,40,40),2.4f){DashStyle=System.Drawing.Drawing2D.DashStyle.Dash})g.DrawLine(ridge,P(0,-l*1.25),P(0,l*1.25));
  /* STAAD axes from the centre */if(!double.IsNaN(StaadXBearing)&&StaadXBearing>=0){Action<double,string,Color> arrow=(b,name,col)=>{double t=b*Math.PI/180,len=Math.Max(w,l)*k*0.9+18;var a=new PointF(c.X+(float)(Math.Sin(t)*len),c.Y-(float)(Math.Cos(t)*len));using(var pen=new Pen(col,2.4f){CustomEndCap=new System.Drawing.Drawing2D.AdjustableArrowCap(4,5)})g.DrawLine(pen,c,a);TextRenderer.DrawText(g,name,ModernUi.Strong,new Point((int)a.X+4,(int)a.Y-8),col,Color.White);};arrow(StaadXBearing,"+X",Color.FromArgb(200,30,30));arrow(StaadXBearing+90,"+Z",Color.FromArgb(30,90,200));}
  var tip=P(0,l*1.25);handle=tip;using(var hb=new SolidBrush(PowTheme.Teal))g.FillEllipse(hb,tip.X-8,tip.Y-8,16,16);using(var hp=new Pen(Color.White,2f))g.DrawEllipse(hp,tip.X-8,tip.Y-8,16,16);
  string label=(unset?"Drag the round handle to turn the building":"Ridge "+WindBuildingLoads.RidgeName(ridgeAzimuth)+" ("+ridgeAzimuth.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"°)")+(enlarged?" (outline enlarged)":"");var size=TextRenderer.MeasureText(label,ModernUi.Strong);/* on the outer side of the ridge end, clear of the ring labels */TextRenderer.DrawText(g,label,ModernUi.Strong,new Point(tip.X<c.X?(int)tip.X-size.Width-12:(int)tip.X+12,(int)tip.Y-size.Height-2),Color.FromArgb(150,30,30),Color.White);}
 public void SetTerrain(WindTerrain value,double height=10){referenceHeight=height;if(!object.ReferenceEquals(terrain,value)){terrain=value;if(roughness!=null){roughness.Dispose();roughness=null;}if(value!=null&&value.CoverGrid!=null){var grid=value.CoverGrid;roughness=new Bitmap(grid.Size,grid.Size);for(int row=0;row<grid.Size;row++)for(int col=0;col<grid.Size;col++)roughness.SetPixel(col,row,Color.FromArgb(RoughnessAlpha,WindColors.Cover(grid.Codes[row*grid.Size+col])));latitude=value.Lat;longitude=value.Lon;zoom=Math.Max(3,Math.Min(16,(int)Math.Floor(Math.Log(Math.Max(250,Math.Min(VW,VH))*156543.03392*Math.Cos(latitude*Math.PI/180)/(2.2*value.Radius),2))));Queue();}}Invalidate();}
 public void Recenter(){if(double.IsNaN(siteLat))return;latitude=siteLat;longitude=siteLon;zoom=14;Queue();}
 void Queue(){if(IsDisposed)return;Invalidate();refresh.Stop();refresh.Start();}
 /* mapScale: 1 on screen; the report draws the tiles of a whole zoom level enlarged by 1-2 so the circle fills the view */double mapScale=1;
 PointF Pixel(double lat,double lon){return new PointF((float)((X(lon,zoom)-X(longitude,zoom))*mapScale+VW/2.0),(float)((Y(lat,zoom)-Y(latitude,zoom))*mapScale+VH/2.0));}
 string Key(int z,int x,int y){return (imagery?"imagery/":"street/")+z+"/"+x+"/"+y;}
 void RequestTiles(){if(IsDisposed||!IsHandleCreated)return;double left=X(longitude,zoom)-VW/2.0/mapScale,top=Y(latitude,zoom)-VH/2.0/mapScale;int max=1<<zoom;
  for(int row=(int)Math.Floor(top/256);row<=Math.Floor((top+VH/mapScale)/256);row++)for(int col=(int)Math.Floor(left/256);col<=Math.Floor((left+VW/mapScale)/256);col++){
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
 protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){base.OnPaint(e);DrawMap(e.Graphics,new Rectangle(0,0,VW,VH),true);}
 void DrawMap(Graphics g,Rectangle bounds,bool controls,bool requireComplete=false){bool complete=!requireComplete||CompleteVisibleTiles();g.SmoothingMode=SmoothingMode.AntiAlias;double left=X(longitude,zoom)-VW/2.0/mapScale,top=Y(latitude,zoom)-VH/2.0/mapScale;int max=1<<zoom,drawn=0;
  for(int row=(int)Math.Floor(top/256);row<=Math.Floor((top+VH/mapScale)/256);row++)for(int col=(int)Math.Floor(left/256);col<=Math.Floor((left+VW/mapScale)/256);col++){Bitmap tile;int x=((col%max)+max)%max;if(tiles.TryGetValue(Key(zoom,x,row),out tile)){g.DrawImage(tile,(float)((col*256-left)*mapScale),(float)((row*256-top)*mapScale),(float)(256*mapScale),(float)(256*mapScale));drawn++;}}
  DrawWindOverlay(g);
  if(!complete)TextRenderer.DrawText(g,"Basemap partly loaded (offline or slow connection) - missing map tiles are blank",ModernUi.Strong,new Rectangle(8,56,Math.Max(120,VW-16),22),Color.FromArgb(150,40,40),Color.White,TextFormatFlags.WordBreak);
  if(!double.IsNaN(siteLat))DrawBuilding(g);if(!double.IsNaN(siteLat)){var p=Pixel(siteLat,siteLon);using(var pen=new Pen(Color.White,5))g.DrawEllipse(pen,p.X-7,p.Y-7,14,14);using(var brush=new SolidBrush(PowTheme.Teal))g.FillEllipse(brush,p.X-6,p.Y-6,12,12);TextRenderer.DrawText(g,"PROJECT SITE",ModernUi.Strong,new Point((int)p.X+12,(int)p.Y-12),Color.FromArgb(32,43,53),Color.White);}
  if(!requireComplete&&(drawn==0||message!=""))TextRenderer.DrawText(g,message,ModernUi.Body,new Rectangle(60,18,Math.Max(100,VW-80),70),Color.FromArgb(32,43,53),Color.White,TextFormatFlags.WordBreak);
  if(controls){using(var b=new SolidBrush(Color.White))g.FillRectangle(b,10,10,32,72);TextRenderer.DrawText(g,"+",ModernUi.Heading,new Rectangle(12,10,30,35),Color.FromArgb(32,43,53));TextRenderer.DrawText(g,"−",ModernUi.Heading,new Rectangle(12,46,30,35),Color.FromArgb(32,43,53));TextRenderer.DrawText(g,"Street / Satellite  ·  zoom "+zoom,ModernUi.Body,new Rectangle(8,VH-36,220,26),Color.FromArgb(32,43,53),Color.White);}
  TextRenderer.DrawText(g,imagery?"Imagery © Esri, Maxar, Earthstar Geographics":"© OpenStreetMap contributors",ModernUi.Body,new Rectangle(6,VH-19,Math.Max(150,VW-12),18),Color.Black,Color.White,TextFormatFlags.Right|TextFormatFlags.EndEllipsis);
 }
 protected override void Dispose(bool disposing){if(disposing){lifetime.Cancel();refresh.Dispose();foreach(var tile in tiles.Values)tile.Dispose();tiles.Clear();if(roughness!=null)roughness.Dispose();}base.Dispose(disposing);}
}
}
