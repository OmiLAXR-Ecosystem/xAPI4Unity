/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Represents the result type of a file merge operation.
    /// </summary>
    [Serializable]
    public enum MergeResultType 
    { 
        /// <summary>
        /// File was successfully copied.
        /// </summary>
        Copy, 
        
        /// <summary>
        /// File was skipped because it already existed and matched.
        /// </summary>
        Skip, 
        
        /// <summary>
        /// File resulted in a conflict during the merge operation.
        /// </summary>
        Conflict,

        /// <summary>
        /// File was replaced with a newer release, because it was unchanged since the installed release.
        /// </summary>
        Update,

        /// <summary>
        /// File was deleted, because the newer release removed it and it was unchanged since the installed release.
        /// </summary>
        Remove
    }
    
    /// <summary>
    /// Represents a single merge result for a specific file, including its path and result type.
    /// </summary>
    [Serializable]
    public class MergeResult
    {
        /// <summary>
        /// Path of the file involved in the merge operation.
        /// </summary>
        public string path;

        /// <summary>
        /// The result type of the merge operation for this file.
        /// </summary>
        public MergeResultType type;
    }

    /// <summary>
    /// Represents the overall merge report, summarizing the results of a merge operation.
    /// </summary>
    [Serializable]
    public class MergeReport
    {
        /// <summary>
        /// Source directory of the merge operation.
        /// </summary>
        [SerializeField] public string source;

        /// <summary>
        /// The timestamp when the merge operation was performed.
        /// </summary>
        [SerializeField]
        public DateTime MergeTime = DateTime.Now;

        /// <summary>
        /// The total number of conflicts encountered during the merge operation.
        /// </summary>
        [SerializeField]
        public int conflicts = 0;

        /// <summary>
        /// The total number of files copied during the merge operation.
        /// </summary>
        [SerializeField]
        public int copied = 0;

        /// <summary>
        /// The total number of files skipped during the merge operation.
        /// </summary>
        [SerializeField]
        public int skipped = 0;

        /// <summary>
        /// The total number of files updated to a newer release.
        /// </summary>
        [SerializeField]
        public int updated = 0;

        /// <summary>
        /// The total number of files removed because a newer release removed them.
        /// </summary>
        [SerializeField]
        public int removed = 0;

        /// <summary>
        /// A collection of files successfully copied.
        /// </summary>
        public MergeResult[] CopyResults => results.Where(r => r.type == MergeResultType.Copy).ToArray();

        /// <summary>
        /// A collection of files that encountered conflicts.
        /// </summary>
        public MergeResult[] ConflictResults => results.Where(r => r.type == MergeResultType.Conflict).ToArray();

        /// <summary>
        /// A collection of files that were skipped.
        /// </summary>
        public MergeResult[] SkipResults => results.Where(r => r.type == MergeResultType.Skip).ToArray();

        /// <summary>
        /// A collection of files updated to a newer release.
        /// </summary>
        public MergeResult[] UpdateResults => results.Where(r => r.type == MergeResultType.Update).ToArray();

        /// <summary>
        /// A collection of files removed because a newer release removed them.
        /// </summary>
        public MergeResult[] RemoveResults => results.Where(r => r.type == MergeResultType.Remove).ToArray();

        /// <summary>
        /// The list of all merge results.
        /// </summary>
        [SerializeField]
        public List<MergeResult> results = new List<MergeResult>();
        
        /// <summary>
        /// Initializes a new <see cref="MergeReport"/> instance with the specified source directory.
        /// </summary>
        /// <param name="source">The source directory used in the merge operation.</param>
        public MergeReport(string source)
        {
            this.source = source;
        }
        
        /// <summary>
        /// Adds a result for a specific file to the merge report.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="type">The result type (Copy, Skip, Conflict, Update, Remove).</param>
        public void AddResult(string path, MergeResultType type)
        {
            if (type == MergeResultType.Conflict)
                conflicts++;
            else if (type == MergeResultType.Copy)
                copied++;
            else if (type == MergeResultType.Skip)
                skipped++;
            else if (type == MergeResultType.Update)
                updated++;
            else if (type == MergeResultType.Remove)
                removed++;
            
            results.Add(new MergeResult { path = path, type = type });
        }

        /// <summary>
        /// Adds a copy result to the merge report.
        /// </summary>
        /// <param name="path">The path of the file copied.</param>
        public void AddCopy(string path)
            => AddResult(path, MergeResultType.Copy);

        /// <summary>
        /// Adds a skip result to the merge report.
        /// </summary>
        /// <param name="path">The path of the file skipped.</param>
        public void AddSkip(string path)
            => AddResult(path, MergeResultType.Skip);

        /// <summary>
        /// Adds a conflict result to the merge report.
        /// </summary>
        /// <param name="path">The path of the file that caused a conflict.</param>
        public void AddConflict(string path)
            => AddResult(path, MergeResultType.Conflict);

        /// <summary>
        /// Adds an update result to the merge report.
        /// </summary>
        /// <param name="path">The path of the file updated to a newer release.</param>
        public void AddUpdate(string path)
            => AddResult(path, MergeResultType.Update);

        /// <summary>
        /// Adds a remove result to the merge report.
        /// </summary>
        /// <param name="path">The path of the file the newer release removed.</param>
        public void AddRemove(string path)
            => AddResult(path, MergeResultType.Remove);
    }
}