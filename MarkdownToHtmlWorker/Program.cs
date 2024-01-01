using Markdig;
using MarkdownToHtmlWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;

Parser runner = new CommandLineBuilder(new MarkdownConverterCommand())
    .UseHost(_ => Host.CreateDefaultBuilder(), 
        hb => hb.ConfigureServices((context, services) =>
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(context.Configuration)
                .CreateLogger();
            
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            services.AddSingleton<MarkdownConverter>();
            services.AddSingleton<MarkdownPipeline>(new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
        })
        .UseCommandHandler<MarkdownConverterCommand, MarkdownConverter>())
    .UseDefaults()
    .Build();

await runner.InvokeAsync(args);
