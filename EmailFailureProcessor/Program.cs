using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
      // Add HttpClient
      services.AddHttpClient();

      // Register AWS SQS
      services.AddAWSService<IAmazonSQS>();

      // Register your worker service
      services.AddHostedService<EmailFailureProcessor>();
    })
    .Build();

await host.RunAsync();