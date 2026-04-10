using System.Data;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Options;
using Veeme.Contracts;
using Veeme.Models;

namespace Veeme.Data;

public sealed class FirebirdConnectionFactory : IFirebirdConnectionFactory
{
    private readonly FirebirdDatabaseOptions _options;

    public FirebirdConnectionFactory(IOptions<FirebirdDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public IDbConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_options.Database))
        {
            throw new InvalidOperationException(
                "Configuração Firebird:Database não foi informada."
            );
        }

        if (string.IsNullOrWhiteSpace(_options.User))
        {
            throw new InvalidOperationException("Configuração Firebird:User não foi informada.");
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "Configuração Firebird:Password não foi informada."
            );
        }

        var connectionStringBuilder = new FbConnectionStringBuilder
        {
            DataSource = string.IsNullOrWhiteSpace(_options.Server) ? "localhost" : _options.Server,
            Port = _options.Port,
            Database = _options.Database,
            UserID = _options.User,
            Password = _options.Password,
            Dialect = _options.Dialect,
        };

        var charset = _options.Charset?.Trim();
        if (!string.IsNullOrWhiteSpace(charset))
        {
            connectionStringBuilder.Charset = charset;
        }

        return new FbConnection(connectionStringBuilder.ToString());
    }
}
