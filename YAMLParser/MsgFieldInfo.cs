using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FauxMessages
{
    public class MsgFieldInfo
    {
        public string ConstVal;
        public bool IsArray;
        public bool IsConst;
        public bool IsLiteral;
        public bool IsMetaType;
        public int Length = -1;
        public string Name;
        public string Type;

#if !TRACE
        [DebuggerStepThrough]
#endif
        public MsgFieldInfo(string name, bool isliteral, string type, bool isconst, string constval, bool isarray,
            string lengths, bool meta)
        {
            Name = name;
            IsArray = isarray;
            Type = type;
            IsLiteral = isliteral;
            IsMetaType = meta;
            IsConst = isconst;
            ConstVal = constval;
            if (lengths == null) return;
            if (lengths.Length > 0)
            {
                Length = int.Parse(lengths);
            }
        }
    }
}
