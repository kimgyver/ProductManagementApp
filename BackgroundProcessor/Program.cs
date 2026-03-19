using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Hosting;
using BackgroundProcessor.Services;
using Amazon.SimpleEmail;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
  var host = Host.CreateDefaultBuilder(args)
      .UseSerilog((context, services, configuration) =>
          configuration.ReadFrom.Configuration(context.Configuration))
      .ConfigureServices(services =>
      {
        services.AddHttpClient();
        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonSimpleEmailService>();
        services.AddHostedService<EmailBackgroundWorker>();
        services.AddHostedService<EmailFailureBackgroundWorker>();
      })
      .Build();

  await host.RunAsync();
}
catch (Exception ex)
{
  Log.Fatal(ex, "BackgroundProcessor host terminated unexpectedly");
}
finally
{
  Log.CloseAndFlush();
}