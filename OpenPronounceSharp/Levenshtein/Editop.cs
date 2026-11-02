using System;
using System.Collections.Generic;
using System.Text;
using TorchSharp.Modules;

namespace OpenPronounceSharp.Levenshtein
{
    /// <summary>
    /// Tuple like object describing an edit operation.
    /// It is in the form (tag, src_pos, dest_pos)
    /// </summary>
    public class Editop
    {
        public string tag { get; set; }
        public int src_pos { get; set; }
        public int dest_pos { get; set; }

        public int len => 3;

        public Editop(string tag, int src_pos, int dest_pos)
        {
            this.tag = tag;
            this.src_pos = src_pos;
            this.dest_pos = dest_pos;
        }
    }
}
