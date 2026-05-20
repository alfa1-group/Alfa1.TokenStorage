using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Alfa1.TokenStorage.SqlServer.Data;

public partial class AuthenticationTokenDbContext
{
    private enum DatabaseProviderType
    {
        SqlServer,
        PostgreSql,
        Sqlite,
        Other
    }

    internal void EnsureAuthenticationTokenTableExists()
    {
        if (!TableExists())
        {
            var createTableSql = GetDatabaseProviderType() switch
            {
                DatabaseProviderType.SqlServer => $@"
                    CREATE TABLE [{_storageOptions.TableName}] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        [{_storageOptions.TokenIdentifierColumnName}] NVARCHAR(256) NOT NULL,
                        [{_storageOptions.RefreshTokenColumnName}] NVARCHAR(1024) NOT NULL,
                        [{_storageOptions.RefreshTokenUpdatedAtColumnName}] DATETIMEOFFSET NOT NULL,
                        [{_storageOptions.AccessTokenColumnName}] NVARCHAR(1024) NULL,
                        [{_storageOptions.AccessTokenUpdatedAtColumnName}] DATETIMEOFFSET NOT NULL,
                        [{_storageOptions.AccessTokenExpireColumnName}] DATETIMEOFFSET NOT NULL,
                        RowVersion ROWVERSION NOT NULL
                    )",

                DatabaseProviderType.PostgreSql => $@"
                    CREATE TABLE ""{_storageOptions.TableName}"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""{_storageOptions.TokenIdentifierColumnName}"" VARCHAR(256) NOT NULL,
                        ""{_storageOptions.RefreshTokenColumnName}"" VARCHAR(1024) NOT NULL,
                        ""{_storageOptions.RefreshTokenUpdatedAtColumnName}"" TIMESTAMPTZ NOT NULL,
                        ""{_storageOptions.AccessTokenColumnName}"" VARCHAR(1024) NULL,
                        ""{_storageOptions.AccessTokenUpdatedAtColumnName}"" TIMESTAMPTZ NOT NULL,
                        ""{_storageOptions.AccessTokenExpireColumnName}"" TIMESTAMPTZ NOT NULL,
                        ""RowVersion"" BYTEA NOT NULL DEFAULT ''::bytea
                    )",

                DatabaseProviderType.Sqlite => $@"
                    CREATE TABLE [{_storageOptions.TableName}] (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        [{_storageOptions.TokenIdentifierColumnName}] TEXT NOT NULL,
                        [{_storageOptions.RefreshTokenColumnName}] TEXT NOT NULL,
                        [{_storageOptions.RefreshTokenUpdatedAtColumnName}] TEXT NOT NULL,
                        [{_storageOptions.AccessTokenColumnName}] TEXT NULL,
                        [{_storageOptions.AccessTokenUpdatedAtColumnName}] TEXT NOT NULL,
                        [{_storageOptions.AccessTokenExpireColumnName}] TEXT NOT NULL,
                        RowVersion BLOB NOT NULL DEFAULT (X'')
                    )",

                _ => throw new NotSupportedException($"Database provider {Database.ProviderName} is not supported")
            };

