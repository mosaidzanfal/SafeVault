using Microsoft.Data.SqlClient;
using SafeVault.Models;

namespace SafeVault.Data
{
    public interface IUserRepository
    {
        Task<bool> UserExistsAsync(string username);
        Task<VaultUser?> GetUserByUsernameAsync(string username);
        Task<VaultUser?> GetUserByIdAsync(int userId);
    }

    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            const string sql =
                "SELECT COUNT(1) FROM Users WHERE Username = @Username";

            await using var connection =
                new SqlConnection(_connectionString);

            await using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter(
                    "@Username",
                    System.Data.SqlDbType.NVarChar,
                    50)
                {
                    Value = username
                });

            await connection.OpenAsync();

            var result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }

        public async Task<VaultUser?> GetUserByUsernameAsync(string username)
        {
            const string sql = @"
        SELECT Id, Username, Email, PasswordHash, Role
        FROM Users
        WHERE Username = @Username";

            await using var connection =
                new SqlConnection(_connectionString);

            await using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter(
                    "@Username",
                    System.Data.SqlDbType.NVarChar,
                    50)
                {
                    Value = username
                });

            await connection.OpenAsync();

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new VaultUser
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                Email = reader.GetString(reader.GetOrdinal("Email")),

                PasswordHash = reader.IsDBNull(
                    reader.GetOrdinal("PasswordHash"))
                        ? string.Empty
                        : reader.GetString(
                            reader.GetOrdinal("PasswordHash")),

                Role = reader.GetString(reader.GetOrdinal("Role"))
            };
        }

        public async Task<VaultUser?> GetUserByIdAsync(int userId)
        {
            const string sql = @"
        SELECT Id, Username, Email, PasswordHash, Role
        FROM Users
        WHERE Id = @Id";

            await using var connection =
                new SqlConnection(_connectionString);

            await using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter(
                    "@Id",
                    System.Data.SqlDbType.Int)
                {
                    Value = userId
                });

            await connection.OpenAsync();

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new VaultUser
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                Email = reader.GetString(reader.GetOrdinal("Email")),

                PasswordHash = reader.IsDBNull(
                    reader.GetOrdinal("PasswordHash"))
                        ? string.Empty
                        : reader.GetString(
                            reader.GetOrdinal("PasswordHash")),

                Role = reader.GetString(reader.GetOrdinal("Role"))
            };
        }
    }
}
