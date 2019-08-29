using SchemaGenerator.Core.Utilities;
using SchemaGenerator.Samples.Shape;
using SchemaGenerator.TypeScript;
using System;
using System.Reflection;

namespace SchemaGenerator.Samples.RulesBasedTypeScriptSchemaGenerator
{
    public class Program
    {
        public static void Main()
        {
            var schemaGenerator =
                new TypeScriptSchemaGenerator(
                    new[] { typeof(Polygon) },
                    assemblyName =>
                        assemblyName.Name.StartsWith($"{nameof(SchemaGenerator)}.{nameof(Samples)}"),
                    memberInfo =>
                    {
                        switch (memberInfo)
                        {
                            case FieldInfo fieldInfo:
                                return fieldInfo.IsPublic;
                            case PropertyInfo propertyInfo:
                                return propertyInfo.GetMethod != null && propertyInfo.SetMethod != null;
                            default:
                                throw new UnexpectedException(nameof(MemberInfo), memberInfo.GetType().Name);
                        }
                    });
            schemaGenerator.Validate();
            var schema = schemaGenerator.Generate();

            Console.WriteLine(schema);
        }
    }
}
