using Microsoft.Data.SqlClient;
using System.Data;

namespace ApiAstil.Services
{
    public class SqlDataService : ISqlDataService
    {
        private readonly string _connectionString;

        public SqlDataService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<DataTable> ExecuteStoredProcedureAsync(
            string procedureName,
            params (string Name, object Value)[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            await connection.OpenAsync();

            using var adapter = new SqlDataAdapter(command);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            return dataTable;
        }

        public async Task<T?> ExecuteScalarAsync<T>(
            string procedureName,
            params (string Name, object Value)[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();

            return result != null && result != DBNull.Value ? (T)result : default;
        }

        public async Task<int> ExecuteNonQueryAsync(
            string procedureName,
            params (string Name, object Value)[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }
    }
}
