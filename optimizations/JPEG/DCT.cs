using System;

namespace JPEG;

public class DCT
{
	private static int _size;
	private static double[,] _cosineTable;
	private static double[] _alphaTable;
	private static double _beta;
	private static double[,] DCT2DTemp;
	private static double[,] IDCT2DTemp;

	public static void Initialize(int dctSize)
	{
		_size = dctSize;
		_cosineTable = new double[dctSize, dctSize];
		_alphaTable = new double[dctSize];
		DCT2DTemp = new double[dctSize, dctSize];
		IDCT2DTemp = new double[dctSize, dctSize];
		_beta = 2d / dctSize;
		for (var x = 0; x < dctSize; x++)
		{
			_alphaTable[x] = x == 0 ? 0.7071067811865475 : 1;
			for (var u = 0; u < dctSize; u++)
				_cosineTable[x, u] = Math.Cos((x + 0.5) * u * Math.PI / dctSize);
		}
	}

	public static void DCT2D(double[,] input, double[,] coeffs)
	{
		for (var u = 0; u < _size; u++)
		{
			for (var y = 0; y < _size; y++)
			{
				var sum = 0.0;
				for (var x = 0; x < _size; x++)
					sum += input[x, y] * _cosineTable[x, u];
				DCT2DTemp[y, u] = sum;
			}
		}

		for (var u = 0; u < _size; u++)
		{
			for (var v = 0; v < _size; v++)
			{
				var sum = 0.0;
				for (var y = 0; y < _size; y++)
					sum += DCT2DTemp[y, u] * _cosineTable[y, v];
				coeffs[u, v] = sum * _beta * _alphaTable[u] * _alphaTable[v];
			}
		}
	}

	public static void IDCT2D(double[,] coeffs, double[,] output)
	{
		for (var y = 0; y < _size; y++)
		{
			for (var u = 0; u < _size; u++)
			{
				var sum = 0.0;
				for (var v = 0; v < _size; v++)
					sum += coeffs[u, v] * _cosineTable[y, v] * _alphaTable[v];
				IDCT2DTemp[y, u] = sum * _alphaTable[u];
			}
		}

		for (var x = 0; x < _size; x++)
		{
			for (var y = 0; y < _size; y++)
			{
				var sum = 0.0;
				for (var u = 0; u < _size; u++)
					sum += IDCT2DTemp[y, u] * _cosineTable[x, u];
				output[x, y] = sum * _beta;
			}
		}
	}
}