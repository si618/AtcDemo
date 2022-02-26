using AtcDemo.Client;
using AtcDemo.Client.Data;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var backendOrigin = builder.Configuration["BackendOrigin"]!;

builder.Services
    .AddScoped(sp => new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

// gRPC-Web client with(out) auth
builder.Services.AddAtcRecordClient((services, options) =>
{
    //var authEnabledHandler = services.GetRequiredService<AuthorizationMessageHandler>();
    //authEnabledHandler.ConfigureHandler(new[] { backendOrigin });
    //authEnabledHandler.InnerHandler = new HttpClientHandler();

    options.BaseUri = backendOrigin;
    //options.MessageHandler = authEnabledHandler;
});

builder.Services.AddAtcClientDbContext();

await builder.Build().RunAsync();
