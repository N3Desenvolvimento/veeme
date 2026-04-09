using System.Data;

namespace Veeme.Contracts;

public interface IFirebirdConnectionFactory
{
    IDbConnection CreateConnection();
}
