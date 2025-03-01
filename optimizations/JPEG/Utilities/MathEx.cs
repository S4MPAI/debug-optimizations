using System;
using System.Linq;

namespace JPEG.Utilities;

public static class MathEx
{
	public static double Sum(int from, int to, Func<int, double> function)
		=> Enumerable.Range(from, to - from).Sum(function);

	public static double SumByTwoVariables(int from1, int to1, int from2, int to2, Func<int, int, double> function)
	{
		var sum = 0d;
		for (var i = from1; i < to1; i++)
			for (var j = from2; j < to2; j++)
				sum += function(i, j);

		return sum;
	}

	public static double LoopByTwoVariables(int from1, int to1, int from2, int to2, Action<int, int> function)
		=> Sum(from1, to1, x => Sum(from2, to2, y =>
		{
			function(x, y);
			return 0;
		}));
}