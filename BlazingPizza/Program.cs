global using BlazingPizza.Shared;
global using BlazingPizza;
using BlazingPizza.Client;
using BlazingPizza.Components;
using BlazingPizza.Components.Account;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BlazingPizza.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();

// Add Security
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<PizzaStoreContext>();
    })
    .AddClient(options =>
    {
        options.AllowAuthorizationCodeFlow();
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();
        options.UseAspNetCore()
               .EnableRedirectionEndpointPassthrough()
               .DisableTransportSecurityRequirement(); // for Development only

        options.UseSystemNetHttp();
        options.UseWebProviders()
               .AddGitHub(options =>
               {
                   options.SetClientId("Open your GitHub Settings to get one")
                          .SetClientSecret("Open your GitHub Settings to get one")
                          .SetRedirectUri("http://localhost:5141/callback/login/github");
               });
    });

// adding Postgres instead of sqlite
builder.Services.AddDbContext<PizzaStoreContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseOpenIddict());
// Configure Redis
var multiplexer = ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]);
builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

// Add Identity
builder.Services.AddIdentityCore<PizzaStoreUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<PizzaStoreContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Configure Email Sender
builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient(typeof(IEmailSender<>), typeof(EmailSender<>));

builder.Services.AddScoped<IRepository, EfRepository>();
builder.Services.AddScoped<OrderState>();
builder.Services.AddScoped<IOrderService, OrderService>(); // added for educational purposes to understand Client-Server communication in Blazor

builder.Services.AddControllers();

var app = builder.Build();

// Initialize the database
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = scopeFactory.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PizzaStoreContext>();
    if (db.Database.EnsureCreated())
    {
        SeedData.Initialize(db);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapPizzaApi();

app.MapControllers();

app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(
            typeof(BlazingPizza.Client._Imports).Assembly,
            typeof(BlazingPizza.ComponentsLibrary._Imports).Assembly
        );

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();