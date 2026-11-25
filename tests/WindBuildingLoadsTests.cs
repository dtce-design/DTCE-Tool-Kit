using System;using System.Linq;using System.Collections.Generic;using BeamScheduleFormatter;
class WindBuildingLoadsTests {
 static int count;static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}static void Near(double a,double b,double tolerance,string why){Check(Math.Abs(a-b)<=tolerance,why+": "+a+" vs "+b);}
 static List<WindDirection> Flat(string exposure="B"){return WindAssessment.Directions.Select(n=>new WindDirection{Name=n,Exposure=exposure,Shape="Low relief",K1=0,K2=0,K3=0,Kzt=1}).ToList();}
 static void Main(){var b=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false};var rows=WindBuildingLoads.Calculate(b,310,Flat());
  // Independent cached results from RCB Wind v.3.3.xlsm, Calculation sheet.
  // The workbook rounds Kz to .001 and pressures to Pa; this engine retains full precision.
  Near(b.Angle,9.462322208025617,1e-10,"Workbook C9 roof angle");Near(b.Height,3,1e-12,"Workbook C10 low-slope mean height");
  Near(WindBuildingLoads.Frequency(b),16.18,.005,"Workbook D17 frequency");
  Near(WindBuildingLoads.Gust(new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=true},"B",true,310),.861,.0005,"Workbook D42 normal gust");
  Near(WindBuildingLoads.Gust(new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=true},"B",false,310),.874,.0005,"Workbook D43 parallel gust");
  var wall=rows.First(r=>r.Surface=="Windward wall"&&Math.Abs(r.Z-3)<1e-8&&r.GCpi>0);Near(wall.Qh,2.222,.003,"Workbook D87 qh kPa");Near(wall.P,1.111,.003,"Workbook D114 wall positive internal");
  Near(rows.First(r=>r.Surface=="Windward wall"&&Math.Abs(r.Z-3)<1e-8&&r.GCpi<0).P,1.911,.003,"Workbook E114 negative internal sign");
  Near(rows.First(r=>r.Surface=="Leeward wall"&&r.Direction.StartsWith("E")&&r.GCpi>0).P,-1.344,.003,"Workbook C127 leeward");
  Check(rows.Select(r=>r.Direction).Distinct().Count()==4,"Four principal axes present");Check(rows.All(r=>Math.Abs(r.P-(r.Q*r.G*r.Cp-r.Qi*r.GCpi))<1e-12),"Every row satisfies net pressure equation");
  Check(rows.Where(r=>r.Surface.StartsWith("Roof zone")).All(r=>!r.Case.StartsWith("3–")&&!r.Case.StartsWith("6–")||r.Case.Contains("m from")),"Roof zones have distance ranges");
  Near(WindBuildingLoads.Leeward(1.5),-.4,1e-12,"Leeward interpolation 1–2");Near(WindBuildingLoads.Leeward(3),-.25,1e-12,"Leeward interpolation 2–4");
  Near(WindBuildingLoads.RoofNormal(20,.25,false),-.3,1e-12,"Workbook F140 roof negative branch");Near(WindBuildingLoads.RoofNormal(20,.25,true),.2,1e-12,"Workbook F141 roof positive branch");Near(WindBuildingLoads.RoofNormal(20,1,false),-.7,1e-12,"High-ratio row applies at h/L>=1");
  Near(WindBuildingLoads.RoofNormal(17.5,.375,false),-.475,1e-12,"Two-axis interpolation roof coefficients");
  Near(WindBuildingLoads.RoofZone(0,.75),-1.1,1e-12,"Roof zone height-ratio interpolation");
  Check(WindBuildingLoads.Calculate(b,310,Flat("Unresolved")).Count==0,"Missing exposure never yields pressures");
  var topography=Flat("C");foreach(var d in topography){d.Shape="Hill";d.H=20;d.Lh=60;d.X=0;var f=WindAssessment.Factors(20,60,0,3,"C","Hill");d.K1=f[0];d.K2=f[1];d.K3=f[2];d.Kzt=f[3];}
  var hill=WindBuildingLoads.Calculate(b,310,topography);Check(hill.First(r=>r.Z==0).Kzt>hill.First(r=>Math.Abs(r.Z-3)<1e-8).Kzt,"Kzt varies with wall elevation");
  b.Enclosure="Partially enclosed";b.OpeningHeight=2;rows=WindBuildingLoads.Calculate(b,310,Flat());Check(rows.All(r=>Math.Abs(Math.Abs(r.GCpi)-.55)<1e-12),"Partially enclosed internal coefficient");
  b.Eave=35;b.Apex=45;b.AutoG=true;b.FrequencyNormal=.5;b.FrequencyParallel=.7;Check(WindBuildingLoads.Calculate(b,310,Flat("C")).All(r=>WindTin.Finite(r.G)&&r.G>0),"Flexible gust response finite for both axes");
  b.Apex=10;bool rejected=false;try{WindBuildingLoads.Calculate(b,310,Flat());}catch{rejected=true;}Check(rejected,"Apex below eave rejected");
  /* ridge alignment suggestion: one exposure D direction makes the alignment facing it govern */
  {var dirs=Flat("B");dirs[2].Exposure="D";var rb=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false,RidgeDirection=1};var peaks=WindBuildingLoads.RidgePeaks(rb,310,dirs);
   Check(peaks.Length==4&&peaks.All(x=>x>0),"pressures for all four ridge alignments");Check(rb.RidgeDirection==1,"the entered ridge alignment is kept");int best=WindBuildingLoads.SuggestedRidge(peaks);Check(peaks[best]==peaks.Max(),"suggested alignment has the largest |p|");
   var flat=WindBuildingLoads.RidgePeaks(rb,310,Flat("B"));Check(Math.Abs(flat.Max()-flat.Min())<1e-9,"uniform exposure: every alignment gives the same peak");}
  /* any ridge azimuth (owner, 25 November 2026: "All yung building rotation ialign sa different azimuth") */
  {var a45=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false,RidgeDirection=1};var b45=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false,Azimuth=45};
   var r1=WindBuildingLoads.Calculate(a45,310,Flat());var r2=WindBuildingLoads.Calculate(b45,310,Flat());Check(r1.Count==r2.Count&&r1.Zip(r2,(x,y)=>Math.Abs(x.P-y.P)<1e-12&&x.Direction==y.Direction).All(q=>q),"azimuth 45° = the NE–SW compass alignment");
   var b30=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false,Azimuth=30};var dirs=Flat();dirs[2]=new WindDirection{Name="E",Exposure="C",Shape="Low relief",K1=0,K2=0,K3=0,Kzt=1};
   var r3=WindBuildingLoads.Calculate(b30,310,dirs);var bearings=r3.Select(r=>r.Bearing).Distinct().OrderBy(x=>x).ToArray();Check(bearings.SequenceEqual(new[]{30.0,120,210,300}),"face normals at 30° steps of 90°: "+string.Join(",",bearings));
   var w120=r3.First(r=>Math.Abs(r.Bearing-120)<1e-9);Check(w120.Exposure=="C"&&w120.Direction.StartsWith("120° (E terrain)"),"between E and SE the larger qh (exposure C of E) governs: "+w120.Direction);
   Check(r3.First(r=>Math.Abs(r.Bearing-30)<1e-9).Direction.StartsWith("030° (")&&WindBuildingLoads.RidgeName(30)=="030°–210°"&&WindBuildingLoads.RidgeName(135)=="SE–NW"&&WindBuildingLoads.BearingName(-90)=="W","bearing and ridge names");
   var v=WindBuildingGeometry.Vertices(b30);Near(Math.Atan2(v[3].X-v[0].X,v[3].Y-v[0].Y)*180/Math.PI,30,1e-9,"the model outline is turned to the azimuth");
   Check(WindStaadInput.AxisOf(30+180,30)=="-X"&&WindStaadInput.AxisOf(120,30)=="+Z","STAAD axes with a +X bearing of 30°");
   var cmds=WindStaadInput.FaceCommands(b30,310,dirs,30);Check(cmds.Contains("WIND WW+X")&&cmds.Contains("WIND LW-Z")&&cmds.Contains("+X = 030°, +Z = 120°"),"STAAD wind types named by the model axes at any +X bearing");
   var peaks=WindBuildingLoads.AzimuthPeaks(b30,310,dirs,new[]{0.0,30,45});Check(peaks.Length==3&&peaks.All(p=>p>0)&&double.IsNaN(b30.Azimuth)==false&&b30.Azimuth==30,"azimuth sweep keeps the input azimuth");}
  /* terrain assessed along the building faces (owner, 25 November 2026: "dapat yung faces ng building, pati yung Topographic factor") */
  {var t=new WindTerrain{Lat=14.6,Lon=121,Radius=3200,Offset=30};for(int n=0;n<24;n++)for(double d=0;d<=3200;d+=100)t.Rays[n].Add(new WindSample{D=d,Z=n%3==1?5:double.NaN,Cover=n/3==2?80:50});
   for(int i=0;i<8;i++)for(double d=-500;d<=500;d+=100)t.Cross[i].Add(new WindSample{D=d,Z=5});
   var dirs=WindAssessment.Calculate(t,10,310,.85);Check(dirs.Select(d=>d.Name).SequenceEqual(new[]{"030°","075°","120°","165°","210°","255°","300°","345°"})&&dirs[2].Index==2&&Math.Abs(dirs[2].Bearing-120)<1e-9,"directions named by their bearings from the offset: "+string.Join(",",dirs.Select(d=>d.Name)));
   var bf=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false,Azimuth=30};var fr=WindBuildingLoads.Calculate(bf,310,dirs);
   Check(fr.Select(r=>r.Direction).Distinct().OrderBy(x=>x).SequenceEqual(new[]{"030° / parallel","120° / normal","210° / parallel","300° / normal"}),"every face uses its own assessed direction (no neighbour substitution): "+string.Join(",",fr.Select(r=>r.Direction).Distinct()));
   Check(fr.Where(r=>Math.Abs(r.Bearing-120)<1e-9).All(r=>r.TerrainDirection==2),"wind from 120° uses the 120° terrain");
   var b45=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false,RidgeDirection=0};var r0=WindBuildingLoads.Calculate(b45,310,dirs);Check(r0.Any(r=>r.Direction.Contains("terrain")),"a building not on the offset falls back to the governing neighbour");
   var doc=new System.Xml.XmlDocument();var root=doc.CreateElement("Sheet");doc.AppendChild(root);WindTerrainXml.Write(root,t);var back=WindTerrainXml.Read(root);Check(back!=null&&Math.Abs(back.Offset-30)<1e-9,"the offset is saved with the terrain snapshot");
   var noOff=new WindTerrain{Lat=14.6,Lon=121,Radius=3200};for(int n=0;n<24;n++)for(double d=0;d<=3200;d+=100)noOff.Rays[n].Add(new WindSample{D=d,Z=1,Cover=50});for(int i=0;i<8;i++)for(double d=-500;d<=500;d+=100)noOff.Cross[i].Add(new WindSample{D=d,Z=1});var doc2=new System.Xml.XmlDocument();var root2=doc2.CreateElement("Sheet");doc2.AppendChild(root2);WindTerrainXml.Write(root2,noOff);Check(!((System.Xml.XmlElement)root2.FirstChild).HasAttribute("offset")&&WindTerrainXml.Read(root2).Offset==0,"older snapshots (no offset) read as north-based");}
  Console.WriteLine("PASS: "+count+" wind pressure, workbook fixture, interpolation, sign and unresolved-data checks");
 }
}
