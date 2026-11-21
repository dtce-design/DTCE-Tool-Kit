using System;using System.IO;using System.Windows.Forms;using BeamScheduleFormatter;
/* v6.0.42: sideways scrolling of a wide table and the saved scroll direction (owner, 21 November 2026) */
class HorizontalWheelTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 [STAThread]static void Main(){
  var path=Path.Combine(Path.GetTempPath(),"dtce-hscroll-"+Guid.NewGuid().ToString("N")+".xml");
  try{Check(!AppearancePreferences.ReadReversedScroll(path),"normal direction when nothing is saved");AppearancePreferences.WriteReversedScroll(path,true);Check(AppearancePreferences.ReadReversedScroll(path),"reversed direction is saved");
   Check(AppearancePreferences.Read(path)==AppearanceMode.System,"the appearance setting still reads");AppearancePreferences.Write(path,AppearanceMode.Dark);AppearancePreferences.WriteReversedScroll(path,false);Check(AppearancePreferences.Read(path)==AppearanceMode.Dark&&!AppearancePreferences.ReadReversedScroll(path),"both settings kept side by side");}
  finally{try{File.Delete(path);File.Delete(path+".bak");}catch(Exception){}}
  using(var f=new Form{Width=500,Height=300}){var g=new DataGridView{Dock=DockStyle.Fill};for(int i=0;i<30;i++)g.Columns.Add("c"+i,"Column "+i);g.Rows.Add();f.Controls.Add(g);f.Show();Application.DoEvents();
   int first=g.FirstDisplayedScrollingColumnIndex,offset=g.HorizontalScrollingOffset;Check(HorizontalWheel.Scroll(g,200),"a wide table scrolls sideways");Application.DoEvents();Check(g.HorizontalScrollingOffset>offset||g.FirstDisplayedScrollingColumnIndex>first,"it moved to the right");
   int after=g.HorizontalScrollingOffset,afterCol=g.FirstDisplayedScrollingColumnIndex;HorizontalWheel.Scroll(g,-200);Application.DoEvents();Check(g.HorizontalScrollingOffset<after||g.FirstDisplayedScrollingColumnIndex<afterCol,"and back to the left");f.Close();}
  Console.WriteLine("PASS: "+checks+" horizontal scroll checks");}
}
