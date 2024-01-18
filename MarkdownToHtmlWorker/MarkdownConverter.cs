using Markdig;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using System.IO.Hashing;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace MarkdownToHtmlWorker;

public class MarkdownConverter
    : ICommandHandler
{
    private readonly MarkdownPipeline _pipeline;
    private readonly ILogger<MarkdownConverter> _logger;

    public MarkdownConverter(MarkdownPipeline pipeline, ILogger<MarkdownConverter> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public string? Dir { get; set; }
    public bool Recursive { get; set; }

    private void ConvertFile(FileInfo file)
    {
        _logger.LogDebug("Input Directory: {fileDir}", file.DirectoryName);
        _logger.LogDebug("Input file: {fileFullName}", file.FullName);

        string crc32 = GetCrc32(file);
        string extension = file.Extension.Replace(".", "");

        string convertedFullFileName = Regex.Replace(file.FullName, $@".{extension}$", $@".{crc32}.html");

        if (File.Exists(convertedFullFileName))
        {
            _logger.LogDebug("File already exists: {convertedFullFileName}", convertedFullFileName);
            return;
        }

        foreach (string oldHtmlFile in Directory.EnumerateFiles(file.Directory!.FullName, $"{Path.GetFileNameWithoutExtension(file.Name)}.????????.html"))
        {
            _logger.LogInformation("Deleting {file}", oldHtmlFile);
            File.Delete(oldHtmlFile);
        }

        if (!string.Equals(extension, "md", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"File extension is not supported: {file.FullName} - must be .md or .markdown");
        }

        _logger.LogInformation("Converting {old} to {new}", file.FullName, convertedFullFileName);
        try
        {
            string html = Markdown.ToHtml(File.ReadAllText(file.FullName), _pipeline);

            html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="ie=edge">
    <title>{{Path.GetFileNameWithoutExtension(file.Name)}}</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-EVSTQN3/azprG1Anm3QDgpJLIm9Nao0Yz1ztcQTwFspd3yD65VohhpuuCOmLASjC" crossorigin="anonymous">
    <style>
        body { margin: 40px 0 0 40px; }
        table thead tr { background-color: #777; color: #fff; }
        table tr:nth-child(even) { background-color: #eee; }
        table td, table th { padding: 5px 10px; border: 1px solid #777; }
    </style>
</head>
<body>
    {{html}}
</body>
</html>
""";

            File.WriteAllText(convertedFullFileName, html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting {file}", file.FullName);
        }
    }

    private static string GetCrc32(FileInfo file)
    {
        Crc32 hashAlgo = new();
        using FileStream stream = file.OpenRead();
        hashAlgo.Append(stream);
        byte[] hash = hashAlgo.GetHashAndReset();
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public int Invoke(InvocationContext context)
    {
        _logger.LogDebug("In Invoke");

        _logger.LogDebug("Dir={dir}", Dir);
        _logger.LogDebug("Recursive={recursive}", Recursive);

        if (string.IsNullOrWhiteSpace(Dir))
        {
            throw new ArgumentException("Directory is required");
        }

        if (!Directory.Exists(Dir))
        {
            throw new ArgumentException($"Directory does not exist: {Dir}");
        }

        foreach (FileInfo file in EnumerateFiles(new(Dir), ["*.md", "*.markdown"], Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            _logger.LogDebug("Calling ConvertFile({file})", file.FullName);
            ConvertFile(file);
        }

        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        _logger.LogDebug("In InvokeAsync, calling Invoke(context)");

        return Task.FromResult(Invoke(context));
    }

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo dir, IEnumerable<string> searchPatterns, SearchOption searchOption)
    {
        foreach (string searchPattern in searchPatterns)
        {
            foreach (FileInfo file in dir.EnumerateFiles(searchPattern, searchOption))
            {
                yield return file;
            }
        }
    }
}
