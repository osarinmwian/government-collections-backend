using Microsoft.Data.SqlClient;

namespace GovernmentCollections.Data.Context;

public interface IGovernmentCollectionsContext
{
    SqlConnection GetConnection();
}

public class GovernmentCollectionsContext : IGovernmentCollectionsContext
{
    private readonly string _connectionString;

    public GovernmentCollectionsContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }
}