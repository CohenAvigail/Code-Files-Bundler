using System.CommandLine;


var languageOption = new Option<string[]>(
    ["--language", "-l"],
    description: "List of programming languages (e.g., C#, JavaScript). Use 'all' to include all files.");

var outputOption = new Option<FileInfo>(
    ["--output", "-o"],
    description: "Output file path and name, e.g., 'bundle.txt' or full path like 'C:/bundles/bundle.txt'");

var noteOption = new Option<bool>(
    ["--note", "-n"],
    description: "Include the source code file name and path as a comment in the bundle file.");

var sortOption = new Option<string>(
    ["--sort", "-s"],
    getDefaultValue: () => "name",
    description: "Sort the files either by 'name' (default) or 'type'.");

var removeEmptyLinesOption = new Option<bool>(
    ["--remove-empty-lines", "-rmv"],
    description: "Remove empty lines from the source code before bundling.");

var authorOption = new Option<string>(
    ["--author", "-a"],
    description: "Name of the author of the bundled code.");


var bundleCommand = new Command("bundle", "Bundle code files to a single file");
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((language, output, note, sort, removeEmptyLines, author) =>
{
    try
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        // Define the file extensions for each language
        var languageExtensions = language.Contains("all")
            ? [".c", ".cpp", ".cs", ".py", ".java", ".js", ".ts", ".jsx", ".css", ".html"]
            : language.Select(l => GetExtensionForLanguage(l)).ToArray();

        // Get all the code files based on the selected languages
        var sourceFiles = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories)
            .Where(file => languageExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Where(file => !file.Contains("bin") && !file.Contains("node_modules") && !file.Contains("obj") && !file.Contains("Properties"))
            .ToList();

        // Sort files based on user preference
        if (sort == "name")
        {
            sourceFiles = [.. sourceFiles.OrderBy(file => Path.GetFileName(file))];
        }
        else if (sort == "type")
        {
            sourceFiles = [.. sourceFiles.OrderBy(file => Path.GetExtension(file))];
        }

        // Create the output file
        using var outputStream = new StreamWriter(output.FullName);

        // If an author is provided, write the author to the top
        if (!string.IsNullOrEmpty(author))
        {
            outputStream.WriteLine($"// Author: {author}");
        }

        // Iterate over files to write their content to the output file
        foreach (var file in sourceFiles)
        {
            var fileContent = File.ReadAllText(file);

            // Remove empty lines if the option is set
            if (removeEmptyLines)
            {
                fileContent = string.Join(Environment.NewLine, fileContent.Split([ "\r\n", "\n" ], StringSplitOptions.None)
                    .Where(line => !string.IsNullOrWhiteSpace(line)));
            }

            // Add note with file name and relative path if requested
            if (note)
            {
                outputStream.WriteLine($"// Path: {Path.GetRelativePath(currentDirectory, file)}");
            }

            outputStream.WriteLine(fileContent);
            outputStream.WriteLine("\n");  // Add 2 blank line between files
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Code bundled successfully!");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error during bundling: {ex.Message}");
        Console.ResetColor();
    }
}, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

// `create-rsp` Command: Generate Response File with Parameters
var createRspCommand = new Command("create-rsp", "Create a response file with a ready-made command for bundling.");
createRspCommand.SetHandler(() =>
{
    // Ask the user for inputs
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.Write("Enter languages (comma separated), or 'all': ");
    Console.ResetColor();
    var selectedlanguages = Console.ReadLine();
    while (string.IsNullOrEmpty(selectedlanguages))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("language is required!");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("Enter languages (comma separated), or 'all': ");
        selectedlanguages = Console.ReadLine();
    };
    var languages = selectedlanguages.Split(',').Select(l => l.Trim()).ToArray();

    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.Write("Enter output file path: ");
    Console.ResetColor();
    var outputPath = Console.ReadLine();

    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.Write("Include source code note? (y/n): ");
    Console.ResetColor();
    var note = Console.ReadLine()?.ToLower() == "y";

    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.Write("Sort files by name or type? (default: name): ");
    Console.ResetColor();
    var userInput = Console.ReadLine();
    var sort = string.IsNullOrEmpty(userInput) ? "name" : userInput.ToLower();

    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.Write("Remove empty lines? (y/n): ");
    Console.ResetColor();
    var removeEmptyLines = Console.ReadLine()?.ToLower() == "y";

    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.Write("Enter author's name (optional): ");
    Console.ResetColor();
    var author = Console.ReadLine();

    // Construct the full command
    var command = $"bundle ";

    foreach (var language in languages)
    {
        command += $"-l {language} ";
    }

    command += $"--output \"{outputPath}\"";

    if (note) command += " --note";
    
    command += " --sort " + sort;
    
    if (removeEmptyLines) command += " --remove-empty-lines";
    
    if (!string.IsNullOrEmpty(author)) command += " --author \"" + author + "\"";

    // Save the command to a response file
    File.WriteAllText("bundle.rsp", command);
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("Response file created: bundle.rsp");
    Console.WriteLine("In order to complit bundling, run: fib @bundle.rsp ");
    Console.ResetColor();

});

var rootCommand = new RootCommand("Code Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);

// Helper Method to Get Extension for a Language
static string GetExtensionForLanguage(string language)
{
    return language.ToLower() switch
    {
        "c" => ".c",
        "cpp" => ".cpp",
        "c#" => ".cs",
        "python" => ".py",
        "java" => ".java",
        "js" => ".js",
        "javascript" => ".js",
        "typescript" => ".ts",
        "ts" => ".ts",
        "jsx" => ".jsx",
        "react" => ".jsx",
        "css" => ".css",
        "html" => ".html",
        _ => throw new ArgumentException($"Unsupported language: {language}")
    };
}
