using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class AppBrand
	{
		private static Icon icon;

		private static Image logo;

		public static Icon Icon
		{
			get
			{
				if (icon == null)
				{
					using (Stream stream = typeof(AppBrand).Assembly.GetManifestResourceStream("DtceIcon.ico"))
					{
						icon = new Icon(stream);
					}
				}
				return icon;
			}
		}

		public static Image Logo
		{
			get
			{
				if (logo == null)
				{
					using (Stream stream = typeof(AppBrand).Assembly.GetManifestResourceStream("DtceLogo.png"))
					{
						using (Image original = Image.FromStream(stream))
						{
							logo = new Bitmap(original);
						}
					}
				}
				return logo;
			}
		}

		public static void Apply(Form form)
		{
			form.Icon = Icon;
			form.Shown += delegate
			{
				PowTheme.Apply(form);
			};
		}
	}
}
