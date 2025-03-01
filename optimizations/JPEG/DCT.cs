using System;
using JPEG.Utilities;

namespace JPEG;

public class DCT
{
	private static int _size;
	private static double[,] _cosineTable;
	private static double[] _alphaTable;

	public static void Initialize(int dctSize)
	{
		_size = dctSize;
		_cosineTable = new double[dctSize, dctSize];
		_alphaTable = new double[dctSize];
		for (var x = 0; x < dctSize; x++)
		{
			_alphaTable[x] = x == 0 ? 0.7071067811865475 : 1;
			for (var u = 0; u < dctSize; u++)
				_cosineTable[x, u] = Math.Cos((x + 0.5) * u * Math.PI / dctSize);
		}
	}

	public static double[,] DCT2D(double[,] input)
	{
		var height = input.GetLength(0);
		var width = input.GetLength(1);
		var coeffs = new double[width, height];

		MathEx.LoopByTwoVariables(
			0, width,
			0, height,
			(u, v) =>
			{
				var sum = MathEx
					.SumByTwoVariables(
						0, width,
						0, height,
						(x, y) => BasisFunction(input[x, y], u, v, x, y, height, width));

				coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
			});

		return coeffs;
	}

	public static void IDCT2D(double[,] coeffs, double[,] output)
	{
		for (var x = 0; x < _size; x++)
		{
			for (var y = 0; y < _size; y++)
			{
				var sum = MathEx
					.SumByTwoVariables(
						0, _size,
						0, _size,
						(u, v) =>
							coeffs[u, v] * _cosineTable[x, u] * _cosineTable[y, v] * _alphaTable[u] * _alphaTable[v]);

				output[x, y] = sum * Beta(_size, _size);
			}
		}
	}

	public static double BasisFunction(double a, int u, int v, int x, int y, int height, int width)
	{
		var b = Math.Cos((x + 0.5) * u * Math.PI / width);
		var c = Math.Cos((y + 0.5) * v * Math.PI / height);

		return a * b * c;
	}

	private static double Alpha(int u) =>
		u == 0 ? 0.7071067811865475 : 1;

	private static double Beta(int height, int width)
	{
		return 1d / width + 1d / height;
	}
}