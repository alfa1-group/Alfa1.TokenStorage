# Alfa1.TokenStorage - Registration Guide

This package family supports multiple storage providers for `ITokenStorageService`.

## Registration styles

All providers support these patterns:

1. **Default registration** (single `ITokenStorageService`)
2. **Keyed registration** (multiple named token stores)
3. **Consumer-bound registration** (bind a dependent type to one token identifier)

---

## SQL Server

```csharp
// Default registration (single ITokenStorageService)
services.AddTokenStorageSqlServer(configuration);

// Keyed registrations (multiple named token stores)
services.AddKeyedTokenStorageSqlServer("TypeA", "TypeAToken", configuration);
services.AddKeyedTokenStorageSqlServer("TypeB", "TypeBToken", configuration);

// Individual registrations (bind token store per dependent type)
services.AddTokenStorageSqlServerFor<TypeA>(configuration, "TypeAToken");
services.AddTokenStorageSqlServerFor<TypeB>(configuration); // defaults to "TypeB"
```

SQL Server schema initialization is explicit and should be done during app startup, after DI registration:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTokenStorageSqlServer(builder.Configuration);

var app = builder.Build();

app.Services.InitializeTokenStorageSqlServer();
```

This startup-time initialization is the recommended pattern.

`appsettings.json` for SQL Server:

```json
{
  "ConnectionStrings": {
    "Tokens": "Server=.;Database=TokenDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "SqlServerTokenStorageOptions": {
    "ConnectionStringName": "Tokens",
    "TableName": "Tokens",
    "TokenIdentifierColumnName": "TokenIdentifier",
    "TokenIdentifier": "default",
    "RefreshTokenColumnName": "RefreshToken",
    "RefreshTokenUpdatedAtColumnName": "RefreshTokenUpdatedAt",
    "AccessTokenColumnName": "AccessToken",
    "AccessTokenUpdatedAtColumnName": "AccessTokenUpdatedAt",
    "AccessTokenExpireColumnName": "AccessTokenExpireAt"
  }
}
```

## PostgreSQL

```csharp
// Default registration (single ITokenStorageService)
services.AddAuthenticationTokenStoragePostgreSQL(configuration);

// Keyed registrations (multiple named token stores)
services.AddKeyedAuthenticationTokenStoragePostgreSQL("TypeA", "TypeAToken", configuration);
services.AddKeyedAuthenticationTokenStoragePostgreSQL("TypeB", "TypeBToken", configuration);

// Individual registrations (bind token store per dependent type)
services.AddAuthenticationTokenStoragePostgreSQLFor<TypeA>(configuration, "TypeAToken");
services.AddAuthenticationTokenStoragePostgreSQLFor<TypeB>(configuration); // defaults to "TypeB"
```

`appsettings.json` for PostgreSQL:

```json
{
  "ConnectionStrings": {
    "Tokens": "Host=localhost;Port=5432;Database=token_db;Username=postgres;Password=postgres"
  },
  "PostgreSQLTokenStorageOptions": {
    "ConnectionStringName": "Tokens",
    "TableName": "Tokens",
    "TokenIdentifierColumnName": "TokenIdentifier",
    "TokenIdentifier": "default",
    "RefreshTokenColumnName": "RefreshToken",
    "RefreshTokenUpdatedAtColumnName": "RefreshTokenUpdatedAt",
    "AccessTokenColumnName": "AccessToken",
    "AccessTokenUpdatedAtColumnName": "AccessTokenUpdatedAt",
    "AccessTokenExpireColumnName": "AccessTokenExpireAt"
  }
}
```

## File system

```csharp
// Default registration (single ITokenStorageService)
services.AddTokenStorageFileSystem(configuration);

// Keyed registrations (multiple named token stores)
services.AddKeyedTokenStorageFileSystem("TypeA", "TypeAToken", configuration);
services.AddKeyedTokenStorageFileSystem("TypeB", "TypeBToken", configuration);

// Individual registrations (bind token store per dependent type)
services.AddTokenStorageFileSystemFor<TypeA>(configuration, "TypeAToken");
services.AddTokenStorageFileSystemFor<TypeB>(configuration); // defaults to "TypeB"
```

`appsettings.json` for File system:

```json
{
  "FileSystemTokenStorageOptions": {
    "TokenIdentifier": "default",
    "RefreshTokenFilePathTemplate": "tokens/refresh.{tokenIdentifier}.txt",
    "AccessTokenFilePathTemplate": "tokens/access.{tokenIdentifier}.txt"
  }
}
```

`FileSystemTokenStorageOptions` supports template paths using `{tokenIdentifier}`:

```json
{
  "FileSystemTokenStorageOptions": {
    "RefreshTokenFilePathTemplate": "tokens/refresh.{tokenIdentifier}.txt",
    "AccessTokenFilePathTemplate": "tokens/access.{tokenIdentifier}.txt"
  }
}
```

For `TypeAToken` this resolves to `tokens/refresh.TypeAToken.txt` and `tokens/access.TypeAToken.txt`.

## Azure Blobs

```csharp
// Default registration (single ITokenStorageService)
services.AddTokenStorageAzureBlobs(configuration);

// Keyed registrations (multiple named token stores)
services.AddKeyedTokenStorageAzureBlobs("TypeA", "TypeAToken", configuration);
services.AddKeyedTokenStorageAzureBlobs("TypeB", "TypeBToken", configuration);

// Individual registrations (bind token store per dependent type)
services.AddTokenStorageAzureBlobsFor<TypeA>(configuration, "TypeAToken");
services.AddTokenStorageAzureBlobsFor<TypeB>(configuration); // defaults to "TypeB"
```

`appsettings.json` for Azure Blobs:

```json
{
  "AzureBlobsTokenStorageOptions": {
    "ConnectionString": "<azure-storage-connection-string>",
    "ContainerName": "tokens",
    "TokenIdentifier": "default",
    "RefreshTokenFilePathTemplate": "refresh.{tokenIdentifier}.txt",
    "AccessTokenFilePathTemplate": "access.{tokenIdentifier}.txt"
  }
}
```

`AzureBlobsTokenStorageOptions` supports template blob paths using `{tokenIdentifier}`:

```json
{
  "AzureBlobsTokenStorageOptions": {
    "RefreshTokenFilePathTemplate": "refresh.{tokenIdentifier}.txt",
    "AccessTokenFilePathTemplate": "access.{tokenIdentifier}.txt"
  }
}
```

For `TypeAToken` this resolves to `refresh.TypeAToken.txt` and `access.TypeAToken.txt`.

---

## Mix and match providers

You can register different providers side-by-side using keyed services:

```csharp
services.AddKeyedTokenStorageSqlServer("ApiA", "ApiAToken", configuration);
services.AddKeyedTokenStorageAzureBlobs("ApiB", "ApiBToken", configuration);
```

Then resolve by key where needed:

```csharp
var tokenStorageA = serviceProvider.GetRequiredKeyedService<ITokenStorageService>("ApiA");
var tokenStorageB = serviceProvider.GetRequiredKeyedService<ITokenStorageService>("ApiB");
```

## Notes

- `ITokenStorageService` remains unchanged.
- Token scoping is controlled internally by `tokenIdentifier`.
- If you omit `tokenIdentifier` in `...For<T>()`, the default is the dependent type name.
