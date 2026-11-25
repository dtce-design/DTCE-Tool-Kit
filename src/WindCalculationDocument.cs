using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
partial class WindDesignForm {
 protected override CalculationDocument BuildDocument(){
  // Validate first so an export cannot silently omit an invalid enabled building sheet.
  BuildReport();double lat=Number("Latitude"),lon=Number("Longitude"),z=Number("Height"),kd=Number("Kd");var wind=WindTin.At(lat,lon,Value("Category"));var doc=new CalculationDocument("Wind load / NSCP 2015");doc.ReportFile=BuildEngineeringReport();
  try {
   Bitmap site=doc.Own(siteMap.Snapshot());string category=Value("Category");bool reviewed=((System.Windows.Forms.CheckBox)Inputs["Reviewed"]).Checked;
   doc.Sheets.Add(g=>{
    CalculationDocument.Section(g,"01 / SITE & WIND-SPEED SUMMARY",130);
    using(var fill=new SolidBrush(ModernUi.Selection))g.FillRectangle(fill,CalculationDocument.L,170,CalculationDocument.CW,74);
    CalculationDocument.Text(g,"BASIC WIND SPEED",CalculationDocument.L+14,181,260,20,8,true,PowTheme.Teal);
    CalculationDocument.Text(g,wind.V.ToString("0.0",CultureInfo.InvariantCulture)+" km/h",CalculationDocument.L+14,202,300,35,23,true,PowTheme.Teal);
    CalculationDocument.Text(g,(wind.V/3.6).ToString("0.00",CultureInfo.InvariantCulture)+" m/s",CalculationDocument.L+CalculationDocument.CW*.58f,203,170,30,17,true);
    CalculationDocument.Table(g,new[]{"Parameter","Value"},new List<string[]>{new[]{"Coordinates",lat.ToString("0.######",CultureInfo.InvariantCulture)+" N, "+lon.ToString("0.######",CultureInfo.InvariantCulture)+" E"},new[]{"Occupancy / reference height","Category "+category+" / z = "+F(z)+" m"},new[]{"Kd / terrain evidence",F(kd)+" / "+(reviewed?"Reviewed by user":"Pending review")}},260,new float[]{175,348},26);
    CalculationDocument.Text(g,"PROJECT SITE MAP",CalculationDocument.L,386,CalculationDocument.CW,20,9,true);float fit=Math.Min(523f/site.Width,295f/site.Height);g.DrawImage(site,new RectangleF(CalculationDocument.L+(CalculationDocument.CW-site.Width*fit)/2,412,site.Width*fit,site.Height*fit));
    CalculationDocument.Text(g,WindColors.Legend,CalculationDocument.L,716,CalculationDocument.CW,28,7);
    CalculationDocument.Text(g,(wind.Coastal?"Nearest-contour continuation outside TIN hull. ":"Delaunay TIN interpolation. ")+"Source raster digitization is approximate; terrain/exposure are preliminary candidates requiring site review.",CalculationDocument.L,751,CalculationDocument.CW,40,8,false,PowTheme.Muted);
   });
   double fetchHeight=ExposureHeight();var directions=terrain==null?new List<WindDirection>():WindAssessment.Calculate(terrain,z,wind.V,kd,WindTin.Finite(fetchHeight)?(double?)fetchHeight:null);
   /* the surface roughness map as a circle (owner, 24 November 2026: "ipakita din yung diagram for surface roughness"; "siguro sa circle
      or sa map") */
   if(terrain!=null){var circleTerrain=terrain;var circleDirections=directions;double circleHeight=fetchHeight;doc.Sheets.Add(g=>{
    CalculationDocument.Section(g,"SURFACE ROUGHNESS AROUND THE SITE",130);
    CalculationDocument.Text(g,"Mapped surface roughness inside the assessment radius of "+F(circleTerrain.Radius)+" m (NSCP 2015 Section 207A.7.2): B trees / built-up (green), C open terrain (gold), D water (blue), no data (gray). Each 45° sector is labelled with its wind direction and the suggested exposure; dashed circles are the B fetch and the D fetch + transition for the fetch height.",CalculationDocument.L,168,CalculationDocument.CW,44,8);
    WindRoughnessCircle.Draw(g,new RectangleF(CalculationDocument.L,220,CalculationDocument.CW,CalculationDocument.CW),circleTerrain,circleDirections,-1,circleHeight,true);
    float ly=230+CalculationDocument.CW;float lx=CalculationDocument.L;foreach(var item in new[]{new[]{"B","B - trees / built-up"},new[]{"C","C - open terrain"},new[]{"D","D - water"},new[]{"?","? - no data"}}){using(var fill=new SolidBrush(WindColors.Roughness(item[0])))g.FillRectangle(fill,lx,ly+2,14,9);CalculationDocument.Text(g,item[1],lx+18,ly,110,14,8);lx+=120;}
    CalculationDocument.Text(g,"Land cover from the mapped data; it cannot prove obstruction heights. Confirm the roughness and the exposure on site.",CalculationDocument.L,ly+22,CalculationDocument.CW,30,8,false,PowTheme.Muted);});}
   doc.Sheets.Add(g=>{
    CalculationDocument.Section(g,"02 / EIGHT-DIRECTION VELOCITY-PRESSURE CALCULATIONS",130);
    var rows=directions.Select(d=>new[]{d.Name,d.Exposure,F(d.H),F(d.Lh),F(d.Kzt),F(d.Kz),F(d.Qz),d.Shape}).ToList();
    CalculationDocument.Table(g,new[]{"From","Exposure","H (m)","Lh (m)","Kzt","Kz","qz kPa","Feature"},rows,172,new float[]{40,60,60,65,70,70,80,78},40);
    CalculationDocument.Text(g,terrain==null?"Site terrain has not been loaded. Kzt, exposure and qz are unresolved.":"Terrain snapshot: "+terrain.Acquired.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'")+" / radius "+F(terrain.Radius)+" m",CalculationDocument.L,548,CalculationDocument.CW,40,8,false,PowTheme.Muted);
    CalculationDocument.Section(g,"EQUATIONS & UNITS",602);
    CalculationDocument.Text(g,"Kzt = (1 + K1 K2 K3)²\r\nKz = 2.01 [max(z, 4.57) / zg]^(2/α)\r\nqz = 0.613 Kz Kzt Kd (V / 3.6)² / 1000   [kPa]\r\n\r\nqz is velocity pressure. Building pressures also require G, Cp and GCpi. A missing terrain/exposure value remains unresolved.",CalculationDocument.L,642,CalculationDocument.CW,138,10);
   });
   var stream=typeof(WindTin).Assembly.GetManifestResourceStream("WindMap"+wind.Map+".jpg");using(stream)using(var original=Image.FromStream(stream)){
    var source=doc.Own(new Bitmap(original));doc.Sheets.Add(g=>{
     CalculationDocument.Section(g,"03 / NSCP WIND-SPEED SOURCE MAP & PROJECT LOCATION",130);
     /* inside the binding margins (1 inch left) */float h=610,w=h*source.Width/source.Height;if(w>CalculationDocument.CW){h*=CalculationDocument.CW/w;w=CalculationDocument.CW;}float x=CalculationDocument.L+(CalculationDocument.CW-w)/2,y=171;g.DrawImage(source,x,y,w,h);
     float px=x+(float)wind.X*w/3179,py=y+(float)wind.Y*h/4801;
     using(var pen=new Pen(Color.Magenta,1.5f)){g.DrawEllipse(pen,px-4,py-4,8,8);g.DrawLine(pen,px-10,py,px+10,py);g.DrawLine(pen,px,py-10,px,py+10);}
     CalculationDocument.Text(g,"Category "+category+" · source image "+wind.Map+" · magenta marker = project",CalculationDocument.L,784,CalculationDocument.CW,15,7);
    });
   }
   for(int i=0;i<directions.Count;i+=2){var pair=directions.Skip(i).Take(2).ToArray();doc.Sheets.Add(g=>{
    CalculationDocument.Section(g,"TERRAIN PROFILES / "+string.Join(" & ",pair.Select(d=>d.Name)),130);
    for(int n=0;n<pair.Length;n++)WindProfileDrawing.Draw(g,new RectangleF(CalculationDocument.L,172+n*305,CalculationDocument.CW,295),pair[n]);
   });}
   {List<WindStaadBand> staadBands=null;string staadText="";if(BuildingEnabled&&terrain!=null){var sb=Building();var sd=WindAssessment.Calculate(terrain,sb.Height,wind.V,sb.Kd,sb.Height);staadBands=WindStaadInput.WallBands(sb,wind.V,sd);staadText=StaadLines.Wrap(WindStaadInput.FaceCommands(sb,wind.V,sd,StaadBearing()));}WindAuditPages.Add(doc,directions,staadBands,staadText,terrain);}
   if(BuildingEnabled){var b=Building();string ridge=RidgeLabel();var pressure=PressureRows(b);var grouped=pressure.GroupBy(r=>r.Direction).ToArray();
    doc.Sheets.Add(g=>{
     CalculationDocument.Section(g,"MAIN WIND FORCE RESISTING SYSTEM (MWFRS) - BUILDING PRESSURE BASIS",130);
     CalculationDocument.Table(g,new[]{"Input","Value"},new List<string[]>{new[]{"Width × length",F(b.Width)+" m × "+F(b.Length)+" m"},new[]{"Eave / apex / mean roof height",F(b.Eave)+" / "+F(b.Apex)+" / "+F(b.Height)+" m"},new[]{"Roof slope / ridge alignment",F(b.Angle)+"° / "+ridge},new[]{"System / enclosure",b.System+" / "+b.Enclosure},new[]{"Gust calculation",b.AutoG?"Calculated; flexible response if n < 1 Hz":"User G = "+F(b.UserG)},new[]{"Internal pressure","Both signs: ±"+(b.Enclosure=="Enclosed"?"0.18":"0.55")}},172,new float[]{235,288},32);
     CalculationDocument.Section(g,"MWFRS - GROSS PRESSURE RANGE BY PRINCIPAL AXIS",440);
     CalculationDocument.Table(g,new[]{"Wind from / axis","Min p (kPa)","Max p (kPa)"},grouped.Select(q=>new[]{q.Key,F(q.Min(r=>r.P)),F(q.Max(r=>r.P))}).ToList(),480,new float[]{283,120,120},32);
     CalculationDocument.Text(g,"p = q G Cp − qi (GCpi)\r\nPositive pressure acts toward a surface; negative acts away. Values are individual gross surface pressures. Minimum MWFRS forces, load patterns, torsion, load combinations, components/cladding, parapets, overhangs and open roofs require separate checks. Unresolved directions are not substituted with default factors.",CalculationDocument.L,675,CalculationDocument.CW,116,9);
    });
    WindLoadCasesReport.AddPages(doc,b,pressure,ridge);
    try{WindComponentsCladding.AddPages(doc,b,WindComponentsCladding.Calculate(b,pressure,Value("CCElements"),WindAssessment.Calculate(terrain,b.Height,wind.V,b.Kd),wind.V));}catch(Exception){/* C&C shown only when it applies (h <= 18 m) */}
    foreach(var group in grouped){var values=group.ToList();for(int offset=0;offset<values.Count;offset+=16){var rows=values.Skip(offset).Take(16).ToArray();string title=group.Key;doc.Sheets.Add(g=>{
     CalculationDocument.Section(g,"MWFRS SURFACE PRESSURES / "+title,130);
     CalculationDocument.Table(g,new[]{"From","Surface","Case / zone","G","Cp","GCpi","p kPa"},rows.Select(r=>new[]{r.Direction.Split(' ')[0],r.Surface,r.Case,F(r.G),F(r.Cp),F(r.GCpi),F(r.P)}).ToList(),170,new float[]{40,85,198,40,40,50,70},34);
    });}}
    foreach(var group in grouped){var sample=group.First(r=>r.Surface=="Windward wall"&&r.Z>0);doc.Sheets.Add(g=>{
     CalculationDocument.Section(g,"PRESSURE SUBSTITUTION / "+group.Key,130);
     CalculationDocument.Text(g,"Windward wall example at z = "+F(sample.Z)+" m",CalculationDocument.L,176,CalculationDocument.CW,30,13,true);
     CalculationDocument.Table(g,new[]{"Coefficient / pressure","Value"},new List<string[]>{new[]{"Exposure / Kz",sample.Exposure+" / "+F(sample.Kz)},new[]{"Kzt at z / Kd",F(sample.Kzt)+" / "+F(b.Kd)},new[]{"V / qz",F(wind.V)+" km/h / "+F(sample.Q)+" kPa"},new[]{"G / Cp / GCpi",F(sample.G)+" / "+F(sample.Cp)+" / "+F(sample.GCpi)},new[]{"qh / qi",F(sample.Qh)+" / "+F(sample.Qi)+" kPa"}},218,new float[]{240,283},32);
     CalculationDocument.Text(g,"p = q G Cp − qi (GCpi)\r\n  = ("+F(sample.Q)+") ("+F(sample.G)+") ("+F(sample.Cp)+")\r\n    − ("+F(sample.Qi)+") ("+F(sample.GCpi)+")\r\n  = "+F(sample.P)+" kPa",CalculationDocument.L,449,CalculationDocument.CW,140,13,true);
     CalculationDocument.Text(g,"Basis: NSCP 2015 directional procedure, RCB Wind workbook Calculation sheet. qz = 0.613 Kz Kzt Kd V². Gust factor follows D31:D69. Wall Cp follows I93:I97 with L/B interpolation. Roof Cp follows Figure 207B.4-1 tables. Both internal-pressure signs and both roof Cp branches are retained as separate cases.\r\n\r\nFull numerical substitutions and profile samples are retained in Export report to Excel.",CalculationDocument.L,625,CalculationDocument.CW,154,9);
    });}
   }
   doc.Sheets.Add(g=>{CalculationDocument.Section(g,"DATA SOURCES & ENGINEERING REVIEW",130);CalculationDocument.Text(g,"WIND SPEED\r\nOwner-supplied RCB Wind v.3.3.xlsm, Figure 207A.5-1 raster maps. Delaunay TIN over digitized contour nodes. Category I = essential / highest map; II = intermediate; III–V = lower map. Registration residual and contour sampling mean this is not an exact georeferenced wind-speed dataset.\r\n\r\nTERRAIN & SURFACE ROUGHNESS\r\nAWS Terrarium elevation (~30–40 m sampling), ESA WorldCover 2021 v200 (10 m). WorldCover: https://doi.org/10.5281/zenodo.7254221 (CC BY 4.0). Eight wind-from sectors with three rays each. Land cover cannot establish obstruction heights or complete fetch coverage. Review suggested exposure and topographic feature geometry.\r\n\r\nSITE MAP\r\nStreet tiles: © OpenStreetMap contributors. Satellite tiles: Esri World Imagery, Maxar, Earthstar Geographics. Tile imagery is a location reference, not the wind-speed or land-cover dataset. Cached tiles remain in LocalAppData across updates.\r\n\r\nTOPOGRAPHY\r\nKzt = (1 + K1 K2 K3)². K1 = A min(H/Lh, 0.5); K2 = max(0, 1 − |x|/(μ Le)); K3 = exp(−γ z/Le), Le = max(Lh, 2H). Hill / ridge / escarpment candidates require review of isolation, upper-half criterion and actual terrain.\r\n\r\nAUDIT\r\nSave sheet XML keeps inputs and the numerical terrain snapshot. Export report to Excel contains the detailed numerical report. This PDF preserves the report's visual pages for printing.",CalculationDocument.L,178,CalculationDocument.CW,598,10);});
   return doc;
  }catch{doc.Dispose();throw;}
 }
}
}
