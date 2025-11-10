using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace OpenApiGenerator;

[Generator]
public class SourceGeneratorWithAdditionalFiles : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(m => m is not null)
            .Where(a => Path.GetFileName(a.Path).EndsWith(".json"))
            .Select((a, c) => (a.Path, a.GetText(c)!.ToString()));

        var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());
        
        context.RegisterSourceOutput(compilationAndFiles,
            (productionContext, sourceContext) => GenerateCode(productionContext, sourceContext.Left, sourceContext.Right));
    }

    private static async void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<(string, string)> files)
    {
        foreach (var file in files)
        {
            var text = file.Item2;
            // replaces all type null with nullable:true, needs more cleanup after
            text = Regex.Replace(text, """{"type":"null"}""", """{"nullable":true}""");
            // removes anyOf if only one type and nullable:true are inside.
            text = Regex.Replace(text, @"""anyOf"":\[{""type"":""([aA-zZ]+)""},{""nullable"":true}]", @"""type"":""$1"",""nullable"":true");
            // remove empty required arrays
            text = Regex.Replace(text, @",\""required\"":\[]", "");
            // change number string to number value
            text = Regex.Replace(text, @"""(min|max)Items"":""([0-9]+)""", @"""$1Items"":$2");
            // patching in the response content for get-icon
            text = Regex.Replace(text, @"(v1\/utils\/get-icon"":{.*?,""responses"":{)}", 
                @"$1""200"":{""content"":{""image/png"":{""schema"":{""type"":""string"",""format"":""binary""}}},""description"":""""}}");
            // force runner to be docker runner only, multiple different objects aren't possible  
            text = Regex.Replace(text, @"(""runner"":{""anyOf"":\[.*?])}", @"""runner"":{""$ref"":""#/components/schemas/wolf__config__AppDocker__tagged""}");
            // removes the enum from type, since only docker is used here.
            text = Regex.Replace(text, @"""type"":{""type"":""string"",""enum"":\[.*?]}", @"""type"":{""type"":""string""}");
            // Make PartialClientSettings correctly optional
            text = Regex.Replace(text, @"""(client_settings|settings)"":{""description"":"".*?"",""anyOf"":\[{""\$ref"":""(.*?)""},{""nullable"":true}]}", @"""$1"":{""nullable"":true,""allOf"":[{""$ref"":""$2""}]}");
            // fixes optional Pin fields, since they shouldn't be "anyOf"
            text = Regex.Replace(text, @"""anyOf"":\[{""type"":""array"",""items"":{""type"":""integer""}},{""nullable"":true}]", @"""type"":""array"",""items"":{""type"":""integer""},""nullable"":true");
            // sanitize reflection type names.
            text = Regex.Replace(text, @"rfl__Reflector_wolf__core__events__(.*?)___ReflType", @"$1");
            // sanitize endpoint return type names
            text = Regex.Replace(text, @"wolf__api__(.*?)""", @"$1""");
            // sanitize events type names
            text = Regex.Replace(text, @"wolf__core__events__(.*?)""", @"$1""");

            try
            {
                var document = await OpenApiDocument.FromJsonAsync(text);

                var settings = new CSharpClientGeneratorSettings
                {
                    ClassName = "NSwag" + Path.GetFileNameWithoutExtension(file.Item1), 
                    CSharpGeneratorSettings = 
                    {
                        Namespace = "NSwag" + Path.GetFileNameWithoutExtension(file.Item1)
                    }
                };
            
                var generator = new CSharpClientGenerator(document, settings);	
                var code = generator.GenerateFile();
                context.AddSource($"{"NSwag" + Path.GetFileNameWithoutExtension(file.Item1)}.g.cs", code);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}