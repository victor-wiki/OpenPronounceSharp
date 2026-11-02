using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace OpenPronounceSharp.Levenshtein
{
    public class LevenshteinDistance
    {
        public static int distance(List<string> s1, List<string> s2)
        {
            var (s3, s4) = LevenshteinHelper.conv_sequences(s1, s2);

            var dist = _uniform_distance(s3, s4);

            return dist;
        }

        private static int _uniform_distance(List<int> s1, List<int> s2)
        {
            if (s1 is null || s1.Count == 0)
            {
                return s2.Count;
            }

            BigInteger VP = (BigInteger.One << s1.Count) - 1;
            BigInteger VN = 0;
            var currDist = s1.Count;
            BigInteger mask = BigInteger.One << (s1.Count - 1);

            var block = new Dictionary<BigInteger, BigInteger>();
            BigInteger x = 1;

            foreach (var ch1 in s1)
            {
                block[ch1] = block.GetValueOrDefault(ch1, 0) | x;
                x <<= 1;
            }

            foreach (var ch2 in s2)
            {
                // Step 1: Computing D0
                var PM_j = block.GetValueOrDefault(ch2, 0);
                var X = PM_j;
                var D0 = (((X & VP) + VP) ^ VP) | X | VN;

                // Step 2: Computing HP and HN
                var HP = VN | ~(D0 | VP);
                var HN = D0 & VP;

                // Step 3: Computing the value D[m,j]
                currDist += (HP & mask) != 0 ? 1 : 0;
                currDist -= (HN & mask) != 0 ? 1 : 0;

                // Step 4: Computing Vp and VN
                HP = (HP << 1) | 1;
                HN = HN << 1;
                VP = HN | ~(D0 | HP);
                VN = HP & D0;
            }

            return currDist;
        }
    }
}
