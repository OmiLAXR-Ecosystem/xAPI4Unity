/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using xAPI4Unity.Editor.Types;

namespace xAPI4Unity.Tests
{
    [TestFixture]
    public class TypeSyntax_Tests
    {
        private static readonly TypeNameResolver Resolver = new TypeNameResolver(new Aliases());

        /// <summary>
        /// Resolves only the names it was given, and records every name it was asked for.
        /// </summary>
        private sealed class StubResolver : ITypeNameResolver
        {
            private readonly Dictionary<string, Type> _types;
            public readonly List<string> Requested = new List<string>();

            public StubResolver(Dictionary<string, Type> types) => _types = types;

            public ResolvedType ResolveType(string name)
            {
                Requested.Add(name);
                return _types.TryGetValue(name, out var t) ? new ResolvedType(t, name) : null;
            }
        }

        private static string Syntax(string expression) =>
            TypeExpression.Parse(expression).Resolve(Resolver).GetTypeSyntax();

        [TestCase("(int,float)[]", "(System.Int32,System.Single)[]")]
        [TestCase("(int,(float,float))?", "(System.Int32,(System.Single,System.Single))?")]
        [TestCase("int?[]", "System.Int32?[]")]
        [TestCase("float[,]", "System.Single[,]")]
        [TestCase("int[][,,]", "System.Int32[][,,]")]
        [TestCase("vec3[]", "UnityEngine.Vector3[]")]
        public void TypeExpression_Builds_Compilable_Syntax(string expression, string expected)
        {
            // Act
            var syntax = Syntax(expression);

            // Assert
            Assert.AreEqual(expected, syntax);
        }

        [Test]
        public void TypeExpression_Array_Of_Tuples_Has_A_ValueTuple_Array_Type()
        {
            // Act
            var resolved = TypeExpression.Parse("(int,float)[]").Resolve(Resolver);

            // Assert
            Assert.That(resolved, Is.InstanceOf<ResolvedArray>());
            Assert.AreEqual(typeof(ValueTuple<int, float>[]), resolved.Type);
        }

        [Test]
        public void TypeExpressionSet_Keeps_Two_Different_Tuples()
        {
            // Arrange
            var set = TypeExpressionSet.Parse("(int,int) | (string,string)");

            // Act
            var syntaxes = set.ResolveAll(Resolver).Select(t => t.GetTypeSyntax()).ToArray();

            // Assert
            CollectionAssert.AreEquivalent(
                new[] { "(System.Int32,System.Int32)", "(System.String,System.String)" }, syntaxes);
        }

        [TestCase("nope?")]
        [TestCase("nope[1,2]")]
        [TestCase("(int,nope)")]
        public void TypeNameResolver_Returns_Null_For_Unknown_Inner_Types(string name)
        {
            // Act
            var resolved = Resolver.ResolveType(name);

            // Assert
            Assert.IsNull(resolved);
        }

        [Test]
        public void TypeExpression_Returns_Null_For_A_Range_Of_An_Unknown_Type()
        {
            // Act
            var resolved = TypeExpression.Parse("nope[1,10]").Resolve(Resolver);

            // Assert
            Assert.IsNull(resolved);
        }

        [TestCase("Vector3", typeof(Vector3))]
        [TestCase("STRING", typeof(string))]
        [TestCase("Int", typeof(int))]
        public void Aliases_Ignore_Case(string name, Type expected)
        {
            // Act
            var resolved = Resolver.ResolveType(name);

            // Assert
            Assert.IsNotNull(resolved);
            Assert.AreEqual(expected, resolved.Type);
        }

        [Test]
        public void TypeExpression_Asks_The_Injected_Resolver_For_Each_Tuple_Element()
        {
            // Arrange
            var stub = new StubResolver(new Dictionary<string, Type> { { "id", typeof(Guid) }, { "n", typeof(int) } });

            // Act
            var resolved = TypeExpression.Parse("(id, n)").Resolve(stub);

            // Assert
            CollectionAssert.AreEqual(new[] { "id", "n" }, stub.Requested);
            Assert.AreEqual("(System.Guid,System.Int32)", resolved.GetTypeSyntax());
        }

        [Test]
        public void TypeSchema_Uses_The_Builtin_Aliases_Without_A_Resolver()
        {
            // Arrange
            var input = new Dictionary<string, string> { { "ctx/extensions/result/pos", "vec3" } };

            // Act
            var schema = TypeSchema.Parse(input);
            var types = schema.GetTypesFromKey("ctx/extensions/result/pos");

            // Assert
            Assert.AreEqual(typeof(Vector3), types.Single().Type);
        }
    }
}
#endif
