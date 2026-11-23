using System;using System.Linq;using BeamScheduleFormatter;
// Owner (23 November 2026): hinges as bought (Ball Bearing Stainless Door Hinge, 2 pcs per pack, four sizes); a double-leaf door gets
// 1 leverset, one 4" chain bolt and one 4" foot bolt; an optional deadbolt; leaves of different widths ("minsan may double leaf na
// magkaiba ang size nilang dalawa").
class DoorHardwareTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static OpeningItem Door(string id,double w,int leaves,string leafWidths="",string size="",string deadbolt="None",int qty=1,bool exterior=false){var d=new OpeningItem{Id=id,Description="Door",Wall="W1",Material="Wood",Kind="Door",Width=w,Height=2.15,Quantity=qty,JambDepth=.15,JambThickness=.05,LeafThickness=.045,Faces=2,Leaves=leaves,LeafWidths=leafWidths,HingeSize=size,Deadbolt=deadbolt,Exterior=exterior};d.Validate();return d;}
 static void Main(){
  Check(DoorHardware.AutoSize("Window",.6,false)=="3.0x3.0x2.5mm"&&DoorHardware.AutoSize("Door",.7,false)=="3.5x3.5x2.5mm"&&DoorHardware.AutoSize("Door",.85,false)=="4.0x3.0x3.0mm"&&DoorHardware.AutoSize("Door",.95,false)=="4.0x4.0x3.0mm"&&DoorHardware.AutoSize("Door",.7,true)=="4.0x4.0x3.0mm","automatic hinge size by leaf width / exterior");
  Check(DoorHardware.Packs(3)==2&&DoorHardware.Packs(6)==3&&DoorHardware.Packs(0)==0,"2 pcs per pack, rounded up");
  var single=Door("D1",.9,1);var by=single.HingesBySizePerUnit();Check(single.Locks==1&&single.Hinges==3&&by.Count==1&&by["3.5x3.5x2.5mm"]==3&&single.ChainBolts==0&&single.FootBolts==0,"single 0.80 m leaf: 1 leverset, 3 hinges 3.5x3.5");
  var dbl=Door("D2",1.5,2,"",qty:2);Check(dbl.Locks==2&&dbl.ChainBolts==2&&dbl.FootBolts==2&&dbl.Hinges==12,"double leaf: 1 leverset, 1 chain bolt and 1 foot bolt per unit");
  var mixed=Door("D3",1.3,2,"0.90+0.30");var m=mixed.HingesBySizePerUnit();Check(mixed.DoorLeaves().SequenceEqual(new[]{.9,.3})&&m["4.0x3.0x3.0mm"]==3&&m["3.5x3.5x2.5mm"]==3&&mixed.Hinges==6,"unequal leaves: each leaf its own hinges and size");
  Check(Door("D4",1.3,2,"900+300").DoorLeaves().SequenceEqual(new[]{.9,.3}),"leaf widths typed in mm");
  bool threw=false;try{Door("D5",1.3,2,"0.90").DoorLeaves();}catch(Exception){threw=true;}Check(threw,"one width per leaf is required");
  threw=false;try{Door("D6",1.0,2,"0.90+0.30").DoorLeaves();}catch(Exception){threw=true;}Check(threw,"leaf widths wider than the door are refused");
  var chosen=Door("D7",1.3,2,"0.90+0.30","3.5x3.5x2.5mm");Check(chosen.HingesBySizePerUnit().Count==1&&chosen.HingesBySizePerUnit()["3.5x3.5x2.5mm"]==6,"a chosen size applies to every leaf");
  var lockable=Door("D8",.9,1,deadbolt:DoorHardware.SingleThumbturn,qty:3);Check(lockable.Deadbolts==3,"deadbolt per door unit");
  var alu=new OpeningItem{Id="AD",Description="Aluminum door",Wall="W1",Material="Aluminum",Kind="Door",Width=1.5,Height=2.1,Quantity=1,Leaves=2,Deadbolt=DoorHardware.DoubleCylinder};alu.Validate();Check(alu.ChainBolts==0&&alu.Deadbolts==0&&alu.Hinges==0,"aluminum doors come with their own hardware");
  Console.WriteLine("PASS: "+checks+" door hardware checks");}
}
