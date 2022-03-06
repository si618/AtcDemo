namespace AtcDemo.Client.Data;

using AtcDemo.Shared;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.EntityFrameworkCore;

public static class AtcServices
{
    public static void AddAtcClassificationClient(
        this IServiceCollection serviceCollection,
        Action<IServiceProvider,
        AtcClientOptions> configure)
    {
        serviceCollection.AddScoped(services =>
        {
            var options = new AtcClientOptions();
            configure(services, options);
            var grpcHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, options.MessageHandler!);
            var httpClient = new HttpClient(grpcHandler);
            var channel = GrpcChannel.ForAddress(
                options.BaseUri!,
                new GrpcChannelOptions
                {
                    HttpClient = httpClient,
                    MaxReceiveMessageSize = null
                });
            return new AtcClassificationRpcService.AtcClassificationRpcServiceClient(channel);
        });
    }

    public static void AddAtcClientDbContext(this IServiceCollection serviceCollection)
    {
        var serviceCollection1 = serviceCollection.AddDbContextFactory<AtcClientDbContext>(
            options => options.UseSqlite($"Filename={DataSynchronizer.SqliteDbFilename}"));
        serviceCollection.AddScoped<DataSynchronizer>();
    }
}
