using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JPEG.Images;

namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
	public static readonly JpegProcessor Init = new();
	public const int CompressionQuality = 70;
	private const int DCTSize = 8;
	private static DCT _dct = new(DCTSize);

	public void Compress(string imagePath, string compressedImagePath)
	{
		using var fileStream = File.OpenRead(imagePath);
		using var bmp = (Bitmap)Image.FromStream(fileStream, false, false);
		var imageMatrix = (Matrix)bmp;
		//Console.WriteLine($"{bmp.Width}x{bmp.Height} - {fileStream.Length / (1024.0 * 1024):F2} MB");
		var compressionResult = Compress(imageMatrix, CompressionQuality);
		compressionResult.Save(compressedImagePath);
	}

	public void Uncompress(string compressedImagePath, string uncompressedImagePath)
	{
		var compressedImage = CompressedImage.Load(compressedImagePath);
		var uncompressedImage = Uncompress(compressedImage);
		using var resultBmp = (Bitmap)uncompressedImage;
		resultBmp.Save(uncompressedImagePath, ImageFormat.Bmp);
	}

	private static CompressedImage Compress(Matrix matrix, int quality = 50)
	{
		var allQuantizedBytes = new byte[matrix.Width * matrix.Height * 3];
		var quantizationMatrix = GetQuantizationMatrix(quality);


		Parallel.For(0, matrix.Height / DCTSize, blockY =>
		{
			var y = blockY * DCTSize;
			var channelFreqs = new double[DCTSize, DCTSize];
			var subMatrix = new double[DCTSize, DCTSize];
			for (var x = 0; x < matrix.Width; x += DCTSize)
			{
				var j = 0;
				for (var i = 0; i < 3; i++)
				{
					GetSubMatrix(matrix, y, DCTSize, x, DCTSize, i, subMatrix);
					ShiftMatrixValues(subMatrix, -128);
					_dct.DCT2D(subMatrix, channelFreqs);
					var quantizedFreqs = Quantize(channelFreqs, quantizationMatrix);
					var start = 3 * (y * matrix.Width + x * DCTSize) + DCTSize * DCTSize * j;
					Array.Copy(ZigZagScan(quantizedFreqs), 0, allQuantizedBytes, start, 64);
					j++;
				}
			}
		});

		var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var decodeTable, out var bitsCount);

		return new CompressedImage
		{
			Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount, DecodeTable = decodeTable,
			Height = matrix.Height, Width = matrix.Width
		};
	}

	private static Matrix Uncompress(CompressedImage image)
	{
		var result = new Matrix(image.Height, image.Width);
		using var allQuantizedBytes =
			new MemoryStream(HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount));
		var quantizationMatrix = GetQuantizationMatrix(image.Quality);
		var blocksY = new List<(int y, byte[] bytes)>(image.Height / 8);
		for (var y = 0; y < image.Height; y += DCTSize)
		{
			var quantizedBytes = new byte[24 * image.Width];
			allQuantizedBytes.ReadExactly(quantizedBytes, 0, quantizedBytes.Length);
			blocksY.Add((y, quantizedBytes));
		}

		Parallel.ForEach(blocksY, block =>
		{
			var _y = new double[DCTSize, DCTSize];
			var cb = new double[DCTSize, DCTSize];
			var cr = new double[DCTSize, DCTSize];
			for (var x = 0; x < image.Width; x += DCTSize)
			{
				var i = 0;
				foreach(var channel in new[] { _y, cb, cr })
				{
					var channelBytes = block.bytes.AsSpan( 24 * x + i * 64, 64);
					var quantizedFreqs = ZigZagUnScan(channelBytes);
					DeQuantize(quantizedFreqs, quantizationMatrix);
					_dct.IDCT2D(quantizedFreqs, channel);
					ShiftMatrixValues(channel, 128);
					i++;
				}
				SetPixels(result, _y, cb, cr, block.y, x);
			}
		});


		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ShiftMatrixValues(double[,] subMatrix, int shiftValue)
	{
		var height = subMatrix.GetLength(0);
		var width = subMatrix.GetLength(1);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			subMatrix[y, x] += shiftValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetPixels(Matrix matrix, double[,] a, double[,] b, double[,] c, int yOffset, int xOffset)
	{
		for (var y = 0; y < DCTSize; y++)
			for (var x = 0; x < DCTSize; x++)
				matrix.SetPixel((byte)a[y, x], (byte)b[y, x], (byte)c[y, x], yOffset + y, xOffset + x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void GetSubMatrix(Matrix matrix, int yOffset, int yLength, int xOffset, int xLength,
		int component, double[,] buffer)
	{
		for (var j = 0; j < yLength; j++)
			for (var i = 0; i < xLength; i++)
				buffer[j, i] = matrix.GetComponentValue(component, yOffset + j, xOffset + i);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte[] ZigZagScan(byte[,] channelFreqs)
	{
		return new[]
		{
			channelFreqs[0, 0], channelFreqs[0, 1], channelFreqs[1, 0], channelFreqs[2, 0], channelFreqs[1, 1],
			channelFreqs[0, 2], channelFreqs[0, 3], channelFreqs[1, 2],
			channelFreqs[2, 1], channelFreqs[3, 0], channelFreqs[4, 0], channelFreqs[3, 1], channelFreqs[2, 2],
			channelFreqs[1, 3], channelFreqs[0, 4], channelFreqs[0, 5],
			channelFreqs[1, 4], channelFreqs[2, 3], channelFreqs[3, 2], channelFreqs[4, 1], channelFreqs[5, 0],
			channelFreqs[6, 0], channelFreqs[5, 1], channelFreqs[4, 2],
			channelFreqs[3, 3], channelFreqs[2, 4], channelFreqs[1, 5], channelFreqs[0, 6], channelFreqs[0, 7],
			channelFreqs[1, 6], channelFreqs[2, 5], channelFreqs[3, 4],
			channelFreqs[4, 3], channelFreqs[5, 2], channelFreqs[6, 1], channelFreqs[7, 0], channelFreqs[7, 1],
			channelFreqs[6, 2], channelFreqs[5, 3], channelFreqs[4, 4],
			channelFreqs[3, 5], channelFreqs[2, 6], channelFreqs[1, 7], channelFreqs[2, 7], channelFreqs[3, 6],
			channelFreqs[4, 5], channelFreqs[5, 4], channelFreqs[6, 3],
			channelFreqs[7, 2], channelFreqs[7, 3], channelFreqs[6, 4], channelFreqs[5, 5], channelFreqs[4, 6],
			channelFreqs[3, 7], channelFreqs[4, 7], channelFreqs[5, 6],
			channelFreqs[6, 5], channelFreqs[7, 4], channelFreqs[7, 5], channelFreqs[6, 6], channelFreqs[5, 7],
			channelFreqs[6, 7], channelFreqs[7, 6], channelFreqs[7, 7]
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double[,] ZigZagUnScan(Span<byte> quantizedBytes)
	{
		return new double[,]
		{
			{
				quantizedBytes[0], quantizedBytes[1], quantizedBytes[5], quantizedBytes[6], quantizedBytes[14],
				quantizedBytes[15], quantizedBytes[27], quantizedBytes[28]
			},
			{
				quantizedBytes[2], quantizedBytes[4], quantizedBytes[7], quantizedBytes[13], quantizedBytes[16],
				quantizedBytes[26], quantizedBytes[29], quantizedBytes[42]
			},
			{
				quantizedBytes[3], quantizedBytes[8], quantizedBytes[12], quantizedBytes[17], quantizedBytes[25],
				quantizedBytes[30], quantizedBytes[41], quantizedBytes[43]
			},
			{
				quantizedBytes[9], quantizedBytes[11], quantizedBytes[18], quantizedBytes[24], quantizedBytes[31],
				quantizedBytes[40], quantizedBytes[44], quantizedBytes[53]
			},
			{
				quantizedBytes[10], quantizedBytes[19], quantizedBytes[23], quantizedBytes[32], quantizedBytes[39],
				quantizedBytes[45], quantizedBytes[52], quantizedBytes[54]
			},
			{
				quantizedBytes[20], quantizedBytes[22], quantizedBytes[33], quantizedBytes[38], quantizedBytes[46],
				quantizedBytes[51], quantizedBytes[55], quantizedBytes[60]
			},
			{
				quantizedBytes[21], quantizedBytes[34], quantizedBytes[37], quantizedBytes[47], quantizedBytes[50],
				quantizedBytes[56], quantizedBytes[59], quantizedBytes[61]
			},
			{
				quantizedBytes[35], quantizedBytes[36], quantizedBytes[48], quantizedBytes[49], quantizedBytes[57],
				quantizedBytes[58], quantizedBytes[62], quantizedBytes[63]
			}
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte[,] Quantize(double[,] channelFreqs, int[,] quantizationMatrix)
	{
		var result = new byte[channelFreqs.GetLength(0), channelFreqs.GetLength(1)];

		for (var y = 0; y < channelFreqs.GetLength(0); y++)
			for (var x = 0; x < channelFreqs.GetLength(1); x++)
				result[y, x] = (byte)(channelFreqs[y, x] / quantizationMatrix[y, x]);

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void DeQuantize(double[,] bytes, int[,] quantizationMatrix)
	{
		var width = bytes.GetLength(0);
		var height = bytes.GetLength(1);

		for (var y = 0; y < width; y++)
			for (var x = 0; x < height; x++)
				bytes[y, x] = (sbyte)bytes[y, x] * quantizationMatrix[y, x]; //NOTE cast to sbyte not to loose negative numbers
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int[,] GetQuantizationMatrix(int quality)
	{
		if (quality is < 1 or > 99)
			throw new ArgumentException("quality must be in [1,99] interval");

		var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

		var result = new[,]
		{
			{ 16, 11, 10, 16, 24, 40, 51, 61 },
			{ 12, 12, 14, 19, 26, 58, 60, 55 },
			{ 14, 13, 16, 24, 40, 57, 69, 56 },
			{ 14, 17, 22, 29, 51, 87, 80, 62 },
			{ 18, 22, 37, 56, 68, 109, 103, 77 },
			{ 24, 35, 55, 64, 81, 104, 113, 92 },
			{ 49, 64, 78, 87, 103, 121, 120, 101 },
			{ 72, 92, 95, 98, 112, 100, 103, 99 }
		};

		for (var y = 0; y < 8; y++)
			for (var x = 0; x < 8; x++)
				result[y, x] = (multiplier * result[y, x] + 50) / 100;

		return result;
	}
}