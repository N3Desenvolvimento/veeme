using Veeme.Components;
using Veeme.Contracts;
using Veeme.Data;
using Veeme.Models;
using Veeme.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder
    .Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.Configure<FirebirdDatabaseOptions>(
    builder.Configuration.GetSection(FirebirdDatabaseOptions.SectionName)
);

builder.Services.AddScoped<IFirebirdConnectionFactory, FirebirdConnectionFactory>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();

var app = builder.Build();

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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet(
    "/api/produtos",
    async (
        string? nome,
        string? codigoBarras,
        IProdutoService produtoService,
        CancellationToken cancellationToken
    ) =>
    {
        var produtos = await produtoService.SearchAsync(nome, codigoBarras, cancellationToken);
        return Results.Ok(produtos);
    }
);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Veeme.Client._Imports).Assembly);

app.Run();
