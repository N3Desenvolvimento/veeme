using System.Data.Common;
using Dapper;
using Veeme.Contracts;

namespace Veeme.Services;

public sealed class ProdutoService : IProdutoService
{
    private readonly IFirebirdConnectionFactory _connectionFactory;

    public ProdutoService(IFirebirdConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> SearchAsync(
        string? nome,
        string? codigoBarras,
        CancellationToken cancellationToken
    )
    {
        nome = string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();
        codigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras.Trim();

        using var connection = _connectionFactory.CreateConnection();

        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }
        else
        {
            connection.Open();
        }

        var filters = new List<string>();
        var parameters = new DynamicParameters();

        if (nome is not null)
        {
            filters.Add("upper(DESCRICAO) like upper(@nomePattern)");
            parameters.Add("nomePattern", $"%{nome}%");
        }

        if (codigoBarras is not null)
        {
            filters.Add("CODIGO_BARRA = @codigoBarras");
            parameters.Add("codigoBarras", codigoBarras);
        }

        var sql = "select first 100 * from PRODUTOS";
        if (filters.Count > 0)
        {
            sql += $" where ({string.Join(" or ", filters)})";
        }

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync(command);
        return MapToDictionaries(rows);
    }

    private static IReadOnlyList<Dictionary<string, object?>> MapToDictionaries(
        IEnumerable<dynamic> rows
    )
    {
        var result = new List<Dictionary<string, object?>>();

        foreach (var row in rows)
        {
            if (row is IDictionary<string, object> rowDictionary)
            {
                var mapped = new Dictionary<string, object?>(
                    rowDictionary.Count,
                    StringComparer.OrdinalIgnoreCase
                );
                foreach (var pair in rowDictionary)
                {
                    mapped[pair.Key] = pair.Value;
                }
                result.Add(mapped);
                continue;
            }

            result.Add(new Dictionary<string, object?> { ["value"] = row });
        }

        return result;
    }
}
