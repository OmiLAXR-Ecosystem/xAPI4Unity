using System;

namespace xAPI4Unity.Editor
{
    public class DefinitionsTree
    {
        protected abstract class TreeNode
        {
            public TreeNode[] children { get; }
            public string name { get; }
            public bool isLeaf => children.Length < 1;

            protected TreeNode(string name, TreeNode[] children)
            {
                this.children = children;
                this.name = name;
            }
        }

        protected abstract class File : TreeNode
        {
            public File(string name) : base(name, Array.Empty<TreeNode>()) {}
        }

        protected abstract class Folder : TreeNode
        {
            public Folder(string name, TreeNode[] children) : base(name, children) {}
        }

        protected class Definition : File
        {
            public Definition(string name) : base(name)
            {
            }
        }
        
        protected class Verb : Definition
        {
            public Verb(string name) : base(name)
            {
            }
        }
        protected class Activitiy : Definition
        {
            public Activitiy(string name) : base(name)
            {
            }
        }
        
        protected class Extensions : Folder
        {
            public Extensions(string name, TreeNode[] children) : base(name, children)
            {
            }
        }
        protected class ExtensionFolder : Folder
        {
            public ExtensionFolder(string name, Extension[] children) : base(name, children)
            {
            }
        }
        
        protected class Extension : Definition
        {
            public Extension(string name) : base(name)
            {
            }
        }
        
        protected class Verbs : Folder
        {
            public Verbs(Verb[] verbs) : base("verbs", verbs)
            {
            }
        }
        
        protected class Activities : Folder
        {
            public Activities(string name, Activitiy[] children) : base(name, children)
            {
            }
        }
        
        protected class Context : Folder
        {
            public Verbs verbs { get; private set; }
            public Activities activities { get; private set; }
            public Extensions extensions { get; private set; }
            public Context(string name) : base(name, Array.Empty<TreeNode>())
            {
                
            }
        }
    }
}