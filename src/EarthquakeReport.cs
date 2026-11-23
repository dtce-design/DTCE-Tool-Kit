using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* The complete earthquake load report (owner, 23 November 2026: "include mo din naman yung complete report sa earthquake"; "complete
   calculation lahat sa design sheets reports, including diagrams, illustrations, everything"): the site and its fault on the map, the
   historical earthquakes, the seismic source type, every coefficient with its table row and interpolation, the spectrum with T0 / Ts,
   the STAAD scale, the ordinates and the STAAD input. */
partial class RsaCurveForm {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 protected override CalculationDocument BuildDocument(){var c=Compute();var rep=new RichReport("Earthquake Load and Design Response Spectrum - NSCP 2015");
  string soil=NscpSpectrum.Soils[c.Soil];
  rep.Heading("1  DESIGN BASIS").Paragraph("NSCP 2015 Section 208 (Earthquake Loads): design response spectrum of Figure 208-3 from the seismic zone (Table 208-1 / Figure 208-1), the soil profile type (Table 208-2), the seismic source type (Table 208-3) and the near-source factors Na / Nv (Tables 208-4, 208-5), giving Ca and Cv (Tables 208-7, 208-8). The spectrum is scaled for STAAD.Pro response spectrum analysis with g × I / R.");
  /* 2 site */
  rep.Heading("2  SITE");var site=new List<string[]>{new[]{"Coordinates",Value("Latitude")==""?"(not entered)":Value("Latitude")+" N, "+Value("Longitude")+" E"}};
  if(fault!=null){site.Add(new[]{"Location",fault.Location});site.Add(new[]{"Nearest active fault (HazardHunterPH)",fault.NearestText+(fault.DateMapped>0?" (mapped "+fault.DateMapped+")":"")});site.Add(new[]{"Ground rupture / shaking / liquefaction",fault.GroundRupture+" / "+fault.GroundShaking+" / "+fault.Liquefaction});site.Add(new[]{"Source",fault.Source+", retrieved "+fault.Retrieved});}
  else site.Add(new[]{"Nearest active fault","not looked up - distance and source type entered by the user"});
  site.Add(new[]{"Seismic zone","Zone "+c.Zone+" (Z = "+(c.Zone==4?"0.40":"0.20")+")"});site.Add(new[]{"Soil profile",Value("Soil")});site.Add(new[]{"Occupancy",Value("Importance")});rep.KeyValues(site);
  double lat,lon;if(Site(out lat,out lon)){var ov=map.Overlay;var ex=map.Extra;var est=estimate;var qs=quakes.ToList();double radius=map.RadiusKm;
   rep.Figure(330,(g,rc)=>{int w=1400,h=Math.Max(300,(int)(w*rc.Height/rc.Width));using(var v=new EarthquakeMapView{Width=w,Height=h,Lat=lat,Lon=lon,RadiusKm=radius,Quakes=qs,Fault=est,Overlay=ov,Extra=ex})using(var bmp=new Bitmap(w,h)){using(var bg=Graphics.FromImage(bmp))v.Paint(bg,false);var st=g.Save();g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(bmp,rc);g.Restore(st);}},
    "Figure 1 - site (centre), active fault traces (the site's fault highlighted) and historical earthquakes within "+F(radius,"0")+" km: circle size by magnitude, colour by depth (red < 70 km, orange < 300 km, blue deeper)");}
  /* 3 seismic source */
  rep.Heading("3  SEISMIC SOURCE");double mm,sr,L;bool hasM=double.TryParse(Value("Mmax"),NumberStyles.Float,Ci,out mm),hasS=double.TryParse(Value("SlipRate"),NumberStyles.Float,Ci,out sr),hasL=double.TryParse(Value("RuptureLength"),NumberStyles.Float,Ci,out L)&&L>0;
  if(estimate!=null)rep.Paragraph(estimate.Basis,true);
  if(hasL)rep.Paragraph("Maximum magnitude from the surface rupture length (Wells and Coppersmith 1994, all slip types): Mmax = 5.08 + 1.16 log10(L) = 5.08 + 1.16 × log10("+F(L,"0.0")+") = "+F(FaultTraces.Mmax(L),"0.00")+"   (σ = 0.28)"+(hasM&&Math.Abs(FaultTraces.Mmax(L)-mm)>0.005?"; Mmax used = "+F(mm,"0.0#")+" (entered).":"."));
  rep.Table(new[]{"Source type","Description (Table 208-3)","Mmax","Slip rate SR (mm/yr)"},new List<string[]>{new[]{"A","Faults capable of large magnitude events with a high rate of activity","M ≥ 7.0","SR ≥ 5"},new[]{"B","All faults other than types A and C","-","-"},new[]{"C","Faults not capable of large magnitude earthquakes, low rate of activity","M < 6.5","SR ≤ 2"}},new[]{.9f,3.2f,.9f,1.3f},row=>row[0]==c.Source?(Color?)Color.FromArgb(226,240,234):null);
  if(hasM)rep.Paragraph("Site fault: Mmax = "+F(mm,"0.0#")+(hasS?", slip rate = "+F(sr,"0.##")+" mm/yr":", slip rate not known")+"  →  seismic source type "+c.Source+". "+FaultTraces.Sensitivity(mm,hasS?sr:double.NaN),false,true);else rep.Paragraph("Seismic source type "+c.Source+" (entered).",false,true);
  if(quakes.Count>0){rep.Paragraph("Historical earthquakes: "+quakes.Count+" events of M"+Value("MinMagnitude")+"+ within "+Value("Radius")+" km since "+Value("FromYear")+" (USGS ANSS Comprehensive Catalog). The 15 largest:");
   rep.Table(new[]{"Date (UTC)","M","Depth km","Distance km","Place"},quakes.OrderByDescending(q=>q.Magnitude).Take(15).Select(q=>new[]{q.TimeUtc.ToString("yyyy-MM-dd",Ci),F(q.Magnitude,"0.0")+(q.MagType==""?"":" "+q.MagType),double.IsNaN(q.DepthKm)?"":F(q.DepthKm,"0"),F(q.DistanceKm,"0.0"),q.Place}).ToList(),new[]{1.2f,.8f,.8f,.9f,3.4f});
   var qs=quakes.ToList();double faultKm=c.Km;rep.Figure(190,(g,rc)=>MagnitudeDistance(g,rc,qs,faultKm),"Figure 2 - magnitude against distance from the site of the historical earthquakes; dashed line = distance to the site's fault");}
  /* 4 coefficients */
  rep.Heading("4  SEISMIC COEFFICIENTS");
  if(c.Zone==4){rep.Paragraph("Near-source factors for source type "+c.Source+" at "+F(c.Km,"0.##")+" km, interpolated linearly between the tabulated distances:");
   rep.Table(new[]{"Factor","≤ 2 km","5 km","10 km","≥ 15 km","At "+F(c.Km,"0.##")+" km"},new List<string[]>{new[]{"Na (Table 208-4)",F(NscpSpectrum.Na(c.Source,2),"0.0#"),F(NscpSpectrum.Na(c.Source,5),"0.0#"),F(NscpSpectrum.Na(c.Source,10),"0.0#"),F(NscpSpectrum.Na(c.Source,15),"0.0#"),F(c.Na,"0.###")},new[]{"Nv (Table 208-5)",F(NscpSpectrum.Nv(c.Source,2),"0.0#"),F(NscpSpectrum.Nv(c.Source,5),"0.0#"),F(NscpSpectrum.Nv(c.Source,10),"0.0#"),F(NscpSpectrum.Nv(c.Source,15),"0.0#"),F(c.Nv,"0.###")}},new[]{1.6f,.8f,.8f,.8f,.8f,1f});
   rep.Paragraph(Interp("Na",c.Source,c.Km,c.Na,true));rep.Paragraph(Interp("Nv",c.Source,c.Km,c.Nv,false));
   string src=c.Source;double km=c.Km,na=c.Na,nv=c.Nv;rep.Figure(170,(g,rc)=>NearSource(g,rc,src,km,na,nv),"Figure 3 - near-source factors Na and Nv for source type "+c.Source+" against the distance; the site at "+F(c.Km,"0.##")+" km");}
  else rep.Paragraph("Zone 2: the near-source factors do not apply (Na = Nv = 1.0).");
  var rows=new List<string[]>();for(int k=0;k<NscpSpectrum.Soils.Length;k++)rows.Add(new[]{NscpSpectrum.Soils[k],F(NscpSpectrum.Ca(c.Zone,k,1),"0.##")+(c.Zone==4?" Na":""),F(NscpSpectrum.Cv(c.Zone,k,1),"0.##")+(c.Zone==4?" Nv":""),k==c.Soil?F(c.Ca,"0.###"):"",k==c.Soil?F(c.Cv,"0.###"):""});
  rep.Table(new[]{"Soil profile","Ca (Table 208-7)","Cv (Table 208-8)","Ca used","Cv used"},rows,new[]{1.1f,1.3f,1.3f,1f,1f},row=>row[0]==soil?(Color?)Color.FromArgb(226,240,234):null);
  rep.Paragraph("Ca = "+(c.Zone==4?F(NscpSpectrum.Ca(4,c.Soil,1),"0.##")+" Na = "+F(NscpSpectrum.Ca(4,c.Soil,1),"0.##")+" × "+F(c.Na,"0.###")+" = ":"")+F(c.Ca,"0.####")+";   Cv = "+(c.Zone==4?F(NscpSpectrum.Cv(4,c.Soil,1),"0.##")+" Nv = "+F(NscpSpectrum.Cv(4,c.Soil,1),"0.##")+" × "+F(c.Nv,"0.###")+" = ":"")+F(c.Cv,"0.####"),false,true);
  /* 5 spectrum */
  rep.Heading("5  DESIGN RESPONSE SPECTRUM (FIGURE 208-3)");
  rep.Table(new[]{"Quantity","Formula","Value"},new List<string[]>{new[]{"Ts","Cv / (2.5 Ca) = "+F(c.Cv,"0.####")+" / (2.5 × "+F(c.Ca,"0.####")+")",F(c.Ts,"0.####")+" s"},new[]{"T0","0.2 Ts = 0.2 × "+F(c.Ts,"0.####"),F(c.T0,"0.####")+" s"},new[]{"Sa, 0 ≤ T < T0","Ca (1 + 1.5 T / T0)",F(c.Ca,"0.###")+" → "+F(2.5*c.Ca,"0.###")+" g"},new[]{"Sa, T0 ≤ T ≤ Ts","2.5 Ca = 2.5 × "+F(c.Ca,"0.####"),F(2.5*c.Ca,"0.####")+" g"},new[]{"Sa, T > Ts","Cv / T","e.g. T = 1 s: "+F(c.Cv,"0.####")+" g"}},new[]{1.2f,3f,1.4f});
  var pts=c.Points;double t0=c.T0,ts=c.Ts;rep.Figure(230,(g,rc)=>SpectrumChart.Render(pts,t0,ts,g,rc),"Figure 4 - design response spectrum, Sa in g against the period T; plateau 2.5Ca = "+F(2.5*c.Ca,"0.###")+" g between T0 and Ts");
  /* 6 STAAD scale */
  rep.Heading("6  STAAD SCALE FACTOR");rep.KeyValues(new[]{new[]{"Importance factor I",F(c.I,"0.##")+" ("+Value("Importance")+", Table 208-1)"},new[]{"Response modification R",F(c.R,"0.##")+" (Table 208-11)"},new[]{"Additional factor",F(c.Extra,"0.####")+" (dynamic / static base-shear ratio, Section 208.5.4.5)"},new[]{"g",F(c.G,"0.#####")+(c.G>100?" mm/s²":" m/s²")},new[]{"ACC SCALE = g × I / R × factor",F(c.G,"0.#####")+" × "+F(c.I,"0.##")+" / "+F(c.R,"0.##")+" × "+F(c.Extra,"0.####")+" = "+F(c.Scale,"0.######")},new[]{"Damping / combination",F(c.Damping,"0.###")+" / "+Value("Method")}});
  /* 7 ordinates */
  rep.Heading("7  SPECTRUM ORDINATES");var ord=new List<string[]>();int per=(c.Points.Count+3)/4;for(int r=0;r<per;r++){var row=new List<string>();for(int col=0;col<4;col++){int k=col*per+r;row.Add(k<c.Points.Count?F(c.Points[k][0],"0.000"):"");row.Add(k<c.Points.Count?F(c.Points[k][1],"0.0000"):"");}ord.Add(row.ToArray());}
  rep.Table(new[]{"T (s)","Sa (g)","T (s)","Sa (g)","T (s)","Sa (g)","T (s)","Sa (g)"},ord,new[]{1f,1f,1f,1f,1f,1f,1f,1f});
  /* 8 STAAD input */
  rep.Heading("8  STAAD INPUT");foreach(var line in Staad(c).Split('\n'))rep.Paragraph(line.TrimEnd('\r'),line.StartsWith("*"));
  rep.Heading("9  NOTES").Paragraph("Section 208.5.4.5: scale the dynamic base shear to not less than the static base shear requirement (90% for regular / 100% for irregular structures) and enter that ratio as the additional factor. Soil profile SF and structures needing a site-specific spectrum are outside this sheet. Verify the seismic source type and distance against the PHIVOLCS active fault map. This sheet prepares analysis input; it is not an engineer's approval.",true);
  rep.Signatures("Prepared by","Checked by");return rep.Build();}
 static string Interp(string name,string source,double km,double value,bool na){double[] at=na?new double[]{2,5,10}:new double[]{2,5,10,15};Func<double,double> f=d=>na?NscpSpectrum.Na(source,d):NscpSpectrum.Nv(source,d);
  if(km<=at[0])return name+" = "+F(value,"0.###")+" (distance ≤ 2 km).";if(km>=at[at.Length-1])return name+" = "+F(value,"0.###")+" (distance ≥ "+F(at[at.Length-1],"0")+" km).";
  for(int i=1;i<at.Length;i++)if(km<=at[i])return name+" = "+F(f(at[i-1]),"0.##")+" + ("+F(f(at[i]),"0.##")+" - "+F(f(at[i-1]),"0.##")+") × ("+F(km,"0.##")+" - "+F(at[i-1],"0")+") / ("+F(at[i],"0")+" - "+F(at[i-1],"0")+") = "+F(value,"0.###");return name+" = "+F(value,"0.###");}
 static readonly Color Ink=Color.FromArgb(40,52,66),Grid=Color.FromArgb(220,225,230);
 static void Axes(Graphics g,RectangleF plot,double xMax,double yMax,string xLabel,string yLabel,string xf,string yf){using(var f=new Font("Segoe UI",7.5f))using(var ink=new SolidBrush(Ink))using(var grid=new Pen(Grid)){
  for(int i=0;i<=5;i++){float x=plot.Left+plot.Width*i/5,y=plot.Bottom-plot.Height*i/5;g.DrawLine(grid,x,plot.Top,x,plot.Bottom);g.DrawLine(grid,plot.Left,y,plot.Right,y);g.DrawString((xMax*i/5).ToString(xf,Ci),f,ink,x-8,plot.Bottom+3);g.DrawString((yMax*i/5).ToString(yf,Ci),f,ink,plot.Left-30,y-6);}
  g.DrawString(xLabel,f,ink,plot.Left+plot.Width/2-40,plot.Bottom+16);g.DrawString(yLabel,f,ink,plot.Left-30,plot.Top-16);using(var axis=new Pen(Ink,1))g.DrawRectangle(axis,plot.X,plot.Y,plot.Width,plot.Height);}}
 static void MagnitudeDistance(Graphics g,RectangleF rc,List<QuakeEvent> qs,double faultKm){g.SmoothingMode=SmoothingMode.AntiAlias;var plot=new RectangleF(rc.X+50,rc.Y+24,rc.Width-80,rc.Height-58);
  double xMax=Math.Max(10,Math.Ceiling(qs.Max(q=>q.DistanceKm)/10)*10),yMin=Math.Floor(qs.Min(q=>q.Magnitude)),yTop=Math.Ceiling(qs.Max(q=>q.Magnitude))+0.5;double ySpan=Math.Max(1,yTop-yMin);
  Axes(g,plot,xMax,ySpan,"Distance from the site (km)","M - "+F(yMin,"0"),"0","0.0");
  foreach(var q in qs){float x=plot.Left+(float)(q.DistanceKm/xMax)*plot.Width,y=plot.Bottom-(float)((q.Magnitude-yMin)/ySpan)*plot.Height;float r=Math.Min(9,EarthquakeMapView.Radius(q.Magnitude)*0.6f);using(var b=new SolidBrush(Color.FromArgb(170,EarthquakeMapView.DepthColor(q.DepthKm))))g.FillEllipse(b,x-r,y-r,2*r,2*r);}
  if(faultKm>=0&&faultKm<=xMax){float fx=plot.Left+(float)(faultKm/xMax)*plot.Width;using(var p=new Pen(Color.FromArgb(25,70,210),1.4f){DashStyle=DashStyle.Dash})g.DrawLine(p,fx,plot.Top,fx,plot.Bottom);using(var f=new Font("Segoe UI",7.5f))g.DrawString("site's fault "+F(faultKm,"0.#")+" km",f,Brushes.MidnightBlue,fx+3,plot.Top+2);}}
 static void NearSource(Graphics g,RectangleF rc,string source,double km,double na,double nv){g.SmoothingMode=SmoothingMode.AntiAlias;var plot=new RectangleF(rc.X+50,rc.Y+22,rc.Width-180,rc.Height-54);double xMax=20,yMax=2.2;
  Axes(g,plot,xMax,yMax,"Distance to the seismic source (km)","factor","0","0.0#");Func<double,float> X=d=>plot.Left+(float)(d/xMax)*plot.Width;Func<double,float> Y=v=>plot.Bottom-(float)(v/yMax)*plot.Height;
  foreach(var s in new[]{new{N="Na",C=Color.FromArgb(39,110,160),F=(Func<double,double>)(d=>NscpSpectrum.Na(source,d)),V=na},new{N="Nv",C=Color.FromArgb(200,90,40),F=(Func<double,double>)(d=>NscpSpectrum.Nv(source,d)),V=nv}}){
   var pts=Enumerable.Range(0,201).Select(i=>i*xMax/200).Select(d=>new PointF(X(d),Y(s.F(d)))).ToArray();using(var p=new Pen(s.C,2))g.DrawLines(p,pts);
   if(km<=xMax){var at=new PointF(X(km),Y(s.V));using(var b=new SolidBrush(s.C))g.FillEllipse(b,at.X-4,at.Y-4,8,8);using(var f=new Font("Segoe UI",7.5f,FontStyle.Bold))using(var b=new SolidBrush(s.C))g.DrawString(s.N+" = "+F(s.V,"0.###"),f,b,at.X+6,at.Y-14);}
   using(var f=new Font("Segoe UI",8f,FontStyle.Bold))using(var b=new SolidBrush(s.C))g.DrawString(s.N+" (source "+source+")",f,b,plot.Right+12,s.N=="Na"?plot.Top+10:plot.Top+30);}}
}
}
