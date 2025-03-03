using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace JPEG.Images;

class Matrix
{
	private byte[] yPlane;
	private byte[] crPlane;
	private byte[] cbPlane;
	public readonly int Height;
	public readonly int Width;

	public Matrix(int height, int width)
	{
		Height = height;
		Width = width;
		yPlane = new byte[Height * Width];
		cbPlane = new byte[(width / 2) * (height / 2)];
		crPlane = new byte[(width / 2) * (height / 2)];
	}

	public void SetPixel(byte component1, byte component2, byte component3, int y, int x)
	{
		yPlane[y * Width + x] = component1;
		if (x % 2 != 0 || y % 2 != 0)
			return;
		cbPlane[(y / 2) * (Width / 2) + x / 2] = component2;
		crPlane[(y / 2) * (Width / 2) + x / 2] = component3;
	}

	public byte GetComponentValue(int component, int y, int x)
	{
		return component switch
		{
			0 => yPlane[y * Width + x],
			1 => cbPlane[(y / 2) * (Width / 2) + x / 2],
			2 => crPlane[(y / 2) * (Width / 2) + x / 2],
			_ => throw new ArgumentOutOfRangeException(nameof(component), component, null)
		};
	}

	public static unsafe explicit operator Matrix(Bitmap bmp)
	{
		var height = bmp.Height - bmp.Height % 8;
		var width = bmp.Width - bmp.Width % 8;
		var matrix = new Matrix(height, width);
		var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
			System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		var ptr = (byte*)bmpData.Scan0;
		var stride = bmpData.Stride;

		for (var y = 0; y < height; y++)
		{
			var rowOffset = y * stride;
			for (var x = 0; x < width; x++)
			{
				var offset = ptr + rowOffset + x * 3;
				var r = *(offset + 2);
				var g = *(offset + 1);
				var b = *offset;

				var _y = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
				var cb = (byte)(-0.168736*r - 0.331264*g + 0.5*b + 128);
				var cr = (byte)(0.5 * r - 0.418688 * g - 0.081312 * b + 128);

				var indexY = y * width + x;
				matrix.yPlane[indexY] = _y;

				if (x % 2 != 0 || y % 2 != 0)
					continue;

				var indexCbCr = (y / 2) * (width / 2) + (x / 2);
				matrix.cbPlane[indexCbCr] = cb;
				matrix.crPlane[indexCbCr] = cr;
			}
		}

		return matrix;
	}

	public static unsafe explicit operator Bitmap(Matrix matrix)
	{
		var width = matrix.Width;
		var height = matrix.Height;
		var bmp = new Bitmap(width, height);
		var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
			System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		var ptr = (byte*)bmpData.Scan0;
		var stride = bmpData.Stride;

		for (var y = 0; y < height; y++)
		{
			var rowOffset = y * stride;
			for (var x = 0; x < width; x++)
			{
				var indexY = y * width + x;
				var indexUV = (y / 2) * (width / 2) + x / 2;

				var _y = matrix.yPlane[indexY];
				var cb = matrix.cbPlane[indexUV];
				var cr = matrix.crPlane[indexUV];

				var offset = ptr + rowOffset + x * 3;
				*(offset + 2) = ToByte(_y + 1.402 * (cr - 128));
				*(offset + 1) = ToByte(_y - 0.344136 * (cb - 128) - 0.714136 * (cr - 128));
				*offset = ToByte(_y + 1.772 * (cb - 128));
			}
		}
		bmp.UnlockBits(bmpData);

		return bmp;
	}

	private static byte ToByte(double d)
	{
		return d switch
		{
			> byte.MaxValue => byte.MaxValue,
			< byte.MinValue => byte.MinValue,
			_ => (byte)d
		};
	}
}