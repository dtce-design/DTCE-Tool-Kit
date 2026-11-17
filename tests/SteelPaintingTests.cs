using System;using System.Linq;using BeamScheduleFormatter;
class SteelPaintingTests {
 static int checks;static void Near(double a,double b,string m){checks++;if(Math.Abs(a-b)>1e-9)throw new Exception(m+": "+a+" != "+b);}
 static void Main(){
  var purlin=new SteelProfile{Kind="Purlin",H=76.2,B=38.1,Lip=12.7,T=0.8};Near(SteelPainting.AreaPerMetre(purlin),(2*76.2+4*38.1+4*12.7)/1000,"Lipped C purlin: web, flanges and lips, both faces");
  Near(SteelPainting.Area(purlin,3.5,4),0.3556*14,"Purlin area = per-metre x length x quantity");
  var angle=new SteelProfile{Kind="Angle bar",H=50,B=50,T=5};Near(SteelPainting.AreaPerMetre(angle),0.2,"Angle 2(H+B): outer, inner and edges");
  Near(SteelPainting.AreaPerMetre(new SteelProfile{Kind="Flat bar",B=50,T=6}),0.112,"Flat bar 2(B+t)");
  Near(SteelPainting.AreaPerMetre(new SteelProfile{Kind="HSS rectangle",H=100,B=50,T=2}),0.3,"Rectangular tube outside only");
  foreach(var p in SteelCatalog.Profiles.Where(p=>p.Kind=="Purlin"||p.Kind=="Angle bar")){checks++;if(!(SteelPainting.AreaPerMetre(p)>0))throw new Exception("Catalog profile without painting area: "+p.Name);}
  try{SteelPainting.Area(angle,0,1);throw new Exception("Zero length accepted");}catch(Exception e){if(e.Message=="Zero length accepted")throw;checks++;}
  Console.WriteLine("PASS: "+checks+" structural steel metal painting area checks");
 }
}
