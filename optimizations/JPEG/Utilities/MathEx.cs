using System;
using System.Linq;

namespace JPEG.Utilities;

public static class MathEx
{
	public static double SumByTwoVariables(int from1, int to1, int from2, int to2, Func<int, int, double> function)
	{
		var sum = 0d;
		for (var i = from1; i < to1; i++)
			for (var j = from2; j < to2; j++)
				sum += function(i, j);

		return sum;
	}

	public static void LoopByTwoVariables(int from1, int to1, int from2, int to2, Action<int, int> function)
	{
		for (var i = from1; i < to1; i++)
			for (var j = from2; j < to2; j++)
				function(i, j);
	}
}