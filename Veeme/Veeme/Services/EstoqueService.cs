using System.Data.Common;
using Dapper;
using Veeme.Contracts;
using Veeme.Utils;

namespace Veeme.Services;

public sealed class EstoqueService : IEstoqueService
{
    private readonly IFirebirdConnectionFactory _connectionFactory;
    private readonly string _defaultUsuario;
    private readonly string _defaultEstacao;

    public EstoqueService(IFirebirdConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _defaultUsuario = "Undefined";
        _defaultEstacao = Environment.MachineName;
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetUltimasMovimentacoesAsync(
        int codigoProduto,
        int take,
        CancellationToken cancellationToken
    )
    {
        if (codigoProduto <= 0)
        {
            return Array.Empty<Dictionary<string, object?>>();
        }

        take = Math.Clamp(take, 1, 50);

        using var connection = _connectionFactory.CreateConnection();

        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }
        else
        {
            connection.Open();
        }

        var sql =
            $"select first {take} CODIGO_AJUSTE, DATA, HORA, TIPO, SALDO_ANTERIOR, QTDE_INFORMADA, USUARIO, MOTIVO from AJUSTES_ESTOQUE where CODIGO_PRODUTO = @codigoProduto order by DATA desc, HORA desc, CODIGO_AJUSTE desc";

        var command = new CommandDefinition(
            sql,
            new { codigoProduto },
            cancellationToken: cancellationToken
        );

        var rows = await connection.QueryAsync(command);
        return DbRowMapper.MapToDictionaries(rows);
    }

    public async Task<Dictionary<string, object?>> MovimentarEstoqueAsync(
        int codigoProduto,
        string tipo,
        decimal quantidade,
        string? motivo,
        CancellationToken cancellationToken
    )
    {
        if (codigoProduto <= 0)
            throw new ArgumentException("Produto inválido.", nameof(codigoProduto));

        tipo = (tipo ?? string.Empty).Trim().ToUpperInvariant();
        if (tipo is not ("E" or "S"))
        {
            throw new ArgumentException(
                "Tipo inválido. Use 'E' (entrada) ou 'S' (saída).",
                nameof(tipo)
            );
        }

        if (quantidade <= 0)
        {
            throw new ArgumentException("Quantidade inválida.", nameof(quantidade));
        }

        motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();

        using var connection = _connectionFactory.CreateConnection();

        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }
        else
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction();

        try
        {
            var estoqueRow = await connection.QueryFirstOrDefaultAsync(
                new CommandDefinition(
                    "select ESTOQUE, DEIXAR_VENDER_NEGATIVO from PRODUTOS where CODIGO_PRODUTO = @codigoProduto",
                    new { codigoProduto },
                    transaction: transaction,
                    cancellationToken: cancellationToken
                )
            );

            if (estoqueRow is null)
            {
                throw new InvalidOperationException("Produto não encontrado.");
            }

            var mappedEstoque = DbRowMapper.MapToDictionaries(new[] { estoqueRow }).First();
            var estoqueAnterior = DbRowMapper.ConvertToDecimal(
                mappedEstoque.GetValueOrDefault("ESTOQUE")
            );
            var deixarNegativo = DbRowMapper.IsTruthy(
                mappedEstoque.GetValueOrDefault("DEIXAR_VENDER_NEGATIVO")
            );

            var estoqueAtual =
                tipo == "E" ? estoqueAnterior + quantidade : estoqueAnterior - quantidade;

            if (!deixarNegativo && estoqueAtual < 0)
            {
                throw new InvalidOperationException("Estoque insuficiente para realizar a saída.");
            }

            var codigoAjuste = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "select coalesce(max(CODIGO_AJUSTE), 0) + 1 from AJUSTES_ESTOQUE",
                    transaction: transaction,
                    cancellationToken: cancellationToken
                )
            );

            //await connection.ExecuteAsync(
            //    new CommandDefinition(
            //        "update PRODUTOS set ESTOQUE = @estoqueAtual, DATA_HORA_ALT = CURRENT_TIMESTAMP where CODIGO_PRODUTO = @codigoProduto",
            //        new { estoqueAtual, codigoProduto },
            //        transaction: transaction,
            //        cancellationToken: cancellationToken
            //    )
            //);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "insert into AJUSTES_ESTOQUE (CODIGO_AJUSTE, DATA, HORA, TIPO, CODIGO_PRODUTO, SALDO_ANTERIOR, QTDE_INFORMADA, USUARIO, ESTACAO, MOTIVO, CODIGO_DEPOSITO) values (@codigoAjuste, CURRENT_DATE, CURRENT_TIME, @tipo, @codigoProduto, @saldoAnterior, @qtdeInformada, @usuario, @estacao, @motivo, @codigoDeposito)",
                    new
                    {
                        codigoAjuste,
                        tipo,
                        codigoProduto,
                        saldoAnterior = estoqueAnterior,
                        qtdeInformada = quantidade,
                        usuario = _defaultUsuario,
                        estacao = _defaultEstacao,
                        motivo,
                        codigoDeposito = 1,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken
                )
            );

            transaction.Commit();

            return new Dictionary<string, object?>
            {
                ["CODIGO_AJUSTE"] = codigoAjuste,
                ["CODIGO_PRODUTO"] = codigoProduto,
                ["TIPO"] = tipo,
                ["QTDE_INFORMADA"] = quantidade,
                ["SALDO_ANTERIOR"] = estoqueAnterior,
                ["SALDO_FINAL"] = estoqueAtual,
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
