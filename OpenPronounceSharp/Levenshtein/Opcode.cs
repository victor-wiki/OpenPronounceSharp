using System;
using System.Collections.Generic;
using System.Text;
using TorchSharp.Modules;

namespace OpenPronounceSharp.Levenshtein
{
    /// <summary>
    /// Tuple like object describing an edit operation.
    /// It is in the form (tag, src_start, src_end, dest_start, dest_end)
    /// </summary>
    public class Opcode
    {
        public string tag { get; set; }
        public int src_start { get; set; }
        public int src_end { get; set; }
        public int dest_start { get; set; }
        public int dest_end { get; set; }
        public int len => 5;

        public Opcode(string tag,int src_start, int src_end, int dest_start, int dest_end)
        {
            this.tag = tag;
            this.src_start = src_start;
            this.src_end = src_end;
            this.dest_start = dest_start;
            this.dest_end = dest_end;
        }
    }
}
