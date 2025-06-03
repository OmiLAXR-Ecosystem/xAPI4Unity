using System;
using System.Collections.Generic;

namespace xAPI4Unity.Editor
{
    [Serializable]
    public class MergeConflict
    {
        public string path;
        public string reason;
    }

    [Serializable]
    public class MergeConflictLog
    {
        public List<MergeConflict> conflicts;
    }

   
}