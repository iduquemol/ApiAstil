using System.Data;

namespace ApiAstil.Services
{
    public interface ISqlDataService
    {
        Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, params (string Name, object Value)[] parameters);
        Task<T?> ExecuteScalarAsync<T>(string procedureName, params (string Name, object Value)[] parameters);
        Task<int> ExecuteNonQueryAsync(string procedureName, params (string Name, object Value)[] parameters);
    }
}
