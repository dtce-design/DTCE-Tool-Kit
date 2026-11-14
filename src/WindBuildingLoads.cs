using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
class WindBuildingInput {
 public double Width,Length,Eave,Apex,Kd=.85,Damping=.02,FrequencyNormal,FrequencyParallel,UserG=.85,OpeningHeight;
 public string System="MRF Steel",Enclosure="Enclosed";public bool AutoG=true;public int RidgeDirection;
 public double Angle{get{return Math.Atan2(2*(Apex-Eave),Width)*180/Math.PI;}}
 public double Height{get{return Angle<=10?Eave:(Eave+Apex)/2;}}
 public void Validate(){foreach(double n in new[]{Width,Length,Eave,Apex,Kd,Damping,UserG})if(!WindTin.Finite(n)||n<=0)throw new Exception("Enter positive building dimensions, Kd, damping and G.");if(!WindTin.Finite(OpeningHeight)||!WindTin.Finite(FrequencyNormal)||!WindTin.Finite(FrequencyParallel)||FrequencyNormal<0||FrequencyParallel<0||Apex<Eave||Angle>60||Height>150||Kd>1||Damping>0.2||UserG>3||OpeningHeight<0||OpeningHeight>Apex||RidgeDirection<0||RidgeDirection>3)throw new Exception("Check building geometry, ridge direction, opening height and factors. Roof slope must be 0–60 degrees.");if(Enclosure!="Enclosed"&&Enclosure!="Partially enclosed")throw new Exception("This pressure sheet supports enclosed / partially enclosed buildings.");}
}
class WindPressureRow {
 public string Direction,Surface,Case,Exposure;public double Z,Kz,Kzt,Q,Qh,Qi,G,Cp,GCpi,P;public int TerrainDirection;
}
static class WindBuildingLoads {
 public static double Lerp(double x,double x0,double x1,double y0,double y1){return x1==x0?y0:y0+(y1-y0)*(x-x0)/(x1-x0);}
 public static double Kz(string exposure,double height){double a=exposure=="B"?7:exposure=="C"?9.5:exposure=="D"?11.5:double.NaN,zg=exposure=="B"?365.76:exposure=="C"?274.32:exposure=="D"?213.36:double.NaN;return 2.01*Math.Pow(Math.Max(height,4.57)/zg,2/a);}
 public static double TopographyAt(WindDirection d,double z){if(!WindTin.Finite(d.Kzt))return double.NaN;if(d.K1==0||d.K2==0)return 1;return WindAssessment.Factors(d.H,d.Lh,d.X,z,d.Exposure,d.Shape)[3];}
 public static double Leeward(double ratio){return ratio<=1?-.5:ratio<=2?Lerp(ratio,1,2,-.5,-.3):ratio<4?Lerp(ratio,2,4,-.3,-.2):-.2;}
 public static double Frequency(WindBuildingInput b){return b.System=="MRF Steel"?22.2/Math.Pow(b.Height,.8):b.System=="MRF Concrete"?43.5/Math.Pow(b.Height,.9):75/b.Height;}
 static double Response(double eta){return eta>1e-5?1/eta-(1-Math.Exp(-2*eta))/(2*eta*eta):1;}
 public static double Gust(WindBuildingInput b,string exposure,bool normal,double speed){
  if(!b.AutoG)return b.UserG;
  int i=exposure=="B"?0:exposure=="C"?1:exposure=="D"?2:-1;if(i<0)return double.NaN;
  double z=Math.Max(.6*b.Height,new[]{9.14,4.57,2.13}[i]),iz=new[]{.3,.2,.15}[i]*Math.Pow(10/z,1.0/6),lz=new[]{97.54,152.4,198.12}[i]*Math.Pow(z/10,new[]{1.0/3,.2,.125}[i]),breadth=normal?b.Length:b.Width,depth=normal?b.Width:b.Length;
  double q=Math.Sqrt(1/(1+.63*Math.Pow((breadth+b.Height)/lz,.63))),n=normal?b.FrequencyNormal:b.FrequencyParallel;if(n<=0)n=Frequency(b);
  if(n>=1)return .925*(1+1.7*3.4*iz*q)/(1+1.7*3.4*iz);
  if(n<=1.0/3600)throw new Exception("Natural frequency is outside this gust-response formulation.");
  double vz=new[]{.45,.65,.8}[i]*Math.Pow(z/10,new[]{.25,1.0/6.5,1.0/9}[i])*speed/3.6,n1=n*lz/vz,rn=7.47*n1/Math.Pow(1+10.3*n1,5.0/3);
  double r=Math.Sqrt(rn/b.Damping*Response(4.6*n*b.Height/vz)*Response(4.6*n*breadth/vz)*(.53+.47*Response(4.6*n*depth/vz))),gr=Math.Sqrt(2*Math.Log(3600*n))+.577/Math.Sqrt(2*Math.Log(3600*n));
  return .925*(1+1.7*iz*Math.Sqrt(3.4*3.4*q*q+gr*gr*r*r))/(1+1.7*3.4*iz);
 }
 // Figure 207B.4-1, workbook Calculation!D139:K145. High h/L row applies at >=1.
 static readonly double[] angles={10,15,20,25,30,35,45,60};
 static readonly double[][] suction={new[]{-.7,-.5,-.3,-.2,-.2,0,.4,.6},new[]{-.9,-.7,-.4,-.3,-.2,-.2,0,.6},new[]{-1.3,-1,-.7,-.5,-.3,-.2,0,.6}};
 static readonly double[][] positive={new[]{-.18,0,.2,.3,.3,.4,.4,.6},new[]{-.18,-.18,0,.2,.2,.3,.4,.6},new[]{-.18,-.18,-.18,0,.2,.2,.3,.6}};
 static double Across(double[] row,double angle){for(int j=1;j<angles.Length;j++)if(angle<=angles[j])return Lerp(angle,angles[j-1],angles[j],row[j-1],row[j]);return row[row.Length-1];}
 static double HeightRatio(double ratio,double a,double b,double c){return ratio<=.25?a:ratio<=.5?Lerp(ratio,.25,.5,a,b):ratio<1?Lerp(ratio,.5,1,b,c):c;}
 public static double RoofNormal(double angle,double ratio,bool alternate){var table=alternate?positive:suction;return HeightRatio(ratio,Across(table[0],angle),Across(table[1],angle),Across(table[2],angle));}
 public static double RoofLee(double angle,double ratio){double a=Lerp(Math.Min(20,angle),10,15,-.3,-.5);if(angle>=15)a=Lerp(Math.Min(20,angle),15,20,-.5,-.6);double b=angle<=15?-.5:Lerp(Math.Min(20,angle),15,20,-.5,-.6),c=angle<=15?Lerp(angle,10,15,-.7,-.6):-.6;return HeightRatio(ratio,a,b,c);}
 public static double RoofZone(int zone,double ratio){return Lerp(Math.Max(.5,Math.Min(1,ratio)),.5,1,new[]{-.9,-.9,-.5,-.3}[zone],new[]{-1.3,-.7,-.7,-.7}[zone]);}
 public static List<WindPressureRow> Calculate(WindBuildingInput b,double speed,IList<WindDirection> directions){
  b.Validate();if(!WindTin.Finite(speed)||speed<=0||directions.Count!=8)throw new Exception("Valid wind speed and all eight terrain directions are required.");var rows=new List<WindPressureRow>();
  for(int axis=0;axis<4;axis++){
   bool normal=axis<2;int index=normal?(b.RidgeDirection+2+axis*4)%8:(b.RidgeDirection+(axis-2)*4)%8;var d=directions[index];
   if(!new[]{"B","C","D"}.Contains(d.Exposure)||!WindTin.Finite(d.Kzt))continue;
   double g=Gust(b,d.Exposure,normal,speed),kh=Kz(d.Exposure,b.Height),qh=.613*kh*TopographyAt(d,b.Height)*b.Kd*Math.Pow(speed/3.6,2)/1000,gcpi=b.Enclosure=="Enclosed"?.18:.55,ratio=normal?b.Width/b.Length:b.Length/b.Width;
   Action<string,string,double,double> add=(surface,loadcase,z,cp)=>{
    double kz=Kz(d.Exposure,z),q=.613*kz*TopographyAt(d,z)*b.Kd*Math.Pow(speed/3.6,2)/1000;
    for(int sign=1;sign>=-1;sign-=2){double qi=b.Enclosure=="Partially enclosed"&&sign>0&&b.OpeningHeight>0?.613*Kz(d.Exposure,b.OpeningHeight)*TopographyAt(d,b.OpeningHeight)*b.Kd*Math.Pow(speed/3.6,2)/1000:qh;
     rows.Add(new WindPressureRow{Direction=d.Name+" / "+(normal?"normal":"parallel"),TerrainDirection=index,Exposure=d.Exposure,Surface=surface,Case=loadcase+"; GCpi "+(sign>0?"+":"−"),Z=z,Kz=kz,Kzt=TopographyAt(d,z),Q=q,Qh=qh,Qi=qi,G=g,Cp=cp,GCpi=sign*gcpi,P=q*g*cp-qi*sign*gcpi});
    }
   };
   double top=normal?b.Eave:b.Apex;for(int j=0;j<=10;j++)add("Windward wall","z="+(top*j/10).ToString("0.##",CultureInfo.InvariantCulture)+"m",top*j/10,.8);
   add("Leeward wall","L/B="+ratio.ToString("0.###",CultureInfo.InvariantCulture),b.Height,Leeward(ratio));add("Side walls","All values",b.Height,-.7);
   if(normal&&b.Angle>=10){add("Windward roof","Cp branch 1",b.Height,RoofNormal(b.Angle,b.Height/b.Width,false));add("Windward roof","Cp branch 2",b.Height,RoofNormal(b.Angle,b.Height/b.Width,true));add("Leeward roof","Leeward",b.Height,RoofLee(b.Angle,b.Height/b.Width));}
   else {double depth=normal?b.Width:b.Length;double[] start={0,b.Height/2,b.Height,2*b.Height},end={b.Height/2,b.Height,2*b.Height,depth};for(int zone=0;zone<4;zone++){if(start[zone]>=depth)continue;string range=start[zone].ToString("0.##",CultureInfo.InvariantCulture)+"–"+Math.Min(depth,end[zone]).ToString("0.##",CultureInfo.InvariantCulture)+"m from windward edge";add("Roof zone "+(zone+1),range+"; Cp branch 1",b.Height,RoofZone(zone,b.Height/depth));add("Roof zone "+(zone+1),range+"; Cp branch 2",b.Height,-.18);}}
  }
  return rows;
 }
 public static string Report(WindBuildingInput b,IEnumerable<WindPressureRow> rows){var text=new StringBuilder();text.AppendLine("## BUILDING WIND PRESSURES | NSCP 2015 DIRECTIONAL PROCEDURE");text.AppendLine(string.Format(CultureInfo.InvariantCulture,"Width={0}m; length={1}m; eave={2}m; apex={3}m; slope={4:0.###}deg; mean roof height={5:0.###}m",b.Width,b.Length,b.Eave,b.Apex,b.Angle,b.Height));text.AppendLine("p = q G Cp - qi (GCpi); positive pressure acts toward the surface, negative away.");text.AppendLine("Four principal building axes use the matching eight-direction terrain sector. Both internal-pressure signs and roof Cp branches are separate load cases.");text.AppendLine("Reference: RCB Calculation!B10,D17,D42:D69,B74:D87,B104:T133,D139:Q203,J209:J265. Linear interpolation of Figure207B.4-1; h/L>=1 uses the high-ratio row, correcting the workbook's 10 sentinel.");text.AppendLine("Gross calculated surface pressures, not a complete MWFRS design: apply minimum design forces, prescribed load patterns/torsion and load combinations separately. C&C, open roofs, parapets and overhangs are outside this table.");foreach(var r in rows)text.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0} | {1} | {2} | Exp {3}\r\n z={4:0.###}m; Kz={5:0.#####}; Kzt={6:0.#####}; q={7:0.#####}kPa; qh={8:0.#####}kPa; qi={9:0.#####}kPa\r\n p=({7:0.#####})({10:0.#####})({11:0.#####})-({9:0.#####})({12:0.#####})={13:0.#####}kPa",r.Direction,r.Surface,r.Case,r.Exposure,r.Z,r.Kz,r.Kzt,r.Q,r.Qh,r.Qi,r.G,r.Cp,r.GCpi,r.P));return text.ToString();}
}
}
