using SchemaGenerator.Common;
using SchemaGenerator.Json;
using SchemaGenerator.Samples.Shape;
using System;
using System.IO;

namespace SchemaGenerator.Samples.BuildTimeAttributeBasedJsonSchemaGenerator
{
    /// <summary>
    /// This program generates a schema at build time.
    /// See .csproj for the actual running of the program.
    /// </summary>
    /// <remarks>Tip: Unload this project while making changes to the SchemaGenerator to enable debugging</remarks>
    public class Program
    {
        public static void Main(string[] commandLineArguments)
        {
            Ensure.NotNullOrEmpty(nameof(commandLineArguments), commandLineArguments);

            try
            {
                Directory.CreateDirectory(commandLineArguments[0]);

                var schemaGenerator =
                    new JsonSchemaGenerator(
                        new[] { typeof(Shape.Shape) },
                        _ => _.Name.StartsWith("SchemaGenerator.Samples"),
                        _ => _.HasAttribute<SerializeAttribute>());
                schemaGenerator.Validate();
                var schema = schemaGenerator.Generate();

                File.WriteAllText(
                    Path.Combine(commandLineArguments[0], "schema.json"),
                    schema);
            }
            catch (Exception exception)
            {
                // This format is for the Error List view at Visual Studio
                Console.WriteLine(
                    $"Error {nameof(BuildTimeAttributeBasedJsonSchemaGenerator)}: " +
                    $"{exception.ToString().Replace(Environment.NewLine, " | ")}");
                throw;
            }
        }
    }
}