            Database.ExecuteSqlRaw(createTableSql);
        }

        EnsureTokenIdentifierColumnExists();
        EnsureTokenIdentifierUniqueIndexExists();
    }

    private DatabaseProviderType GetDatabaseProviderType()
    {
        var providerName = Database.ProviderName?.ToLowerInvariant() ?? string.Empty;

        if (providerName.Contains("sqlserver"))
        {
            return DatabaseProviderType.SqlServer;
        }

        if (providerName.Contains("npgsql"))
        {
            return DatabaseProviderType.PostgreSql;
        }

        if (providerName.Contains("sqlite"))
        {
            return DatabaseProviderType.Sqlite;
        }

        return DatabaseProviderType.Other;
    }

    private bool TableExists()
    {
        var provider = GetDatabaseProviderType();
        var checkTableExistsSql = provider switch
        {
            DatabaseProviderType.SqlServer =>
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName) THEN 1 ELSE 0 END",

            DatabaseProviderType.PostgreSql =>
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = @tableName)",

            DatabaseProviderType.Sqlite =>
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@tableName",

            _ => throw new NotSupportedException($"Database provider {Database.ProviderName} is not supported")
        };

        var exists = false;

        using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = checkTableExistsSql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = _storageOptions.TableName;
        command.Parameters.Add(parameter);

        Database.OpenConnection();
        try
        {
            using var result = command.ExecuteReader();
            if (result.Read())
            {
                exists = provider == DatabaseProviderType.PostgreSql ? result.GetBoolean(0) : result.GetInt32(0) > 0;
            }
        }
        finally
        {
            Database.CloseConnection();
        }

        return exists;
    }

    private void EnsureTokenIdentifierColumnExists()
    {
        if (ColumnExists(_storageOptions.TokenIdentifierColumnName))
        {
            return;
        }

        var alterTableSql = GetDatabaseProviderType() switch
        {
            DatabaseProviderType.SqlServer => $"ALTER TABLE [{_storageOptions.TableName}] ADD [{_storageOptions.TokenIdentifierColumnName}] NVARCHAR(256) NOT NULL CONSTRAINT [DF_{_storageOptions.TableName}_{_storageOptions.TokenIdentifierColumnName}] DEFAULT ('{_storageOptions.TokenIdentifier.Replace("'", "''")}')",
            DatabaseProviderType.PostgreSql => $"ALTER TABLE \"{_storageOptions.TableName}\" ADD COLUMN \"{_storageOptions.TokenIdentifierColumnName}\" VARCHAR(256) NOT NULL DEFAULT '{_storageOptions.TokenIdentifier.Replace("'", "''")}'",
            DatabaseProviderType.Sqlite => $"ALTER TABLE [{_storageOptions.TableName}] ADD COLUMN [{_storageOptions.TokenIdentifierColumnName}] TEXT NOT NULL DEFAULT '{_storageOptions.TokenIdentifier.Replace("'", "''")}'",
            _ => throw new NotSupportedException($"Database provider {Database.ProviderName} is not supported")
        };

        Database.ExecuteSqlRaw(alterTableSql);
    }

    private bool ColumnExists(string columnName)
    {
        var provider = GetDatabaseProviderType();
        var checkColumnExistsSql = provider switch
        {
            DatabaseProviderType.SqlServer =>
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName) THEN 1 ELSE 0 END",

            DatabaseProviderType.PostgreSql =>
                "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = @tableName AND column_name = @columnName)",

            DatabaseProviderType.Sqlite =>
                $"PRAGMA table_info([{_storageOptions.TableName}])",

            _ => throw new NotSupportedException($"Database provider {Database.ProviderName} is not supported")
        };

        Database.OpenConnection();
        try
        {
            using var command = Database.GetDbConnection().CreateCommand();
            command.CommandText = checkColumnExistsSql;

            if (provider != DatabaseProviderType.Sqlite)
            {
                var tableNameParameter = command.CreateParameter();
                tableNameParameter.ParameterName = "@tableName";
                tableNameParameter.Value = _storageOptions.TableName;
                command.Parameters.Add(tableNameParameter);

                var columnNameParameter = command.CreateParameter();
                columnNameParameter.ParameterName = "@columnName";
                columnNameParameter.Value = columnName;
                command.Parameters.Add(columnNameParameter);
            }

            using var result = command.ExecuteReader();

            if (provider == DatabaseProviderType.Sqlite)
            {
                while (result.Read())
                {
                    var sqliteColumnName = result.GetString(1);
                    if (string.Equals(sqliteColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return result.Read() && (provider == DatabaseProviderType.PostgreSql ? result.GetBoolean(0) : result.GetInt32(0) > 0);
        }
        finally
        {
            Database.CloseConnection();
        }
    }

    private void EnsureTokenIdentifierUniqueIndexExists()
    {
        var indexName = $"UX_{_storageOptions.TableName}_{_storageOptions.TokenIdentifierColumnName}";

        var createIndexSql = GetDatabaseProviderType() switch
        {
            DatabaseProviderType.SqlServer => $@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{indexName}' AND object_id = OBJECT_ID(N'[{_storageOptions.TableName}]'))
                BEGIN
                    CREATE UNIQUE INDEX [{indexName}] ON [{_storageOptions.TableName}] ([{_storageOptions.TokenIdentifierColumnName}])
                END",

            DatabaseProviderType.PostgreSql =>
                $"CREATE UNIQUE INDEX IF NOT EXISTS \"{indexName}\" ON \"{_storageOptions.TableName}\" (\"{_storageOptions.TokenIdentifierColumnName}\")",

            DatabaseProviderType.Sqlite =>
                $"CREATE UNIQUE INDEX IF NOT EXISTS [{indexName}] ON [{_storageOptions.TableName}] ([{_storageOptions.TokenIdentifierColumnName}])",

            _ => throw new NotSupportedException($"Database provider {Database.ProviderName} is not supported")
        };

        Database.ExecuteSqlRaw(createIndexSql);
    }
}