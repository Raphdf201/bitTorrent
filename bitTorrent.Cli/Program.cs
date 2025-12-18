// https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial
// https://www.ottorinobruni.com/building-powerful-console-app-in-csharp-with-dotnet-and-system-commandline/

using System.CommandLine;

var rootCommand = new RootCommand("Test");

var fileOption = new Option<FileInfo>("--file");

rootCommand.Add(fileOption);

var result = rootCommand.Parse(args);
if (result.Errors.Count == 0 && result.GetValue(fileOption) is { } file)
{
    // do smth with file.FullName
    return 0;
}
foreach (var error in result.Errors)
{
    Console.Error.WriteLine(error.Message);
}
return 1;
