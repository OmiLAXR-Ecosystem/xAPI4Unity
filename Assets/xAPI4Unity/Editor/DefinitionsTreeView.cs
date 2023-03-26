using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EduXR.xAPI.Editor
{
    public class DefinitionsTreeView : EditorWindow
    {
        [SerializeField]
        protected VisualTreeAsset uxml;
        
        protected interface IDefinition
        {
        }
        
        protected interface IDefinitions
        {
            public IDefinitions[] subDefinitions { get; }
            public IDefinitions definitions { get; }
            public string name { get; }
        }
        
        protected class Definitions : IDefinitions
        {
            public IDefinitions[] subDefinitions { get; }
            public IDefinitions definitions { get; }
            public string name { get; private set; }

            public Definitions(string name, IDefinitions[] subDefinitions = null)
            {
                this.name = name;
                this.subDefinitions = subDefinitions ?? Array.Empty<IDefinitions>();
            }
        }
        
        protected class Activities : Definitions
        {
            public Activities(IDefinitions[] subDefinitions = null) : base("activities", subDefinitions)
            {
            }
        }
        
        protected class Extensions : Definitions
        {
            public Extensions(IDefinitions[] subDefinitions = null) : base("extensions", subDefinitions)
            {
            }
        }
        
        protected class Verbs : Definitions
        {
            public Verbs(IDefinitions[] subDefinitions = null) : base("verbs", subDefinitions)
            {
            }
        }

        protected class Context : Definitions
        {
            public Verbs verbs { get; private set; }
            public Activities activities { get; private set; }
            public Extensions extensions { get; private set; }
            public Context(string name) : base(name, Array.Empty<IDefinitions>())
            {
                verbs = new Verbs();
                activities = new Activities();
                extensions = new Extensions();
            }
        }
    }
}