using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xAPI4Unity.Editor.Parser.IO
{

    /// <summary>
    /// Class that represents a context of xAPI definitions
    /// </summary>
    public class Context
    {
        /// <summary>
        /// Name of the context
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Activity files in the context
        /// </summary>
        public List<FileMeta> Activities { get; } = new List<FileMeta>();

        /// <summary>
        /// Extension files in the context
        /// </summary>
        public IDictionary<string, List<FileMeta>> Extensions { get; } = new Dictionary<string, List<FileMeta>>();

        /// <summary>
        /// Verb files in the context
        /// </summary>
        public List<FileMeta> Verbs { get; } = new List<FileMeta>();

        /// <summary>
        /// Creates a Context
        /// </summary>
        /// <param name="name">Name of the context</param>
        public Context(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Adds an activity to the context
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <returns>True, if it was successfully added, false, if not</returns>
        public bool AddActivity(FileMeta activity)
        {
            var added = false;
            if (activity != null && (added = !Activities.Contains(activity)))
            {
                Activities.Add(activity);
            }

            return added;
        }

        /// <summary>
        /// Adds activities to the context
        /// </summary>
        /// <param name="activities">A list of activities</param>
        /// <returns>Number of successfully added activities</returns>
        public int AddActivities(IEnumerable<FileMeta> activities)
            => activities?.Count(AddActivity) ?? 0;

        /// <summary>
        /// Adds a verb to the context
        /// </summary>
        /// <param name="verb">Verb</param>
        /// <returns>True, if it was successfully added, false, if not</returns>
        public bool AddVerb(FileMeta verb)
        {
            var added = false;
            if (verb != null && (added = !Verbs.Contains(verb)))
            {
                Verbs.Add(verb);
            }

            return added;
        }

        /// <summary>
        /// Adds verbs to the context
        /// </summary>
        /// <param name="verbs">List of verbs</param>
        /// <returns>Number of successfully added verbs</returns>
        public int AddVerbs(List<FileMeta> verbs)
            => verbs?.Count(AddVerb) ?? 0;

        /// <summary>
        /// Adds an extension to the context
        /// </summary>
        /// <param name="extension">Extension</param>
        /// <returns>True, if it was successfully added, false, if not</returns>
        public bool AddExtension(string type, FileMeta extension)
        {
            var added = false;
            if (!Extensions.ContainsKey(type))
            {
                Extensions.Add(type, new List<FileMeta>());
            }

            var extensionsOfType = Extensions[type];
            if (extension != null && (added = !extensionsOfType.Contains(extension)))
            {
                Extensions[type].Add(extension);
            }

            return added;
        }

        /// <summary>
        /// Adds extensions to the context
        /// </summary>
        /// <param name="extensions">List of extension file objects</param>
        /// <returns>Number of successfully added extensions</returns>
        public int AddExtensions(IDictionary<string, List<FileMeta>> extensions)
        {
            var added = 0;
            if (extensions == null) return added;
            foreach (var extType in extensions)
            {
                var type = extType.Key;
                if (!Extensions.ContainsKey(type))
                {
                    Extensions.Add(type, new List<FileMeta>());
                }

                foreach (var extension in extensions[type].Where(extension => !Extensions[type].Contains(extension)))
                {
                    Extensions[type].Add(extension);
                    ++added;
                }
            }

            return added;
        }

        public override string ToString()
        {
            var extensions = new StringBuilder();
            foreach (var ext in Extensions)
            {
                extensions.Append($"key={ext.Key},value=[{StringExt.ToString(ext.Value.ToArray())}]");
            }

            return
                $"[Context name={Name}, Activities={StringExt.ToString(Activities.ToArray())}, Verbs={StringExt.ToString(Verbs.ToArray())}, Extensions=[{extensions}]]";
        }
    }
}