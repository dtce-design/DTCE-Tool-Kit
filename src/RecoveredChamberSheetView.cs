using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class ChamberSheetView
	{
		public string Kind;

		public string SelectedId = "";

		public TabPage Page;

		public DataGridView Entries;

		public DataGridView Materials;

		public DataGridView CutDetails;

		public Label Dimensions;

		public TabControl MaterialTabs;

		public long Revision = -1L;

		public bool Updating;
	}
}
