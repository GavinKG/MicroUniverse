using System;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {


    public class WFC {

        // Useful LUTs
        protected static int[] DX = { -1, 0, 1, 0 };
        protected static int[] DY = { 0, 1, 0, -1 };
        static int[] opposite = { 2, 3, 0, 1 };

        // Baked Info (by ctor): 
        int N; // tile size
        byte[][] patterns; // D1: patterns, D2: a flattened pattern with length NxN
        double[] weights;
        List<byte> colors; // all colors (block types) occured in input (`sample` var)
        bool makeOutputRepeatable;
        int patternCount;
        int[][][] propagator; // D1: 4 propagate directions, D2: Possible patterns, D3: possible accompany patterns

        // Runtime info:
        int outputWidth, outputHeight;
        bool[][] wave; // D1: each output (flattened), D2: each pattern can be selected or not
        int[][][] compatible;
        int[] collapsed; // return value
        // bool init = false;
        Tuple<int, int>[] stack;
        int stacksize;
        System.Random randomNumberGenerator;
        double[] weightLogWeights;
        int[] sumsOfOnes;
        double sumOfWeights, sumOfWlogW, startingEntropy;
        double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;


        /// <summary>
        /// Ctor. Will bake a give bytemap sample.
        /// symmetry should be in 1~7
        /// </summary>
        public WFC(byte[,] sample, int N, bool inputRepeatable, bool outputRepeatable, int symmetryVariantCount) {

            this.N = N;
            makeOutputRepeatable = outputRepeatable;

            int sampleWidth = sample.GetLength(0), sampleHeight = sample.GetLength(1);

            colors = new List<byte>();
            colors.Add((byte)0); // 0: blank, empty, void, sad sad pixel...

            // get all colors (or block types, stored in byte) occured in sample pattern.
            for (int y = 0; y < sampleHeight; y++) {
                for (int x = 0; x < sampleWidth; x++) {
                    byte color = sample[x, y];

                    int i = 0;
                    foreach (var c in colors) {
                        if (c == color) break;
                        ++i;
                    }

                    if (i == colors.Count) colors.Add(color);
                }
            }

            long W = Util.PowerInt(colors.Count, N * N);

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
                return Flatten((dx, dy) => { return sample[(x + dx) % sampleWidth, (y + dy) % sampleHeight]; });
            }


            byte[] Rotate(byte[] p) {
                return Flatten((x, y) => { return p[N - 1 - y + x * N]; });
            }

            byte[] Reflect(byte[] p) {
                return Flatten((x, y) => { return p[N - 1 - x + y * N]; });
            }

            Dictionary<long, int> patternIndexKeyToWeightMap = new Dictionary<long, int>(); // hashed index (after r/f) -> sum of appearance (weight), used for counting patterns
            List<long> hashKeyOrdering = new List<long>();

            // pattern r/f, then record
            for (int y = 0; y < (inputRepeatable ? sampleHeight : sampleHeight - N + 1); y++) {
                for (int x = 0; x < (inputRepeatable ? sampleWidth : sampleWidth - N + 1); x++) {

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

                    for (int k = 0; k < symmetryVariantCount; k++) {
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

            Debug.Log("WFC loaded and baked from given " + sampleWidth.ToString() + "x" + sampleHeight.ToString() + " sample. " + patternCount.ToString() + " pattern(s) generated.");
        }


        /// <summary>
        /// --- WFC Core entry point. ---
        /// use give random seed if != 0
        /// </summary>
        public byte[,] Run(int outputWidth, int outputHeight, int seed) {

            this.outputWidth = outputWidth + N - 1;
            this.outputHeight = outputHeight + N - 1;

            //if (wave == null) Init();
            Init();
            Clear();
            /*
            if (!this.init) {
                this.init = true;
                this.Clear();
            }
            */

            if (seed == 0) {
                randomNumberGenerator = new System.Random();
            } else {
                randomNumberGenerator = new System.Random(seed);
            }

            // try collapse - propagate loop until it yields a useful data.
            bool shouldRun = true;
            int deadCounter = 0;
            while (shouldRun) {
                ++deadCounter;
                if (deadCounter == 5) {
                    Debug.Log("WFC: Okay I failed...CHANGE INPUT PATTERN!!!");
                    return null;
                }
                while (true) { // collapse - propagate loop
                    bool? result = Collapse();
                    if (result != null) {
                        if (result.Value) {
                            Debug.Log("WFC: succesfully generated an output pattern in " + outputWidth.ToString() + "x" + outputHeight.ToString());
                            shouldRun = false;
                            break;
                        } else {
                            Debug.Log("WFC: A contradiction has occured...but I will not give up!");
                            shouldRun = true;
                            break;
                        }
                    }
                    Propagate();
                }
            }

            // prepare output bytemap:
            byte[,] ret = new byte[outputWidth, outputHeight];
            for (int y = 0; y < outputHeight; ++y) {
                for (int x = 0; x < outputWidth; ++x) {
                    ret[x, y] = ResultSampler(x, y);
                }
            }

            return ret;
        }


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

        /// <summary>
        /// Is given coord on boundary?
        /// </summary>
        protected bool IsOnBoundary(int x, int y) {
            return !makeOutputRepeatable && (x + N > outputWidth || y + N > outputHeight || x < 0 || y < 0);
        }

        /// <summary>
        /// Sample from finished wave map. Should be called after collapsing.
        /// if not collapsed return 99 (for debugging purpose only, will not returning 99 if you run WFC from outer code).
        /// </summary>
        byte ResultSampler(int x, int y) {
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

        /// <summary>
        /// return true:  FINISHED!
        /// return false: FAILED!
        /// return null:  COLLAPSED ONE PIXEL.
        /// </summary>
        bool? Collapse() {
            double min = 1000;
            int minIndex = -1;

            for (int i = 0; i < wave.Length; i++) {
                if (IsOnBoundary(i % outputWidth, i / outputWidth)) continue;

                int amount = sumsOfOnes[i];
                if (amount == 0) return false; // we failed. nothing to choose. dead end.

                double entropy = entropies[i];
                if (amount > 1 && entropy <= min) {
                    double entropyNoise = 1E-6 * randomNumberGenerator.NextDouble();
                    if (entropy + entropyNoise < min) {
                        min = entropy + entropyNoise;
                        minIndex = i;
                    }
                }
            }

            if (minIndex == -1) {
                collapsed = new int[outputWidth * outputHeight];
                for (int i = 0; i < wave.Length; i++) {
                    for (int t = 0; t < patternCount; t++) {
                        if (wave[i][t]) {
                            collapsed[i] = t;
                            break;
                        }
                    }
                }
                return true; // we are done!!
            }

            double[] distribution = new double[patternCount];
            for (int t = 0; t < patternCount; t++) distribution[t] = wave[minIndex][t] ? weights[t] : 0;
            int r = distribution.Random(randomNumberGenerator.NextDouble());

            bool[] w = wave[minIndex];
            for (int t = 0; t < patternCount; t++) if (w[t] != (t == r)) Ban(minIndex, t);

            return null; // successfully collapsed one pixel. (like yield null in enumerator)
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

        /// <summary>
        /// Ban a wave's pattern posibility.
        /// </summary>
        protected void Ban(int waveIndex, int patternIndex) {
            wave[waveIndex][patternIndex] = false;

            int[] comp = compatible[waveIndex][patternIndex];
            for (int d = 0; d < 4; d++) comp[d] = 0;
            stack[stacksize] = new Tuple<int, int>(waveIndex, patternIndex);
            stacksize++;

            double sum = sumsOfWeights[waveIndex];
            entropies[waveIndex] += sumsOfWeightLogWeights[waveIndex] / sum - Math.Log(sum);

            sumsOfOnes[waveIndex] -= 1;
            sumsOfWeights[waveIndex] -= weights[patternIndex];
            sumsOfWeightLogWeights[waveIndex] -= weightLogWeights[patternIndex];

            sum = sumsOfWeights[waveIndex];
            entropies[waveIndex] -= sumsOfWeightLogWeights[waveIndex] / sum - Math.Log(sum);
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
        }


    }
}
