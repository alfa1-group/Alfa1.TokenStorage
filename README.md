# Alfa1.TokenStorage

## Alfa1.TokenStorage.Abstractions
Contains the interface `ITokenStorageService` which defines how to store and retrieve the Refresh and Access Tokens.

This interface is implemented by several packages:

| Package | NuGet |
| :- | :- |
| Alfa1.TokenStorage.Azure.Blobs | [![NuGet Badge](https://img.shields.io/nuget/v/Alfa1.TokenStorage.Azure.Blobs)](https://www.nuget.org/packages/Alfa1.TokenStorage.Azure.Blobs)
| Alfa1.TokenStorage.FileSystem | [![NuGet Badge](https://img.shields.io/nuget/v/Alfa1.TokenStorage.FileSystem)](https://www.nuget.org/packages/Alfa1.TokenStorage.FileSystem)
| Alfa1.TokenStorage.SqlServer | [![NuGet Badge](https://img.shields.io/nuget/v/Alfa1.TokenStorage.SqlServer)](https://www.nuget.org/packages/Alfa1.TokenStorage.SqlServer)
| Alfa1.TokenStorage.PostgreSQL | [![NuGet Badge](https://img.shields.io/nuget/v/Alfa1.TokenStorage.PostgreSQL)](https://www.nuget.org/packages/Alfa1.TokenStorage.PostgreSQL)


## Used
This package is used by the following projects:
- https://github.com/alfa1-group/Alfa1.TokenStorage