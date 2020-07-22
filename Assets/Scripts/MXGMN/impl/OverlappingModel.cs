using System;
using System.Collections.Generic;
using UnityEngine;

class OverlappingModel {

    int N; // tile size

    public byte[][] patterns; // D1: patterns, D2: a flattened pattern with length NxN
    int ground;
    public List<byte> colors; // all colors (block types) occured in input (`sample` var)

    // BASE:
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

    /// <summary>
    /// Init WFC Core.
    /// </summary>
    void Init() {
        int outputSize = outputWidth * outputHeight;
        wave = new bool[outputSize][]; // D1: each output (flattened), D2: pattern
        compatible = new int[outputSize][][]; // D1: each output (flattened), D2: pattern D3: 4 directions
        for (int i = 0; i < outputSize; i++) {
            wave[i] = new bool[patternCount];
            compatible[i] = new int[patternCount][];
            for (int t = 0; t < patternCount; t++) compatible[i][t] = new int[4];
        }

        weightLogWeights = new double[patternCount]; // entropy w*log(w)
        sumOfWeights = 0;
        sumOfWlogW = 0;

        for (int t = 0; t < patternCount; t++) {
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


    public OverlappingModel(byte[,] sample, int N, int width, int height, bool periodicInput, bool periodicOutput, int symmetry, int ground) {

        outputWidth = width;
        outputHeight = height;

        this.N = N;
        periodic = periodicOutput;

        int sampleXSize = sample.GetLength(0), sampleYSize = sample.GetLength(1);

        colors = new List<byte>();
        colors.Add((byte)0);

        // get all colors (or block types, stored in byte) occured in sample pattern.
        for (int y = 0; y < sampleYSize; y++) {
            for (int x = 0; x < sampleXSize; x++) {
                byte color = sample[x, y];

                int i = 0;
                foreach (var c in colors) {
                    if (c == color) break;
                    i++;
                }

                if (i == colors.Count) colors.Add(color);
            }
        }

        long W = Stuff.Power(colors.Count, N * N);

        byte[] Flatten(Func<int, int, byte> GridGetter) {
            byte[] result = new byte[N * N];
            for (int y = 0; y < N; y++) {
                for (int x = 0; x < N; x++) {
                    result[x + y * N] = GridGetter(x, y);
                }
            }
            return result;
        }

        byte[] patternFromSample(int x, int y) {
            return Flatten((dx, dy) => { return sample[(x + dx) % sampleXSize, (y + dy) % sampleYSize]; });
        }
        Func<byte[], byte[]> Rotate = (p) => { return Flatten((x, y) => { return p[N - 1 - y + x * N]; }); };
        Func<byte[], byte[]> Reflect = (p) => { return Flatten((x, y) => { return p[N - 1 - x + y * N]; }); };



        Dictionary<long, int> patternIndexKeyToWeightMap = new Dictionary<long, int>(); // hashed index (after r/f) -> sum of appearance (weight), used for counting patterns
        List<long> hashKeyOrdering = new List<long>();

        // pattern r/f, then record
        for (int y = 0; y < (periodicInput ? sampleYSize : sampleYSize - N + 1); y++) {
            for (int x = 0; x < (periodicInput ? sampleXSize : sampleXSize - N + 1); x++) {

                byte[][] ps = new byte[8][];

                // 8 possible r/f
                ps[0] = patternFromSample(x, y);
                ps[1] = Reflect(ps[0]);
                ps[2] = Rotate(ps[0]);
                ps[3] = Reflect(ps[2]);
                ps[4] = Rotate(ps[2]);
                ps[5] = Reflect(ps[4]);
                ps[6] = Rotate(ps[4]);
                ps[7] = Reflect(ps[6]);

                for (int k = 0; k < symmetry; k++) {
                    long indexHashKey = HashPattern(ps[k]);
                    if (patternIndexKeyToWeightMap.ContainsKey(indexHashKey)) patternIndexKeyToWeightMap[indexHashKey]++;
                    else {
                        patternIndexKeyToWeightMap.Add(indexHashKey, 1);
                        hashKeyOrdering.Add(indexHashKey);
                    }
                }
            }
        }

        patternCount = patternIndexKeyToWeightMap.Count;
        Debug.Log("WFC: Pattern Count: " + patternCount.ToString());
        this.ground = (ground + patternCount) % patternCount;

        patterns = new byte[patternCount][];
        weights = new double[patternCount];

        for (int i = 0; i < hashKeyOrdering.Count; ++i) {
            patterns[i] = ReconstructPatternFromHash(hashKeyOrdering[i], W);
            weights[i] = patternIndexKeyToWeightMap[hashKeyOrdering[i]];
        }

        // init propagator
        propagator = new int[4][][]; // D1: 4 propagate directions, D2: Possible patterns, D3: possible accompany patterns
        for (int d = 0; d < 4; d++) { // for each direction: (left up right down)
            propagator[d] = new int[patternCount][];
            for (int t = 0; t < patternCount; t++) { // for each possible pattern t:
                List<int> list = new List<int>();
                for (int t2 = 0; t2 < patternCount; t2++) { // for each possible pattern that can sit alongside pattern t
                    if (CanPlaceP2AlongsideP1(patterns[t], patterns[t2], DX[d], DY[d])) {
                        list.Add(t2);
                    }
                }
                propagator[d][t] = new int[list.Count];
                for (int c = 0; c < list.Count; c++) {
                    propagator[d][t][c] = list[c];
                }
            }
        }
    }


    /// <summary>
    /// Is given coord on boundary?
    /// </summary>
    protected bool IsOnBoundary(int x, int y) {
        return !periodic && (x + N > outputWidth || y + N > outputHeight || x < 0 || y < 0);
    }

    /// <summary>
    /// Sample from Grid.
    /// </summary>
    public byte Sampler(int x, int y) {
        bool found = false;
        byte ret = (byte)99;
        for (int t = 0; t < patternCount; t++) {
            if (wave[x + y * outputWidth][t]) {
                if (found) {
                    return (byte)99;
                }
                found = true;
                ret = patterns[t][0];
            }
        }
        return ret;
    }

    /// <summary>
    /// Core overlapping placement filter.
    /// </summary>
    bool CanPlaceP2AlongsideP1(byte[] p1, byte[] p2, int dx, int dy) {

        int xmin = dx < 0 ? 0 : dx,
            xmax = dx < 0 ? dx + N : N,
            ymin = dy < 0 ? 0 : dy, 
            ymax = dy < 0 ? dy + N : N;

        for (int y = ymin; y < ymax; y++) {
            for (int x = xmin; x < xmax; x++) {
                if (p1[x + N * y] != p2[x - dx + N * (y - dy)]) {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// compress (for easy hashing) ...
    /// </summary>
    long HashPattern(byte[] p) {
        long ret = 0, power = 1;
        for (int i = 0; i < p.Length; i++) {
            ret += p[p.Length - 1 - i] * power;
            power *= colors.Count;
        }
        return ret;
    }

    /// <summary>
    /// ... and reconstruct
    /// </summary>
    byte[] ReconstructPatternFromHash(long patternHash, long W) {
        long residue = patternHash, power = W;
        byte[] result = new byte[N * N];

        for (int i = 0; i < result.Length; i++) {
            power /= colors.Count;
            int count = 0;

            while (residue >= power) {
                residue -= power;
                count++;
            }

            result[i] = (byte)count;
        }

        return result;
    }


















    // CORE:
    bool? Observe() {
        double min = 1000;
        int minIndex = -1;

        for (int i = 0; i < wave.Length; i++) {
            if (IsOnBoundary(i % outputWidth, i / outputWidth)) continue;

            int amount = sumsOfOnes[i];
            if (amount == 0) return false;

            double entropy = entropies[i];
            if (amount > 1 && entropy <= min) {
                double entropyNoise = 1E-6 * random.NextDouble();
                if (entropy + entropyNoise < min) {
                    min = entropy + entropyNoise;
                    minIndex = i;
                }
            }
        }

        if (minIndex == -1) {
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
        for (int t = 0; t < patternCount; t++) if (w[t] != (t == r)) Ban(minIndex, t);

        return null;
    }

    protected void Propagate() {
        while (stacksize > 0) {
            var e1 = stack[stacksize - 1];
            stacksize--;

            int i1 = e1.Item1;
            int x1 = i1 % outputWidth, y1 = i1 / outputWidth;
            bool[] w1 = wave[i1];

            for (int d = 0; d < 4; d++) {
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

                for (int l = 0; l < p.Length; l++) {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    comp[d]--;
                    if (comp[d] == 0) Ban(i2, t2);
                }
            }
        }
    }

    public bool Run(int seed, int limit) {
        if (wave == null) Init();

        if (!this.init) {
            this.init = true;
            this.Clear();
        }

        if (seed == 0) {
            random = new System.Random();
        } else {
            random = new System.Random(seed);
        }

        for (int l = 0; l < limit || limit == 0; l++) {
            bool? result = Observe();
            if (result != null) return (bool)result;
            Propagate();
        }

        return true;
    }

    protected void Ban(int i, int t) {
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

    protected void Clear() {
        for (int i = 0; i < wave.Length; i++) {
            for (int t = 0; t < patternCount; t++) {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
            }

            sumsOfOnes[i] = weights.Length;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWlogW;
            entropies[i] = startingEntropy;
        }

        if (ground != 0) {
            for (int x = 0; x < outputWidth; x++) {

                //bottom
                for (int t = 0; t < patternCount; t++) if (t != ground) Ban(x, t);

            }
            Propagate();
        }
    }

    protected static int[] DX = { -1, 0, 1, 0 };
    protected static int[] DY = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };
}