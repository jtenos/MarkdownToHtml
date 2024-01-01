using System.CommandLine;

namespace MarkdownToHtmlWorker;

public class MarkdownConverterCommand
    : RootCommand
{
    private static readonly string[] _recursiveArgNames = ["--recursive", "-r"];

    public MarkdownConverterCommand()
        : base("Converts markdown to HTML")
    {
        AddArgument(new Argument<string>("dir", "The directory to convert markdown files in"));
        AddOption(new Option<bool>(_recursiveArgNames, () => true, "Whether to recursively convert markdown files in subdirectories"));
    }
}
