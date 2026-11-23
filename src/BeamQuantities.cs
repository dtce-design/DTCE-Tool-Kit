using System;
namespace BeamScheduleFormatter {
class BeamQuantities {public double Concrete,SideForms,BottomForms;/* the dimensions, for the formula in the reports */public double B,D,L;public int N;public double Formwork{get{return SideForms+BottomForms;}}public static BeamQuantities Calculate(double widthMm,double depthMm,double length,int quantity,bool gravel,double slabMm){if(double.IsNaN(widthMm+depthMm+length)||double.IsInfinity(widthMm+depthMm+length)||widthMm<=0||depthMm<=0||length<=0||quantity<=0)throw new Exception("Check B, D, length, quantity.");double b=widthMm/1000,d=depthMm/1000;return new BeamQuantities{B=b,D=d,L=length,N=quantity,Concrete=b*d*length*quantity,SideForms=2*d*length*quantity,BottomForms=gravel?0:b*length*quantity};}}
}
