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
