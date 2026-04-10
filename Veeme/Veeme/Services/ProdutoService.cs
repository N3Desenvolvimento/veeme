using System.Data.Common;
using Dapper;
using Veeme.Contracts;
using Veeme.Utils;

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
        return DbRowMapper.MapToDictionaries(rows);
    }

    public async Task<Dictionary<string, object?>?> GetDetalheAsync(
        int codigoProduto,
        CancellationToken cancellationToken
    )
    {
        if (codigoProduto <= 0)
        {
            return null;
        }

        using var connection = _connectionFactory.CreateConnection();

        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }
        else
        {
            connection.Open();
        }

        const string sql = """
            select
                p.CODIGO_PRODUTO,
                p.CODIGO_BARRA,
                p.DESCRICAO,
                p.UNIDADE,
                p.LOCALIZACAO,
                p.REFERENCIA,
                p.ESTOQUE,
                p.PRECO_VENDA,
                p.CODIGO_FABRICANTE,
                f.DESCRICAO as FABRICANTE_DESCRICAO,
                p.CODIGO_LINHA,
                l.DESCRICAO as LINHA_DESCRICAO,
                p.DEIXAR_VENDER_NEGATIVO
            from PRODUTOS p
            left join FABRICANTES f on f.CODIGO_FABRICANTE = p.CODIGO_FABRICANTE
            left join LINHAS l on l.CODIGO_LINHA = p.CODIGO_LINHA
            where p.CODIGO_PRODUTO = @codigoProduto
            """;

        var command = new CommandDefinition(
            sql,
            new { codigoProduto },
            cancellationToken: cancellationToken
        );

        var row = await connection.QueryFirstOrDefaultAsync(command);
        if (row is null)
        {
            return null;
        }

        return DbRowMapper.MapToDictionaries(new[] { row }).FirstOrDefault();
    }
}
