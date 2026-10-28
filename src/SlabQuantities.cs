using System;
namespace BeamScheduleFormatter {
class SlabQuantities {
 public double AreaPerPanel,ConcretePerPanel,ConcreteTotal,SoffitTotal,EdgeFormsTotal;
 public double FormworkTotal{get{return SoffitTotal+EdgeFormsTotal;}}
 public static SlabQuantities Calculate(double shortMetres,double longMetres,double thicknessMm,int quantity,bool elevated=true,double exposedEdgeMetres=0,bool cantileverEdge=false){
  foreach(double v in new[]{shortMetres,longMetres,thicknessMm})if(double.IsNaN(v)||double.IsInfinity(v)||v<=0)throw new ArgumentException("Slab dimensions and thickness must be positive finite numbers.");
  if(shortMetres>longMetres)throw new ArgumentException("Shorter Side must not exceed Longer Side.");if(quantity<1||quantity>100000)throw new ArgumentException("Enter a whole slab quantity from 1 to 100000.");
  if(!cantileverEdge&&exposedEdgeMetres>0)throw new ArgumentException("Exposed-edge forms are counted only when the cantilever-edge option is selected.");
  if(double.IsNaN(exposedEdgeMetres)||double.IsInfinity(exposedEdgeMetres)||exposedEdgeMetres<0||exposedEdgeMetres>2*(shortMetres+longMetres))throw new ArgumentException("Exposed edge length must be between zero and panel perimeter.");
  double area=shortMetres*longMetres,volume=area*thicknessMm/1000;if(double.IsInfinity(volume*quantity))throw new ArgumentException("Slab quantities exceed calculation limits.");
  return new SlabQuantities{AreaPerPanel=area,ConcretePerPanel=volume,ConcreteTotal=volume*quantity,SoffitTotal=elevated?area*quantity:0,EdgeFormsTotal=exposedEdgeMetres*thicknessMm/1000*quantity};
 }
}
}
