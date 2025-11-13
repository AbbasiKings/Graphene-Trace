using GrapheneTrace.Client;
using GrapheneTrace.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorBootstrap();

builder.Services.AddSingleton<AppToastService>();
builder.Services.AddSingleton<BlazorBootstrap.ToastService>();
builder.Services.AddSingleton<AuthService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();

