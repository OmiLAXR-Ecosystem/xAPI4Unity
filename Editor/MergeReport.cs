using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    [Serializable]
    public enum MergeResultType { Copy, Skip, Conflict }
    
    [Serializable]
    public class MergeResult
    {
        public string path;
        public MergeResultType type;
        public bool isResolved = false;
    }

    [Serializable]
    public class MergeReport
    {
        [SerializeField]
        public DateTime MergeTime = DateTime.Now;
        [SerializeField]
        public int conflicts = 0;
        [SerializeField]
        public int copied = 0;
        [SerializeField]
        public int skipped = 0;
        public MergeResult[] CopyResults => results.Where(r => r.type == MergeResultType.Copy).ToArray();
        public MergeResult[] ConflictResults => results.Where(r => r.type == MergeResultType.Conflict).ToArray();
        public MergeResult[] SkipResults => results.Where(r => r.type == MergeResultType.Skip).ToArray();
        [SerializeField]
        public List<MergeResult> results = new List<MergeResult>();

        public void AddResult(string path, MergeResultType type)
        {
            if (type == MergeResultType.Conflict)
                conflicts++;
            else if (type == MergeResultType.Copy)
                copied++;
            else if (type == MergeResultType.Skip)
                skipped++;
            results.Add(new MergeResult { path = path, type = type, isResolved = type == MergeResultType.Conflict });
        }
        public void AddCopy(string path)
            => AddResult(path, MergeResultType.Copy);
        public void AddSkip(string path)
            => AddResult(path, MergeResultType.Skip);
        public void AddConflict(string path)
            => AddResult(path, MergeResultType.Conflict);
    }

   
}