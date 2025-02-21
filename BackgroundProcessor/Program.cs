using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using BackgroundProcessor.Services;
using Amazon.SimpleEmail;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
      // Add HttpClient
      services.AddHttpClient();

      // Register AWS SQS
      services.AddAWSService<IAmazonSQS>();
      services.AddAWSService<IAmazonSimpleEmailService>();

      // Register both workers
      services.AddHostedService<EmailBackgroundWorker>();
      services.AddHostedService<EmailFailureBackgroundWorker>();
    })
    .Build();

await host.RunAsync();