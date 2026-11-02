using System.Numerics;

namespace OpenPronounceSharp.Levenshtein
{
    public class LevenshteinOpcodes
    {
        public static Opcodes opcodes(List<string> s1, List<string> s2)
        {
            var _editops = editops(s1, s2);

            return _editops.as_opcodes();
        }

        private static Editops editops(List<string> s1, List<string> s2)
        {
            var (s3, s4) = LevenshteinHelper.conv_sequences(s1, s2);

            var (prefix_len, suffix_len) = common_affix(s3, s4);

            s3 = s3.Skip(prefix_len).Take(s3.Count - suffix_len - prefix_len).ToList();
            s4 = s4.Skip(prefix_len).Take(s4.Count - suffix_len - prefix_len).ToList();
            var (dist, VP, VN) = _matrix(s3, s4);

            var editops = new Editops([], 0, 0);
            editops._src_len = s3.Count + prefix_len + suffix_len;
            editops._dest_len = s4.Count + prefix_len + suffix_len;

            if (dist == 0)
            {
                return editops;
            }

            var editop_list = Enumerable.Repeat(default(Editop), dist).ToList();
            var col = s3.Count;
            var row = s4.Count;

            while (row != 0 && col != 0)
            {
                // deletion
                if ((VP[row - 1] & (BigInteger.One << (col - 1))) != 0)
                {
                    dist -= 1;
                    col -= 1;
                    editop_list[dist] = new Editop("delete", col + prefix_len, row + prefix_len);
                }
                else
                {
                    row -= 1;

                    // insertion
                    if (row > 0 && (VN[row - 1] & (BigInteger.One << (col - 1))) != 0)
                    {
                        dist -= 1;
                        editop_list[dist] = new Editop("insert", col + prefix_len, row + prefix_len);
                    }
                    else
                    {
                        col -= 1;

                        // replace (Matches are not recorded)
                        if (s3[col] != s4[row])
                        {
                            dist -= 1;
                            editop_list[dist] = new Editop("replace", col + prefix_len, row + prefix_len);
                        }
                    }
                }
            }

            while (col != 0)
            {
                dist -= 1;
                col -= 1;
                editop_list[dist] = new Editop("delete", col + prefix_len, row + prefix_len);
            }

            while (row != 0)
            {
                dist -= 1;
                row -= 1;
                editop_list[dist] = new Editop("insert", col + prefix_len, row + prefix_len);
            }

            editops._editops = editop_list;

            return editops;
        }

        private static (int, List<BigInteger>, List<BigInteger>) _matrix(List<int> s1, List<int> s2)
        {
            if (s1 is null || s1.Count == 0)
            {
                return (s2.Count, [], []);
            }

            BigInteger VP = (BigInteger.One << s1.Count) - 1;
            BigInteger VN = 0;
            var currDist = s1.Count;
            BigInteger mask = BigInteger.One << (s1.Count - 1);

            var block = new Dictionary<int, BigInteger>();
            BigInteger x = 1;

            foreach (var ch1 in s1)
            {
                block[ch1] = block.GetValueOrDefault(ch1, 0l) | x;
                x <<= 1;
            }

            var matrix_VP = new List<BigInteger>();
            var matrix_VN = new List<BigInteger>();

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

                matrix_VP.Add(VP);
                matrix_VN.Add(VN);
            }

            return (currDist, matrix_VP, matrix_VN);
        }

        public static (int, int) common_affix(List<int> s1, List<int> s2)
        {
            var prefix_len = common_prefix(s1, s2);
            var suffix_len = common_suffix(s1.Skip(prefix_len).ToList(), s2.Skip(prefix_len).ToList());

            return (prefix_len, suffix_len);
        }

        public static int common_suffix(List<int> s1, List<int> s2)
        {
            var suffix_len = 0;

            foreach (var (ch1, ch2) in Enumerable.Reverse(s1).Zip(Enumerable.Reverse(s2), (k, v) => (k, v)))
            {
                if (ch1 != ch2)
                {
                    break;
                }

                suffix_len += 1;
            }

            return suffix_len;
        }

        public static int common_prefix(List<int> s1, List<int> s2)
        {
            var prefix_len = 0;

            foreach (var (ch1, ch2) in s1.Zip(s2, (k, v) => (k, v)))
            {
                if (ch1 != ch2)
                {
                    break;
                }

                prefix_len += 1;
            }

            return prefix_len;
        }
    }
}
