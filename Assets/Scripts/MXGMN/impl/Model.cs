using System;

public abstract class Model
{
	protected bool[][] wave; // D1: each output (flattened), D2: pattern

	protected int[][][] propagator; // D1: 4 propagate directions, D2: Possible patterns, D3: possible accompany patterns
    int[][][] compatible;
	protected int[] observed;

	protected bool init = false;

	Tuple<int, int>[] stack;
	int stacksize;

	protected System.Random random;
	protected int outputWidth, outputHeight, patternCount;
	protected bool periodic;

	protected double[] weights;
	double[] weightLogWeights;

	int[] sumsOfOnes;
	double sumOfWeights, sumOfWlogW, startingEntropy;
	double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

	protected Model(int width, int height)
	{
		outputWidth = width;
		outputHeight = height;

        // other variables should be baked by derived class.
        // can pre-bake these info and serialize them to disk.
	}

    /// <summary>
    /// Init WFC Core.
    /// </summary>
	void Init()
	{
        int outputSize = outputWidth * outputHeight;
        wave = new bool[outputSize][]; // D1: each output (flattened), D2: pattern
        compatible = new int[outputSize][][]; // D1: each output (flattened), D2: pattern D3: 4 directions
        for (int i = 0; i < outputSize; i++)
		{
			wave[i] = new bool[patternCount];
			compatible[i] = new int[patternCount][];
			for (int t = 0; t < patternCount; t++) compatible[i][t] = new int[4];
		}

		weightLogWeights = new double[patternCount]; // entropy w*log(w)
		sumOfWeights = 0;
		sumOfWlogW = 0;

		for (int t = 0; t < patternCount; t++)
		{
			weightLogWeights[t] = weights[t] * Math.Log(weights[t]); // actual w*log(w) calc
            sumOfWeights += weights[t];
			sumOfWlogW += weightLogWeights[t];
		}

		startingEntropy = Math.Log(sumOfWeights) - sumOfWlogW / sumOfWeights;

		sumsOfOnes = new int[outputSize];
		sumsOfWeights = new double[outputSize];
		sumsOfWeightLogWeights = new double[outputSize];
		entropies = new double[outputSize];
		
		stack = new Tuple<int, int>[outputSize * patternCount];
		stacksize = 0;
	}

	

	bool? Observe()
	{
		double min = 1000;
		int minIndex = -1;

		for (int i = 0; i < wave.Length; i++)
		{
			if (IsOnBoundary(i % outputWidth, i / outputWidth)) continue;

			int amount = sumsOfOnes[i];
			if (amount == 0) return false;

			double entropy = entropies[i];
			if (amount > 1 && entropy <= min)
			{
				double entropyNoise = 1E-6 * random.NextDouble();
				if (entropy + entropyNoise < min)
				{
					min = entropy + entropyNoise;
					minIndex = i;
				}
			}
		}

		if (minIndex == -1)
		{
			observed = new int[outputWidth * outputHeight];
            for (int i = 0; i < wave.Length; i++) {
                for (int t = 0; t < patternCount; t++) {
                    if (wave[i][t]) {
                        observed[i] = t;
                        break;
                    }
                }
            }
			return true;
		}

		double[] distribution = new double[patternCount];
		for (int t = 0; t < patternCount; t++) distribution[t] = wave[minIndex][t] ? weights[t] : 0;
		int r = distribution.Random(random.NextDouble());
		
		bool[] w = wave[minIndex];
		for (int t = 0; t < patternCount; t++)	if (w[t] != (t == r)) Ban(minIndex, t);

		return null;
	}

	protected void Propagate()
	{
		while (stacksize > 0)
		{
			var e1 = stack[stacksize - 1];
			stacksize--;

			int i1 = e1.Item1;
			int x1 = i1 % outputWidth, y1 = i1 / outputWidth;
			bool[] w1 = wave[i1];

			for (int d = 0; d < 4; d++)
			{
				int dx = DX[d], dy = DY[d];
				int x2 = x1 + dx, y2 = y1 + dy;
				if (IsOnBoundary(x2, y2)) continue;

				if (x2 < 0) x2 += outputWidth;
				else if (x2 >= outputWidth) x2 -= outputWidth;
				if (y2 < 0) y2 += outputHeight;
				else if (y2 >= outputHeight) y2 -= outputHeight;

				int i2 = x2 + y2 * outputWidth;
				int[] p = propagator[d][e1.Item2];
				int[][] compat = compatible[i2];

				for (int l = 0; l < p.Length; l++)
				{
					int t2 = p[l];
					int[] comp = compat[t2];

					comp[d]--;
					if (comp[d] == 0) Ban(i2, t2);
				}
			}
		}
	}

	public bool Run(int seed, int limit)
	{
		if (wave == null) Init();

		if (!this.init) {
			this.init = true;
			this.Clear();
		}

		if (seed==0) {
			random = new System.Random();
		} else {
			random = new System.Random(seed);
		}

		for (int l = 0; l < limit || limit == 0; l++)
		{
			bool? result = Observe();
			if (result != null) return (bool)result;
			Propagate();
		}

		return true;
	}

	protected void Ban(int i, int t)
	{
		wave[i][t] = false;

		int[] comp = compatible[i][t];
		for (int d = 0; d < 4; d++) comp[d] = 0;
		stack[stacksize] = new Tuple<int, int>(i, t);
		stacksize++;

		double sum = sumsOfWeights[i];
		entropies[i] += sumsOfWeightLogWeights[i] / sum - Math.Log(sum);

		sumsOfOnes[i] -= 1;
		sumsOfWeights[i] -= weights[t];
		sumsOfWeightLogWeights[i] -= weightLogWeights[t];

		sum = sumsOfWeights[i];
		entropies[i] -= sumsOfWeightLogWeights[i] / sum - Math.Log(sum);
	}

	protected virtual void Clear()
	{
		for (int i = 0; i < wave.Length; i++)
		{
			for (int t = 0; t < patternCount; t++)
			{
				wave[i][t] = true;
				for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
			}

			sumsOfOnes[i] = weights.Length;
			sumsOfWeights[i] = sumOfWeights;
			sumsOfWeightLogWeights[i] = sumOfWlogW;
			entropies[i] = startingEntropy;
		}
	}

	protected abstract bool IsOnBoundary(int x, int y);

	protected static int[] DX = { -1,  0,  1,  0 };
	protected static int[] DY = { 0 ,  1,  0, -1 };
	static int[] opposite = { 2, 3, 0, 1 };
}