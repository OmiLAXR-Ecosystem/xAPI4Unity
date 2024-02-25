#if UNITY_EDITOR
using System.Collections.Generic;

namespace xAPI4Unity.Editor.Parser.Code.SyntaxTree {
    /// <summary>
    /// A class in the syntax tree
    /// </summary>
    internal sealed class Class : SyntaxNode<Class> {
        private List<Property> properties = new List<Property>();
        private List<Constructor> constructors = new List<Constructor>();
        private List<GetableProperty> getableProperties = new List<GetableProperty>();
        private List<Method> methods = new List<Method>();
        
        /// <summary>
        /// Base class of the class
        /// </summary>
        public string BaseClass { get; private set; }

        /// <summary>
        /// Properties of the class
        /// </summary>
        public IReadOnlyList<Property> Properties => properties.AsReadOnly();
        /// <summary>
        /// Constructors of the class
        /// </summary>
        public IReadOnlyList<Constructor> Constructors => constructors.AsReadOnly();
        /// <summary>
        /// Properties of the class that have a getter
        /// </summary>
        public IReadOnlyList<GetableProperty> GetableProperties => getableProperties.AsReadOnly();
        /// <summary>
        /// Methods of the class
        /// </summary>
        public IReadOnlyList<Method> Methods => methods.AsReadOnly();

        public Class() : base() { }

        public Class(string name, string baseClass = default, string comment = default) : base(name, comment) {
            BaseClass = baseClass;
        }

        public Class WithBaseClass(string baseClass) {
            BaseClass = baseClass;
            return this;
        }

        public Class AddProperty(Property property) {
            properties.Add(property);
            return this;
        }

        public Class AddProperties(params Property[] properties) {
            this.properties.AddRange(properties);
            return this;
        }

        public Class AddProperties(IEnumerable<Property> properties) {
            this.properties.AddRange(properties);
            return this;
        }

        public Class AddConstructor(Constructor constructor) {
            constructors.Add(constructor);
            return this;
        }

        public Class AddConstructors(params Constructor[] constructors) {
            this.constructors.AddRange(constructors);
            return this;
        }

        public Class AddConstructors(IEnumerable<Constructor> constructors) {
            this.constructors.AddRange(constructors);
            return this;
        }

        public Class AddGetableProperty(GetableProperty getableProperty) {
            getableProperties.Add(getableProperty);
            return this;
        }

        public Class AddGetableProperties(params GetableProperty[] getableProperties) {
            this.getableProperties.AddRange(getableProperties);
            return this;
        }

        public Class AddGetableProperties(IEnumerable<GetableProperty> getableProperties) {
            this.getableProperties.AddRange(getableProperties);
            return this;
        }

        public Class AddMethod(Method method) {
            methods.Add(method);
            return this;
        }

        public Class AddMethods(params Method[] methods) {
            this.methods.AddRange(methods);
            return this;
        }

        public Class AddMethods(IEnumerable<Method> methods) {
            this.methods.AddRange(methods);
            return this;
        }
    }
}
#endif