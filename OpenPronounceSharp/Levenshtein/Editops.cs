using PyException.common;
using SharpUtility.Helper;

namespace OpenPronounceSharp.Levenshtein
{
    /// <summary>
    /// List like object of Editops describing how to turn s1 into s2.
    /// </summary>
    public class Editops
    {
        internal int _src_len { get; set; }
        internal int _dest_len { get; set; }
        internal List<Editop> _editops;

        public int src_len => this._src_len;
        public int dest_len => this._dest_len;

        public List<Editop> Items => this._editops;
        public int Count => this._editops?.Count ?? 0;

        public Editops(List<Editop> editops = null, int src_len = 0, int dest_len = 0)
        {
            this._src_len = src_len;
            this._dest_len = dest_len;
            this._editops = _list_to_editops(editops, src_len, dest_len);
        }

        private static List<Editop> _list_to_editops(List<Editop> ops, int src_len, int dest_len)
        {
            if (ops is null || ops.Count == 0)
            {
                return [];
            }

            if (ops[0].len == 5)
            {
                return new Opcodes(ops.Select(item => new Opcode(item.tag, item.src_pos, item.dest_pos, 0, 0)).ToList(), src_len, dest_len).as_editops()._editops;
            }

            var blocks = new List<Editop>();

            foreach (var op in ops)
            {
                string edit_type = op.tag;
                var src_pos = op.src_pos;
                var dest_pos = op.dest_pos;

                if (src_pos > src_len || dest_pos > dest_len)
                {
                    var msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                if (src_pos == src_len && edit_type != "insert")
                {
                    var msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                if (dest_pos == dest_len && edit_type != "delete")
                {
                    var msg = "List of edit operations invalid";
                    throw new ValueError(msg);
                }

                // keep operations are not relevant in editops
                if (edit_type == "equal")
                {
                    continue;
                }

                blocks.Add(new Editop(edit_type, src_pos, dest_pos));
            }

            // validate order of editops
            foreach (var i in RangeHelper.GetRange(blocks.Count - 1))
            {
                if (blocks[i + 1].src_pos < blocks[i].src_pos || blocks[i + 1].dest_pos < blocks[i].dest_pos)
                {
                    var msg = "List of edit operations out of order";
                    throw new ValueError(msg);
                }

                if (blocks[i + 1].src_pos == blocks[i].src_pos && blocks[i + 1].dest_pos == blocks[i].dest_pos)
                {
                    var msg = "Duplicated edit operation";
                    throw new ValueError(msg);
                }
            }

            return blocks;
        }

        public Opcodes as_opcodes()
        {
            var blocks = new List<Opcode>();
            var src_pos = 0;
            var dest_pos = 0;
            var i = 0;

            while (i < this._editops.Count)
            {
                if (src_pos < this._editops[i].src_pos || dest_pos < this._editops[i].dest_pos)
                {
                    blocks.Add(new Opcode("equal", src_pos, this._editops[i].src_pos, dest_pos, this._editops[i].dest_pos));

                    src_pos = this._editops[i].src_pos;
                    dest_pos = this._editops[i].dest_pos;
                }

                var src_begin = src_pos;
                var dest_begin = dest_pos;
                var tag = this._editops[i].tag;

                while((i < this._editops.Count)&& this._editops[i].tag == tag && src_pos == this._editops[i].src_pos && dest_pos == this._editops[i].dest_pos)
                {
                    if(tag == "replace")
                    {
                        src_pos += 1;
                        dest_pos += 1;
                    }
                    else if(tag == "insert")
                    {
                        dest_pos += 1;
                    }
                    else if(tag == "delete")
                    {
                        src_pos += 1;
                    }

                    i += 1;
                }

                blocks.Add(new Opcode(tag, src_begin, src_pos, dest_begin, dest_pos));
            }

            if(src_pos < this.src_len || dest_pos<this.dest_len)
            {
                blocks.Add(new Opcode("equal", src_pos, this.src_len, dest_pos, this.dest_len));
            }

            var x = new Opcodes(blocks, this._src_len, this._dest_len);

            return x;
        }
    }
}
