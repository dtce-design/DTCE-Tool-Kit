using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class PdfBrand
	{
		private static Bitmap watermark;

		private static byte[] jpeg;

		private const string LogoResource = "DtceLogo.png";

		public static Bitmap Watermark
		{
			get
			{
				if (watermark == null)
				{
					using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DtceLogo.png"))
					{
						string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DtceLogo.png");
						if (stream == null && !File.Exists(text))
						{
							throw new Exception("Watermark logo missing: DtceLogo.png");
						}
						using (Image image = ((stream == null) ? Image.FromFile(text) : Image.FromStream(stream)))
						{
							watermark = new Bitmap(1200, 1200);
							using (Graphics graphics = Graphics.FromImage(watermark))
							{
								graphics.Clear(Color.White);
								float num = 1120f / (float)Math.Max(image.Width, image.Height);
								float num2 = (float)image.Width * num;
								float num3 = (float)image.Height * num;
								using (ImageAttributes imageAttributes = new ImageAttributes())
								{
									ColorMatrix colorMatrix = new ColorMatrix();
									colorMatrix.Matrix33 = 0.085f;
									imageAttributes.SetColorMatrix(colorMatrix);
									graphics.DrawImage(image, new Rectangle((int)((1200f - num2) / 2f), (int)((1200f - num3) / 2f), (int)num2, (int)num3), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
								}
							}
						}
					}
				}
				return watermark;
			}
		}

		public static byte[] Jpeg
		{
			get
			{
				if (jpeg == null)
				{
					using (MemoryStream memoryStream = new MemoryStream())
					{
						using (EncoderParameters encoderParameters = new EncoderParameters(1))
						{
							encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
							ImageCodecInfo encoder = null;
							ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
							foreach (ImageCodecInfo imageCodecInfo in imageEncoders)
							{
								if (imageCodecInfo.FormatID == ImageFormat.Jpeg.Guid)
								{
									encoder = imageCodecInfo;
								}
							}
							Watermark.Save(memoryStream, encoder, encoderParameters);
						}
						jpeg = memoryStream.ToArray();
					}
				}
				return jpeg;
			}
		}

		public static void Draw(Graphics g)
		{
			for (int i = -1; i < 7; i++)
			{
				for (int j = -1; j < 5; j++)
				{
					g.DrawImage(Watermark, 40 + j * 160 + (i + 2) % 2 * 80, (float)((double)i * 138.564), 126f, 126f);
				}
			}
		}

		public static string PdfCommands()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = -1; i < 7; i++)
			{
				for (int j = -1; j < 5; j++)
				{
					stringBuilder.Append("q 126 0 0 126 " + (40 + j * 160 + (i + 2) % 2 * 80) + " " + (841.89 - (double)i * 138.564 - 126.0).ToString("0.00", CultureInfo.InvariantCulture) + " cm /WM Do Q ");
				}
			}
			return stringBuilder.ToString();
		}
	}
}
