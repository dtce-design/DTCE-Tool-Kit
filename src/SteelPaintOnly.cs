using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Structural Steel paint options (owner, 18 November 2026):
// - "Paint area m2" shows the metal painting area calculated for each purlin, angle bar, crossbracing and fascia row
//   ("bat di pinapakita yung paint calculated sa purlin").
// - "Paint only" marks a row whose members are only painted (e.g. existing steel): its paint area still goes to
//   metal painting, but it is left out of steel weight, purchase, cutting plan and turnbuckles
//   ("maglagay ng option na paint lang i-calculate, hindi kasama sa weight").
// Both are named columns placed before Status; Paint only is saved as its own attribute (po) so the index-based
// c0..cN entries of older files are unchanged.
partial class ProgramOfWorksForm {
 internal const string SteelPaintOnlyColumn="SteelPaintOnly",SteelPaintAreaColumn="SteelPaintArea";
 internal const string SteelPaintOnlyStatus="Paint only - not in steel weight";
 static readonly string[] SteelPaintSections={"SteelPurlins","SteelAngles","CrossBracing","FasciaFrame"};
 void AddSteelPaintColumns(){foreach(var item in SteelTables()){if(!SteelPaintSections.Contains(item.Key))continue;var g=item.Value;if(g.Columns.Contains(SteelPaintOnlyColumn))continue;
   var area=new DataGridViewTextBoxColumn{Name=SteelPaintAreaColumn,HeaderText="Paint area m2",ToolTipText="Metal painting area of this row (installed length, all exposed faces). Goes to Metal painting in Bill of Quantities.",ReadOnly=true,Width=100};
   area.DefaultCellStyle.Format="0.00";area.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;g.Columns.Insert(g.Columns.Count-1,area);
   var only=new DataGridViewCheckBoxColumn{Name=SteelPaintOnlyColumn,HeaderText="Paint only",ToolTipText="Tick when these members are only painted (e.g. existing steel). The paint area is counted; steel weight, purchase and cutting are not.",Width=80};
   only.DefaultCellStyle.BackColor=ModernUi.Input;g.Columns.Insert(g.Columns.Count-1,only);
   g.CurrentCellDirtyStateChanged+=delegate{if(g.IsCurrentCellDirty&&g.CurrentCell!=null&&g.CurrentCell.OwningColumn.Name==SteelPaintOnlyColumn)g.CommitEdit(DataGridViewDataErrorContexts.Commit);};}}
 static bool IsSteelPaintOnly(DataGridViewRow row){if(row==null||row.DataGridView==null||!row.DataGridView.Columns.Contains(SteelPaintOnlyColumn))return false;object v=row.Cells[SteelPaintOnlyColumn].Value;return v is bool?(bool)v:string.Equals(Convert.ToString(v),"True",StringComparison.OrdinalIgnoreCase);}
 static void ShowSteelPaintArea(DataGridViewRow row,double area){if(row==null||row.DataGridView==null||!row.DataGridView.Columns.Contains(SteelPaintAreaColumn))return;var c=row.Cells[SteelPaintAreaColumn];double old=0;if(c.Value is double)old=(double)c.Value;c.Value=old+area;}
 void AddPaintOnlyRow(DataGridViewRow row,string profile,double length,int qty){SteelWeight.Positive(length);SteelWeight.Count(qty);AddSteelPaint(row,profile,length,qty);row.Cells[row.DataGridView.Columns.Count-1].Value=SteelPaintOnlyStatus;}
 static void WriteSteelPaintOnly(DataGridViewRow row,XmlElement entry){if(IsSteelPaintOnly(row))entry.SetAttribute("po","1");}
 static void ReadSteelPaintOnly(DataGridViewRow row,XmlElement entry){if(row.DataGridView.Columns.Contains(SteelPaintOnlyColumn))row.Cells[SteelPaintOnlyColumn].Value=entry.GetAttribute("po")=="1";}
}
}
