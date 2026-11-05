using System;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
// Report derivations use the same full-precision inputs/results as the calculation engine.
static class DetailedDesignReports {
 static string N(double value){return value.ToString("0.######",CultureInfo.InvariantCulture);}
 static void Line(StringBuilder b,string label,string formula){b.AppendLine(label);b.AppendLine("  "+formula);}
 static string Status(bool pass){return pass?"PASS (listed check)":"REVIEW / EXCEEDED";}
 static void Heading(StringBuilder b,string title){b.AppendLine();b.AppendLine("## "+title);b.AppendLine(new string('-',Math.Min(96,title.Length)));}
 public static string Purlin(PurlinDesignInput i,PurlinDesignResult r){
  var b=new StringBuilder();var p=i.Properties;double theta=i.Angle*Math.PI/180,self=i.Profile.KgM*9.81/1000,dead=i.Dead*i.Spacing+self,l=i.Span*1000,det=p.Ix*p.Iy-p.Ixy*p.Ixy;
  b.AppendLine("DTCE | DETAILED PURLIN CALCULATION REPORT");b.AppendLine(i.Name+" | "+i.Profile.Name+" | ASD | "+i.Support);
  b.AppendLine("Units: dimensions mm; spans m; area loads kPa = kN/m2; line loads kN/m = N/mm.");
  b.AppendLine("Moments kN.m are multiplied by 1,000,000 for stress calculations in N.mm and MPa.");
  Heading(b,"01  DESIGN INPUTS AND SECTION BASIS");
  Line(b,"Geometry", "L="+N(i.Span)+" m; tributary spacing="+N(i.Spacing)+" m; roof slope="+N(i.Angle)+" deg.");
  Line(b,"Handbook section", "h="+N(i.Profile.H)+"; b="+N(i.Profile.B)+"; lip="+N(i.Profile.Lip)+"; t="+N(i.Profile.T)+" mm; mass="+N(i.Profile.KgM)+" kg/m.");
  Line(b,"Materials / restraint", "Fy="+N(i.Fy)+" MPa; E="+N(i.E)+" MPa; effective sag rods="+i.SagRods+"; "+i.Ceiling+".");
  b.AppendLine(p.Verified?"Ix/Iy/Ixy/Sx/Sy are user-verified inputs; area/centroid reference is nominal geometry.":"Gross-section properties use nonoverlapping rectangular plates; bend radii are neglected.");
  b.AppendLine("A=sum(bi*hi); xbar=sum(Ai*xi)/A; ybar=sum(Ai*yi)/A.");
  b.AppendLine("Ix=sum(bi*hi^3/12 + Ai*(yi-ybar)^2); Iy=sum(hi*bi^3/12 + Ai*(xi-xbar)^2).");
  b.AppendLine("Ixy=sum(Ai*(xi-xbar)*(yi-ybar)); Sx=Ix/max(abs(y)); Sy=Iy/max(abs(x)).");
  Line(b,"Adopted properties", "A="+N(p.Area)+" mm2; xbar="+N(p.Xbar)+" mm; ybar="+N(p.Ybar)+" mm.");
  Line(b,"Inertia", "Ix="+N(p.Ix)+"; Iy="+N(p.Iy)+"; Ixy="+N(p.Ixy)+" mm4.");
  Line(b,"Section moduli", "Sx="+N(p.Sx)+"; Sy="+N(p.Sy)+" mm3.");
  Heading(b,"02  SELF-WEIGHT AND RESOLVED LINE LOADS");
  Line(b,"Self-weight = mass * g / 1000", N(i.Profile.KgM)+" * 9.81 / 1000 = "+N(self)+" kN/m.");
  Line(b,"D = superimposed D * spacing + self-weight", N(i.Dead)+" * "+N(i.Spacing)+" + "+N(self)+" = "+N(dead)+" kN/m.");
  Line(b,"Dx = D*cos(theta); Dy = D*sin(theta)", N(dead)+" * cos("+N(i.Angle)+") = "+N(r.Dx)+"; "+N(dead)+" * sin("+N(i.Angle)+") = "+N(r.Dy)+" kN/m.");
  Line(b,"Lx = live*spacing*cos(theta); Ly = live*spacing*sin(theta)", N(i.Live)+" * "+N(i.Spacing)+" * cos("+N(i.Angle)+") = "+N(r.Lx)+"; Ly="+N(r.Ly)+" kN/m.");
  Line(b,"Wind acts normal to the roof; retain its entered sign", "Wp="+N(i.WindPressure)+" * "+N(i.Spacing)+" = "+N(i.WindPressure*i.Spacing)+"; Ws="+N(i.WindSuction)+" * "+N(i.Spacing)+" = "+N(i.WindSuction*i.Spacing)+" kN/m.");
  Heading(b,"03  WORKBOOK ALLOWABLE STRESSES AND MOMENT COEFFICIENTS");
  Line(b,"Flange ratio b/t versus 0.38*sqrt(E/Fy)", N(i.Profile.B/i.Profile.T)+" versus "+N(.38*Math.Sqrt(i.E/i.Fy))+" -> Fbx="+N(r.Fbx/i.Fy)+" * "+N(i.Fy)+" = "+N(r.Fbx)+" MPa.");
  Line(b,"Web ratio (h-2t)/t versus 3.76*sqrt(E/Fy)", N((i.Profile.H-2*i.Profile.T)/i.Profile.T)+" versus "+N(3.76*Math.Sqrt(i.E/i.Fy))+" -> Fby="+N(r.Fby/i.Fy)+" * "+N(i.Fy)+" = "+N(r.Fby)+" MPa.");
  b.AppendLine("These are the supplied workbook stress rules; they are not a complete cold-formed buckling assessment.");
  Line(b,"Mx=Cx*wx*L^2; My=Cy*wy*L^2", "Cx="+N(r.Cx)+"; Cy="+N(r.Cy)+"; L^2="+N(i.Span*i.Span)+" m2.");
  b.AppendLine("Sag rods affect the stated tangential coefficient only; normal-axis span and deflection use full span.");
  Heading(b,"04  LOAD CASES, SUBSTITUTIONS AND INTERACTION");
  bool coupled=Math.Abs(p.Ixy)>=1e-8;
  b.AppendLine(coupled?"Coupled section: Delta=Ix*Iy-Ixy^2="+N(det)+" mm8.":"Uncoupled section: ratio=abs(Mx)*1e6/(Sx*Fbx*) + abs(My)*1e6/(Sy*Fby*).");
  if(coupled){b.AppendLine("At each section corner (x,y): sx=Mx*1e6*(Iy*y-Ixy*x)/Delta.");b.AppendLine("sy=My*1e6*(Ix*x-Ixy*y)/Delta; ratio=max(abs(sx)/Fbx* + abs(sy)/Fby*).");}
  foreach(var c in r.Cases){double increase=c.Alternate&&i.AlternateIncrease?4.0/3:1;Heading(b,"CASE: "+c.Name);Line(b,"Resolved design line loads", "wx="+N(c.Normal)+"; wy="+N(c.Tangential)+" kN/m.");Line(b,"Moment substitutions", "Mx="+N(r.Cx)+" * "+N(c.Normal)+" * "+N(i.Span)+"^2 = "+N(c.Mx)+" kN.m.");b.AppendLine("  My="+N(r.Cy)+" * "+N(c.Tangential)+" * "+N(i.Span)+"^2 = "+N(c.My)+" kN.m.");Line(b,"Allowable stress factor", N(increase)+" -> Fbx*="+N(r.Fbx*increase)+"; Fby*="+N(r.Fby*increase)+" MPa.");if(!coupled)b.AppendLine("  Ratio="+N(Math.Abs(c.Mx))+"*1e6/("+N(p.Sx)+"*"+N(r.Fbx*increase)+") + "+N(Math.Abs(c.My))+"*1e6/("+N(p.Sy)+"*"+N(r.Fby*increase)+").");b.AppendLine("  Interaction="+N(c.Ratio)+" <= 1.0 : "+Status(c.Ratio<=1));}
  Heading(b,"05  GOVERNING FLEXURE ENVELOPES");
  foreach(bool alternate in new[]{false,true}){var cases=r.Cases.Where(c=>c.Alternate==alternate).ToList();var x=cases.OrderByDescending(c=>Math.Abs(c.Mx)).First();var y=cases.OrderByDescending(c=>Math.Abs(c.My)).First();double ratio=alternate?r.Alternate:r.Basic;b.AppendLine((alternate?"Alternate":"Basic")+" independent component envelope:");b.AppendLine("  abs(Mx)="+N(Math.Abs(x.Mx))+" from "+x.Name+"; abs(My)="+N(Math.Abs(y.My))+" from "+y.Name+".");b.AppendLine("  Envelope interaction="+N(ratio)+" <= 1.0 : "+Status(ratio<=1));}
  b.AppendLine("Independent component maxima may come from different cases; the envelope is a conservative screen.");var governing=r.Cases.OrderByDescending(c=>c.Ratio).First();b.AppendLine("Largest individual-case ratio: "+governing.Name+" = "+N(governing.Ratio)+".");
  Heading(b,"06  SERVICEABILITY / VECTOR DEFLECTION");double coefficient=i.Support=="Cantilever"?1.0/8:5.0/384;
  b.AppendLine("K="+N(coefficient)+"; span="+N(l)+" mm; Delta=Ix*Iy-Ixy^2="+N(det)+" mm8.");
  b.AppendLine("delta_x=K*Lmm^4*(Ix*wy-Ixy*wx)/(E*Delta).");b.AppendLine("delta_y=K*Lmm^4*(Iy*wx-Ixy*wy)/(E*Delta); resultant=sqrt(delta_x^2+delta_y^2).");
  foreach(bool total in new[]{false,true}){double wx=total?r.Dx+r.Lx:r.Lx,wy=total?r.Dy+r.Ly:r.Ly,factor=coefficient*Math.Pow(l,4)/i.E,dx=factor*(p.Ix*wy-p.Ixy*wx)/det,dy=factor*(p.Iy*wx-p.Ixy*wy)/det,value=total?r.TotalDeflection:r.LiveDeflection,limit=total?r.TotalLimit:r.LiveLimit;b.AppendLine((total?"D + L":"Live only")+": wx="+N(wx)+"; wy="+N(wy)+" N/mm.");b.AppendLine("  delta_x="+N(dx)+"; delta_y="+N(dy)+" -> resultant="+N(value)+" mm.");b.AppendLine("  Limit = "+N(l)+" / "+N(l/limit)+" = "+N(limit)+" mm; DCR="+N(value/limit)+" : "+Status(value<=limit));}
  Heading(b,"07  DESIGN DISPOSITION AND REFERENCES");b.AppendLine(r.WithinWorkbookChecks?"PASS for the listed workbook checks only; remaining member checks require verification.":"REVIEW: one or more listed workbook checks exceed their limits.");b.AppendLine("Full precision is used internally; displayed substitutions are rounded to six decimal places.");
  Heading(b,"APPENDIX  INPUT AUDIT, METHOD SCOPE AND SOURCE ADJUSTMENTS");b.Append(PurlinDesigner.ReferenceSummary(i,r));return b.ToString();
 }
 public static string Truss(ConnectionInput i,ConnectionResult r,string profile,string source){
  var b=new StringBuilder();int count=i.Family=="Double angle"?2:1;double gross=i.Area*count,connectionLength=i.Connection=="Weld"?(i.LL1+i.LL2)/2:(i.Bolts-1)*i.Spacing;
  b.AppendLine("DTCE | DETAILED TRUSS CONNECTION CALCULATION REPORT");b.AppendLine(profile+" | "+i.Family+" | "+i.Connection+" | "+i.Method);b.AppendLine("Units: dimensions mm; areas mm2; stresses MPa=N/mm2; forces kN.");
  Heading(b,"01  INPUTS AND LOAD BASIS");Line(b,"Axial design demand", N(i.Demand)+" kN; use loads consistent with "+i.Method+".");Line(b,"Member material", "Fy="+N(i.Fy)+"; Fu="+N(i.Fu)+" MPa; thickness="+N(i.Thickness)+" mm.");Line(b,"Member assembly area", "Ag="+N(i.Area)+" per member * "+count+" = "+N(gross)+" mm2.");Line(b,"Gusset / connected part", "Fy="+N(i.GussetFy)+"; Fu="+N(i.GussetFu)+" MPa; t="+N(i.GussetThickness)+" mm; effective width="+N(i.GussetWidth)+" mm.");
  b.AppendLine("Demand is an absolute axial envelope; sign, selected member/load cases and other force components are audited below.");
  Heading(b,"02  DESIGN FACTORS AND MEMBER YIELDING");b.AppendLine(i.Method=="LRFD"?"Available strength=phi*Rn; yielding phi=0.90, other listed limit states phi=0.75.":"Available strength=Rn/Omega; yielding Omega=1.67, other listed limit states Omega=2.00.");Line(b,"Member yield Rn=Fy*Ag/1000", N(i.Fy)+" * "+N(gross)+" / 1000 = "+N(i.Fy*gross/1000)+" kN.");
  Heading(b,"03  CONNECTION LENGTH AND SHEAR LAG");Line(b,i.Connection=="Weld"?"L=(LL1+LL2)/2":"L=(number of bolts-1)*spacing", i.Connection=="Weld"?"("+N(i.LL1)+"+"+N(i.LL2)+")/2 = "+N(connectionLength)+" mm.":"("+i.Bolts+"-1)*"+N(i.Spacing)+" = "+N(connectionLength)+" mm.");
  if(i.AllElementsConnected)b.AppendLine("U=1.0: user confirms all cross-section elements are connected.");else if(i.VerifiedU>0)b.AppendLine("U="+N(r.U)+": user-supplied verified factor.");else b.AppendLine("U=1-xbar/L = 1-"+N(i.Offset)+"/"+N(connectionLength)+" = "+N(r.U)+".");
  if(i.Connection=="Weld"){
   Heading(b,"04  WELD DETAIL AND EFFECTIVE LENGTHS");double thicker=Math.Max(i.Thickness,i.GussetThickness),minimum=thicker<=6?3:thicker<=13?5:thicker<=19?6:8,edge=i.WeldEdgeThickness>0?i.WeldEdgeThickness:i.Thickness,maximum=edge<6?edge:edge-2;b.AppendLine("w="+N(i.Weld)+" mm; Fexx="+N(i.Fexx)+" MPa; throat=0.707w="+N(.707*i.Weld)+" mm.");b.AppendLine("Thicker connected part="+N(thicker)+" mm -> minimum weld="+N(minimum)+" mm.");b.AppendLine("Actual welded edge="+N(edge)+" mm -> maximum weld="+N(maximum)+" mm; verify exceptions separately.");b.AppendLine("Minimum segment length=4w="+N(4*i.Weld)+" mm.");
   foreach(var item in new[]{new{Label="LL1",Length=i.LL1},new{Label="LL2",Length=i.LL2},new{Label="LT",Length=i.LT}}){double ratio=item.Length/i.Weld,eff=TrussConnectionDesigner.WeldLength(item.Length,i.Weld);b.AppendLine(item.Label+": L="+N(item.Length)+" mm; L/w="+N(ratio)+"; Leff="+N(eff)+" mm.");}
   b.AppendLine("Leff=L for L/w<=100; (1.2-0.002L/w)*L for <=300; 180w above 300.");
   Heading(b,"05  WELD METAL AND MEMBER RUPTURE");double longLength=TrussConnectionDesigner.WeldLength(i.LL1,i.Weld)+TrussConnectionDesigner.WeldLength(i.LL2,i.Weld),transLength=TrussConnectionDesigner.WeldLength(i.LT,i.Weld),rl=.707*i.Weld*longLength*.6*i.Fexx/1000,rt=.707*i.Weld*transLength*.6*i.Fexx/1000;Line(b,"Rn,long = 0.707*w*(Leff1+Leff2)*0.60*Fexx/1000", "0.707*"+N(i.Weld)+"*"+N(longLength)+"*0.60*"+N(i.Fexx)+"/1000 = "+N(rl)+" kN.");Line(b,"Rn,trans = 0.707*w*LeffT*0.60*Fexx/1000", "0.707*"+N(i.Weld)+"*"+N(transLength)+"*0.60*"+N(i.Fexx)+"/1000 = "+N(rt)+" kN.");b.AppendLine(i.DirectionalWeldIncrease?"Directional option: Rn=max(Rlong+Rtrans,0.85Rlong+1.50Rtrans)*assembly count.":"Directional increase disabled: Rn=(Rlong+Rtrans)*assembly count.");b.AppendLine("  Assembly weld Rn="+N(r.NominalWeld)+" kN; assembly count="+count+".");Line(b,"Member rupture Rn=Fu*U*Ag/1000", N(i.Fu)+"*"+N(r.U)+"*"+N(gross)+"/1000 = "+N(r.NominalRupture)+" kN.");
  }else{
   Heading(b,"04  BOLT GEOMETRY AND DETAIL LIMITS");double thin=Math.Min(i.Thickness,i.GussetThickness),lc=Math.Min(i.Spacing-i.HoleDiameter,i.EdgeDistance-i.HoleDiameter/2),ab=Math.PI*i.BoltDiameter*i.BoltDiameter/4;
   b.AppendLine(i.Grade+"; db="+N(i.BoltDiameter)+" mm; hole="+N(i.HoleDiameter)+" mm; bolts="+i.Bolts+"; shear planes="+i.ShearPlanes+".");b.AppendLine("Spacing="+N(i.Spacing)+" mm; end distance="+N(i.EdgeDistance)+" mm; Fnv="+N(i.Fnv)+" MPa.");b.AppendLine("Minimum spacing=8db/3="+N(8*i.BoltDiameter/3)+" mm; maximum="+N(i.Exposed?Math.Min(14*thin,180):Math.Min(24*thin,300))+" mm.");b.AppendLine("Edge limits: 1.75db="+N(1.75*i.BoltDiameter)+" to min(12t,150)="+N(Math.Min(12*thin,150))+" mm.");Line(b,"Lc=min(spacing-hole,edge-hole/2)", "min("+N(i.Spacing-i.HoleDiameter)+","+N(i.EdgeDistance-i.HoleDiameter/2)+") = "+N(lc)+" mm.");
   Heading(b,"05  BOLT SHEAR, BEARING AND NET-SECTIONS");Line(b,"Bolt area Ab=pi*db^2/4", "pi*"+N(i.BoltDiameter)+"^2/4 = "+N(ab)+" mm2.");Line(b,"Bolt shear Rn=Fnv*Ab*n*shear planes/1000", N(i.Fnv)+"*"+N(ab)+"*"+i.Bolts+"*"+i.ShearPlanes+"/1000 = "+N(r.NominalBolt)+" kN.");
   foreach(bool member in new[]{true,false}){double t=member?i.Thickness:i.GussetThickness,fu=member?i.Fu:i.GussetFu,first=1.2*lc*t*fu,second=2.4*i.BoltDiameter*t*fu;int mult=member?count:1;Line(b,(member?"Member":"Gusset")+" bearing Rn=n*min(1.2Lc*t*Fu,2.4db*t*Fu)*multiplicity/1000", i.Bolts+"*min("+N(first)+","+N(second)+")*"+mult+"/1000 = "+N(i.Bolts*Math.Min(first,second)*mult/1000)+" kN.");}
   double net=gross-i.HoleDiameter*i.Thickness*count,gnet=(i.GussetWidth-i.HoleDiameter)*i.GussetThickness;Line(b,"Member An=Ag-hole*t*assembly count", N(gross)+"-"+N(i.HoleDiameter)+"*"+N(i.Thickness)+"*"+count+" = "+N(net)+" mm2.");Line(b,"Member rupture Rn=Fu*U*An/1000", N(i.Fu)+"*"+N(r.U)+"*"+N(net)+"/1000 = "+N(r.NominalRupture)+" kN.");Line(b,"Gusset yield Rn=Fy*width*t/1000", N(i.GussetFy)+"*"+N(i.GussetWidth)+"*"+N(i.GussetThickness)+"/1000 = "+N(i.GussetFy*i.GussetWidth*i.GussetThickness/1000)+" kN.");Line(b,"Gusset An=(width-hole)*t", "("+N(i.GussetWidth)+"-"+N(i.HoleDiameter)+")*"+N(i.GussetThickness)+" = "+N(gnet)+" mm2.");Line(b,"Gusset rupture Rn=Fu*An/1000", N(i.GussetFu)+"*"+N(gnet)+"/1000 = "+N(i.GussetFu*gnet/1000)+" kN.");
  }
  Heading(b,"06  MEMBER BLOCK SHEAR");
  if(i.Agv>0){double first=(.6*i.Fu*i.Anv+i.Fu*i.Ant)/1000,second=(.6*i.Fy*i.Agv+i.Fu*i.Ant)/1000;b.AppendLine("Verified TOTAL assembly areas: Agv="+N(i.Agv)+"; Anv="+N(i.Anv)+"; Ant="+N(i.Ant)+" mm2; Ubs=1.");Line(b,"Rn=min(0.6Fu*Anv+Fu*Ant,0.6Fy*Agv+Fu*Ant)/1000", "min("+N(first)+","+N(second)+") = "+N(Math.Min(first,second))+" kN.");b.AppendLine("Only the entered member path is checked; gusset and other paths remain separate.");}else b.AppendLine("NOT CHECKED: no verified member path areas entered. This omission prevents a complete connection approval.");
  Heading(b,"07  AVAILABLE STRENGTHS, DEMAND/CAPACITY RATIOS AND GOVERNING CHECK");
  foreach(var check in r.Checks){bool yield=check.Name.IndexOf("yield",StringComparison.OrdinalIgnoreCase)>=0;double phi=yield?.9:.75,omega=yield?1.67:2,nominal=i.Method=="LRFD"?check.Capacity/phi:check.Capacity*omega;b.AppendLine(check.Name);b.AppendLine("  "+(i.Method=="LRFD"?"phi*Rn="+N(phi)+"*"+N(nominal):"Rn/Omega="+N(nominal)+"/"+N(omega))+" = "+N(check.Capacity)+" kN.");b.AppendLine("  DCR="+N(check.Demand)+"/"+N(check.Capacity)+" = "+N(check.Demand/check.Capacity)+" <= 1.0 : "+Status(check.Pass));}
  var governing=r.Checks.OrderByDescending(c=>c.Demand/c.Capacity).First();b.AppendLine("Governing listed strength check: "+governing.Name+"; DCR="+N(governing.Demand/governing.Capacity)+".");foreach(string error in r.DetailErrors)b.AppendLine("REVIEW DETAIL: "+error);
  Heading(b,"08  DISPOSITION, SCOPE AND SOURCE AUDIT");b.AppendLine(r.ListedChecksPass?"PASS for listed strengths/detail limits only; omitted limit states require separate verification.":"REVIEW: a listed strength or detail limit is exceeded.");b.AppendLine("Full precision is used internally; displayed substitutions are rounded to six decimal places.");b.Append(TrussConnectionDesigner.ReferenceSummary(i,r,profile,source));return b.ToString();
 }
}
}
