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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FirebirdDatabaseOptions>(
    builder.Configuration.GetSection(FirebirdDatabaseOptions.SectionName)
);

builder.Services.AddScoped<IFirebirdConnectionFactory, FirebirdConnectionFactory>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
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

if (app.Environment.IsDevelopment())
{
    app.MapGet(
        "/api/_schema",
        async (IFirebirdConnectionFactory connectionFactory, CancellationToken cancellationToken) =>
        {
            using var connection = connectionFactory.CreateConnection();
            if (connection is System.Data.Common.DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = """
            select
                trim(rf.rdb$relation_name) as relation_name,
                trim(rf.rdb$field_name) as field_name,
                rf.rdb$field_position as field_position
            from rdb$relation_fields rf
            join rdb$relations r on r.rdb$relation_name = rf.rdb$relation_name
            where coalesce(r.rdb$system_flag, 0) = 0
              and r.rdb$view_blr is null
            order by 1, 3
            """;

            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (command is System.Data.Common.DbCommand dbCommand)
            {
                using var reader = await dbCommand.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var tableName = reader.GetString(0);
                    var fieldName = reader.GetString(1);
                    if (!result.TryGetValue(tableName, out var fields))
                    {
                        fields = new List<string>();
                        result[tableName] = fields;
                    }
                    fields.Add(fieldName);
                }
            }
            else
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var tableName = reader.GetString(0);
                    var fieldName = reader.GetString(1);
                    if (!result.TryGetValue(tableName, out var fields))
                    {
                        fields = new List<string>();
                        result[tableName] = fields;
                    }
                    fields.Add(fieldName);
                }
            }

            return Results.Ok(result);
        }
    );

    app.MapGet(
        "/api/_ajustes/tipos",
        async (IFirebirdConnectionFactory connectionFactory, CancellationToken cancellationToken) =>
        {
            using var connection = connectionFactory.CreateConnection();
            if (connection is System.Data.Common.DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = "select distinct TIPO from AJUSTES_ESTOQUE";

            var tipos = new List<string?>();

            if (command is System.Data.Common.DbCommand dbCommand)
            {
                using var reader = await dbCommand.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    tipos.Add(reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString());
                }
            }
            else
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    tipos.Add(reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString());
                }
            }

            return Results.Ok(tipos);
        }
    );
}

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
