using System;
using System.Collections.Generic;
using System.Text;

namespace OpenPronounceSharp.Levenshtein
{
    public class LevenshteinHelper
    {
        public static (List<int>, List<int>) conv_sequences(List<string> s1, List<string> s2)
        {
            return (conv_sequence(s1), conv_sequence(s2));
        }

        public static List<int> conv_sequence(List<string> s)
        {
            var res = new List<int>();

            foreach (var elem in s)
            {
                if (elem is string && elem.Length == 1)
                {
                    res.Add((int)elem[0]);
                }
                else
                {
                    res.Add(elem.GetHashCode());
                }
            }

            return res;
        }
    }
}
