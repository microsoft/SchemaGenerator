using FluentAssertions;
using JetBrains.Annotations;
using SchemaGenerator.Core.Utilities;
using SchemaGenerator.TypeScript;
using System.Reflection;
using Xunit;

namespace SchemaGenerator.Tests
{
    public class TypeScriptTest
    {
        [Fact]
        public void BasicTest()
        {
            const string expectedSchema = @"declare namespace SchemaGenerator.Tests {
    interface TypeScriptTestA {
        A: number;
    }
}
declare namespace Schema {
    export import TypeScriptTestA = SchemaGenerator.Tests.TypeScriptTestA;
}";

            var schemaGenerator =
                new TypeScriptSchemaGenerator(
                    new[] {typeof(TypeScriptTestA)},
                    assemblyName => assemblyName.Name == $"{nameof(SchemaGenerator)}.{nameof(Tests)}",
                    memberInfo =>
                    {
                        switch (memberInfo)
                        {
                            case FieldInfo fieldInfo:
                                return fieldInfo.IsPublic;
                            case PropertyInfo propertyInfo:
                                return propertyInfo.GetMethod?.IsPublic == true && propertyInfo.SetMethod != null;
                            default:
                                throw new UnexpectedException(nameof(MemberInfo), memberInfo.GetType().Name);
                        }
                    });

            var schema = schemaGenerator.Generate();

            schema.Should().Be(expectedSchema);
        }
    }

    public class TypeScriptTestA
    {
        [UsedImplicitly] public int A { get; set; }
    }
}