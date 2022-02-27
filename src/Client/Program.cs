using AtcDemo.Client;
using AtcDemo.Client.Data;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.RootComponents.Add<App>("#app");
builder.RootComponents.RegisterAsCustomElement<App>("blazor-app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var backendOrigin = builder.Configuration["BackendOrigin"]!;

builder.Services
    .AddScoped(sp =>
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("AtcDemo.ServerAPI"))
    .AddHttpClient(
    "AtcDemo.ServerAPI",
        client => client.BaseAddress = new Uri(backendOrigin));


// gRPC-Web client with(out) auth
builder.Services.AddAtcRecordClient((services, options) =>
{
    //var handler = services.GetRequiredService<HttpClientHandler>();
    //handler.ConfigureHandler(new[] { backendOrigin });
    //handler.InnerHandler = new HttpClientHandler();

    options.BaseUri = backendOrigin;
    options.MessageHandler = new HttpClientHandler();
});

builder.Services.AddAtcClientDbContext();

await builder.Build().RunAsync();
