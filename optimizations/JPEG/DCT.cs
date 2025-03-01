using System;
using JPEG.Utilities;

namespace JPEG;

public class DCT
{
	private static int _size;
	private static double[,] _cosineTable;
	private static double[] _alphaTable;
	private static double _beta;

	public static void Initialize(int dctSize)
	{
		_size = dctSize;
		_cosineTable = new double[dctSize, dctSize];
		_alphaTable = new double[dctSize];
		_beta = 2d / dctSize;
		for (var x = 0; x < dctSize; x++)
		{
			_alphaTable[x] = x == 0 ? 0.7071067811865475 : 1;
			for (var u = 0; u < dctSize; u++)
				_cosineTable[x, u] = Math.Cos((x + 0.5) * u * Math.PI / dctSize);
		}
	}

	public static double[,] DCT2D(double[,] input)
	{
		var coeffs = new double[_size, _size];

		MathEx.LoopByTwoVariables(
			0, _size,
			0, _size,
			(u, v) =>
			{
				var sum = MathEx
					.SumByTwoVariables(
						0, _size,
						0, _size,
						(x, y) => input[x, y] * _cosineTable[x, u] * _cosineTable[y, v]);

				coeffs[u, v] = sum * _beta * _alphaTable[u] * _alphaTable[v];
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

				output[x, y] = sum * _beta;
			}
		}
	}
}