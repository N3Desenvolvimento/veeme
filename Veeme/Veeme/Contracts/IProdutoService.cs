namespace Veeme.Contracts;

public interface IProdutoService
{
    Task<IReadOnlyList<Dictionary<string, object?>>> SearchAsync(
        string? nome,
        string? codigoBarras,
        CancellationToken cancellationToken
    );

    Task<Dictionary<string, object?>?> GetDetalheAsync(
        int codigoProduto,
        CancellationToken cancellationToken
    );
}
