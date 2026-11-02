using PyException.common;
using SharpUtility.Helper;

namespace OpenPronounceSharp.Levenshtein
{
    /// <summary>
    /// List like object of Opcodes describing how to turn s1 into s2.
    /// The first Opcode has src_start == dest_start == 0, and remaining tuples
    /// have src_start == the src_end from the tuple preceding it,
    /// and likewise for dest_start == the previous dest_end.
    /// </summary>
    public class Opcodes
    {
        private int _src_len;
        private int _dest_len;
        internal List<Opcode> _opcodes;

        public List<Opcode> Items => this._opcodes;
        public int Count => this._opcodes?.Count ?? 0;

        public Opcodes(List<Opcode> opcodes = null, int src_len = 0, int dest_len = 0)
        {
            this._src_len = src_len;
            this._dest_len = dest_len;

            this._opcodes = _list_to_opcodes(opcodes, src_len, dest_len);
        }

        private static List<Opcode> _list_to_opcodes(List<Opcode> ops, int src_len, int dest_len)
        {
            if (ops is null || ops.Count == 0 || ops[0].len == 3)
            {
                return new Editops(ops.Select(item=>new Editop(item.tag,item.src_start,item.dest_start)).ToList(), src_len, dest_len).as_opcodes()._opcodes;
            }

            var blocks = new List<Opcode>();

            foreach (var op in ops)
            {
                string edit_type = op.tag;
                var src_start = op.src_start;
                var src_end = op.src_end;
                var dest_start = op.dest_start;
                var dest_end = op.dest_end;
                string msg = null;

                if (src_end > src_len || dest_end > dest_len)
                {
                    msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                if (src_end < src_start || dest_end < dest_start)
                {
                    msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                if (ArrayHelper.GetArray("equal", "replace").Contains(edit_type) && (src_end - src_start != dest_end - dest_start || src_start == src_end))
                {
                    msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                if (edit_type == "insert" && (src_start != src_end || dest_start == dest_end))
                {
                    msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                if (edit_type == "delete" && (src_start == src_end || dest_start != dest_end))
                {
                    msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                // merge similar adjacent blocks
                if (blocks.Count > 0 && (blocks.LastOrDefault().tag == edit_type && blocks.LastOrDefault().src_end == src_start && blocks.LastOrDefault().dest_end == dest_start))
                {
                    blocks[blocks.Count - 1].src_end = src_end;
                    blocks[blocks.Count - 1].dest_end = dest_end;
                    continue;
                }

                blocks.Add(new Opcode(edit_type, src_start, src_end, dest_start, dest_end));
            }

            // check if edit operations span the complete string
            if (blocks[0].src_start != 0 || blocks[0].dest_start != 0)
            {
                var msg = "List of edit operations does not start at position 0";
                throw new ValueError(msg);
            }

            if (blocks.LastOrDefault().src_end != src_len || blocks.LastOrDefault().dest_end != dest_len)
            {
                var msg = "List of edit operations does not end at the string ends";
                throw new ValueError(msg);
            }

            foreach(var i in RangeHelper.GetRange(blocks.Count - 1))
            {
                if(blocks[i + 1].src_start != blocks[i].src_end || blocks[i + 1].dest_start != blocks[i].dest_end)
                {
                    var msg = "List of edit operations is not continuous";
                    throw new ValueError(msg);
                }
            }

            return blocks;
        }

        /// <summary>
        /// Convert Opcodes to Editops
        /// </summary>
        /// <returns></returns>
        public Editops as_editops()
        {
            var blocks = new List<Editop>();

            foreach(var op in this._opcodes)
            {
                if(op.tag == "replace")
                {
                    foreach(var j in RangeHelper.GetRange(op.src_end - op.src_start))
                    {
                        blocks.Add(new Editop("replace", op.src_start + j, op.dest_start + j));
                    }
                }
                else if(op.tag == "insert")
                {
                    foreach(var j in RangeHelper.GetRange(op.dest_end - op.dest_start))
                    {
                        blocks.Add(new Editop("insert", op.src_start, op.dest_start + j));
                    }
                }
                else if(op.tag == "delete")
                {
                    foreach(var j in RangeHelper.GetRange(op.src_end - op.src_start))
                    {
                        blocks.Add(new Editop("delete", op.src_start + j, op.dest_start));
                    }
                }
            }

            var x = new Editops(blocks, this._src_len, this._dest_len);

            return x;
        }         
    }
}
