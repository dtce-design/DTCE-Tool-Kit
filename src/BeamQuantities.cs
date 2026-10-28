using System;
namespace BeamScheduleFormatter {
class BeamQuantities {public double Concrete,SideForms,BottomForms;public double Formwork{get{return SideForms+BottomForms;}}public static BeamQuantities Calculate(double widthMm,double depthMm,double length,int quantity,bool gravel,double slabMm){if(double.IsNaN(widthMm+depthMm+length+slabMm)||double.IsInfinity(widthMm+depthMm+length+slabMm)||widthMm<=0||depthMm<=0||length<=0||quantity<=0||slabMm<0||slabMm>=depthMm)throw new Exception("Check B, D, length, quantity and slab thickness (< beam depth).");double b=widthMm/1000,d=depthMm/1000;return new BeamQuantities{Concrete=b*d*length*quantity,SideForms=2*(gravel?d:d-slabMm/1000)*length*quantity,BottomForms=gravel?0:b*length*quantity};}}
}
