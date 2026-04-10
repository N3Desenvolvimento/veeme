namespace Veeme.Contracts;

public interface IEstoqueService
{
    Task<IReadOnlyList<Dictionary<string, object?>>> GetUltimasMovimentacoesAsync(
        int codigoProduto,
        int take,
        CancellationToken cancellationToken
    );

    Task<Dictionary<string, object?>> MovimentarEstoqueAsync(
        int codigoProduto,
        string tipo,
        decimal quantidade,
        string? motivo,
        CancellationToken cancellationToken
    );
}
