using Common.Extensions;
using Common.Utilities;
using Shape;
using System;
using System.IO;
using Utilities;

namespace PolygonSchemaGenerator
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
                        new[] { typeof(Polygon) },
                        _ => _.Name.StartsWith(nameof(Shape)),
                        _ => _.HasAttribute<SerializeAttribute>());
                schemaGenerator.Validate();
                var schema = schemaGenerator.Generate();

                File.WriteAllText(
                    Path.Combine(commandLineArguments[0], "PolygonSchema.json"),
                    schema);
            }
            catch (Exception exception)
            {
                // This format is for the Error List view at Visual Studio
                Console.WriteLine(
                    $"Error {nameof(PolygonSchemaGenerator)}: " +
                    $"{exception.ToString().Replace(Environment.NewLine, " | ")}");
                throw;
            }
        }
    }
}
