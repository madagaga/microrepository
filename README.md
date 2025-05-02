# microrepository

Inspired by Dapper and PetaPoco, this project is a simple, tiny ORM implementing the repository pattern for .NET. It is designed for learning purposes and works similarly to Entity Framework in terms of usage.

---

## Architecture & Core Features

### MicroRepository.Core

- **Type Conversion System**
  - `TypeConverterCache` provides robust, extensible type conversion between .NET types, supporting both custom and default converters.
  - Uses expression trees and concurrent dictionaries for high performance and thread safety.

- **Fast Property Access & Manipulation**
  - `CompiledPropertyAccessor<T>` generates compiled delegates for property get/set/copy operations, greatly improving performance over standard reflection.
  - Supports copying property values between objects, retrieving default values, and robust error handling.

- **Database Connection Extensions**
  - `IDbConnectionExtension` adds a suite of extension methods to `IDbConnection`:
    - `Execute`: Run non-query SQL commands.
    - `Query`/`Query<T>`: Execute SQL and map results to dynamic rows or strongly-typed objects.
    - `QueryFirst`/`QueryFirstOrDefault`: Fetch single results.
    - `ExecuteScalar<T>`: Run scalar queries with type conversion.
  - Internally uses property accessor and constructor caching for fast object materialization.
  - Handles parameter binding, connection state management, and supports both buffered and streaming (yield) result sets.

- **Reflection Caching**
  - `ReflectionCache` caches property accessors and parameterless constructors for all types used in the ORM.
  - Reduces the overhead of repeated reflection calls, further improving performance.
  - Provides methods to create new instances of types and clear caches when needed.

- **Thread Safety & Performance**
  - All caches use `ConcurrentDictionary` for thread safety.
  - Expression trees are used throughout for runtime code generation, minimizing runtime overhead.

- **Extensibility & Robustness**
  - Modular architecture allows for easy extension (e.g., adding new type converters, supporting new data types).
  - Exception handling and argument validation are present throughout, improving reliability.

---

## Project Structure

The solution is organized into two main projects:

- **MicroRepository.Core**  
  Core abstractions, type conversion, caching, and schema utilities.
  - `Caching/` - Reflection-based caching utilities.
  - `DynamicParameters/` - Support for dynamic query parameters.
  - `IDbConnection/` - Extensions for database connections.
  - `Schema/` - Property accessors and row mapping.
  - `TypeConversion/` - Type conversion and caching logic.

- **MicroRepository.Repository**  
  Implements the repository pattern, providing both read-only and read-write repositories, schema mapping, SQL generation, and extensibility features.
  - `Repository/` - Core repository classes:
      - `Repository<TEntity>`: Full CRUD repository, implements `IRepository<TEntity>`.
      - `ReadOnlyRepository<TEntity>`: Base for read operations, implements `IReadOnlyRepository<TEntity>`.
  - `Interfaces/` - Repository contracts:
      - `IRepository<TEntity>`: Add, Remove, Update, and read operations.
      - `IReadOnlyRepository<TEntity>`: Read/query operations.
  - `Attributes/` - Data annotations for mapping (e.g., `KeyAttribute`, `MapAttribute`, `NotMappedAttribute`).
  - `Schema/` - Table and column descriptors for mapping .NET types to database schema.
  - `Sql/SqlBuilder/` - SQL builder and clause logic for dynamic SQL generation.
  - `Caching/` - Table definition caching for performance.
  - `EnumerableRepository/` - Enumerable repository pattern and LINQ-style extensions.
  - `Enums/` - Database type and case format enums.
  - `ExpressionParser/` - Expression parsing for translating LINQ expressions to SQL.
  - `Paging/` - Paging interfaces and implementations (`IPagedList`, `PagedList`).
  - `Templates/` - SQL template support for customizable queries.

**Key Features:**
- Implements the repository pattern with clear separation of read-only and read-write operations.
- Strongly-typed schema mapping using descriptors and attributes.
- Dynamic SQL generation using builders and templates.
- LINQ-style querying and expression parsing for flexible filtering.
- Paging, caching, and extensibility for advanced scenarios.
- Designed for performance and maintainability, leveraging core features from MicroRepository.Core.

---

## Getting Started

### Initializing a Repository

**Explicit Implementation:**
```csharp
public class DbContext : MicroRepository.Repository.Repositories
{
    public DbContext() : base("default") { }

    public Repository<Configuration> Configurations { get; set; }
    public Repository<Account> Accounts { get; set; }
    public Repository<Payment> Payments { get; set; }
}
```
Where `"default"` is the name of the connection string. The default initialization uses a discovery service like EF. You can create your own and initialize on demand.

**Implicit Implementation:**
```csharp
public class DbContext : MicroRepository.Repository.Repositories
{
    public DbContext() : base(new System.Data.SQLite.SQLiteConnection("Data Source=local.db;")) { }
}
```

You can create your own repository by implementing `IRepository<TEntity>`.  
`IRepository` exposes:

```csharp
EnumerableRepository<TEntity> Elements { get; }
TEntity Add(TEntity item);
bool Remove(TEntity item);
TEntity Update(TEntity item);
TEntity Find(params object[] orderedKeyValues);
IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object parameter = null);
```

---

## Usage Examples

### 1. Define a DbContext

```csharp
public class DbContext : MicroRepository.Repository.Repositories
{
    public DbContext() : base("default") { }

    public Repository<Account> Accounts { get; set; }
    public Repository<Payment> Payments { get; set; }
    // Add more repositories as needed
}
```

### 2. Add a new entity

```csharp
var context = new DbContext();
var newAccount = new Account { Email = "user@example.com", Name = "User" };
Account inserted = context.Accounts.Add(newAccount);
```

### 3. Update an entity

```csharp
inserted.Name = "Updated Name";
Account updated = context.Accounts.Update(inserted);
```

### 4. Remove an entity

```csharp
bool removed = context.Accounts.Remove(updated);
```

### 5. Find by primary key

```csharp
Account? found = context.Accounts.Find(primaryKeyValue);
```

### 6. Query with LINQ-style filtering

```csharp
// LINQ-style filtering directly on the repository
var filtered = context.Accounts.Where(a => a.Email == "user@example.com").ToList();
```

### 7. Execute raw SQL

```csharp
var results = context.Accounts.ExecuteQuery("SELECT * FROM Accounts WHERE IsActive = 1");
```

### 8. Paging

```csharp
var paged = context.Accounts.Elements.Skip(10).Take(10).ToList();
```

> Note: EnumerableRepository conditions are translated into SQL. Once enumerated (e.g., ToList()), further filtering must be done in-memory.

---

## SQL Components

- `Elements.AndRawSql("column=value")` → AND column=value
- `Elements.OrRawSql("column=value")` → OR column=value
- `Elements.In(c => c.Column, columnValueArray)` → AND table.column IN (columnValueArray)
- `Elements.NotIn(c => c.Column, columnValueArray)` → AND table.column NOT IN (columnValueArray)
- `Elements.OrIn(c => c.Column, columnValueArray)` → OR table.column IN (columnValueArray)
- `Elements.Any()` → SELECT COUNT(*) ... != 0
- `Elements.LeftJoin<Entity2>((t1, t2) => t1.Id == t2.t1Id)` → LEFT JOIN
- `Elements.Select(c => c.Column)` → SELECT Column
- `Elements.Count()` → SELECT COUNT(*)
- `Elements.Distinct()` → DISTINCT
- `Elements.FirstOrDefault()` → LIMIT 1
- `Elements.GroupBy(c => c.Column)` → GROUP BY
- `Elements.Last()` → IEnumerable implementation
- `Elements.OrderBy(c => c.Col)` → ORDER BY ASC
- `Elements.OrderByDescending(c => c.Col)` → ORDER BY DESC
- `Elements.Skip(3)` → OFFSET 3 (SQLite/MySQL)
- `Elements.Take(3)` → LIMIT 3 / TOP 3
- `Elements.Where(...).OrWhere(...)` → WHERE (...) OR (...)

**Methods:**
- `string.Contains` → LIKE %pattern%
- `!string.Contains` → NOT LIKE
- `string.StartsWith` → LIKE pattern%
- `string.EndsWith` → LIKE %pattern
- `enum.HasFlag` → (enum & value) = value
- `== null` → IS NULL
- `!= null` → IS NOT NULL
- `c.boolean` → = true
- `!c.boolean` → = false

---

## Configuration Options

**Buffering:**
- `DbSettings.Buffered` (default: true): If true, queries are buffered (results are materialized as a list). If false, results are streamed.

**Quoting:**
- `DbSettings.EnquoteTableNames` (default: true): If true, table names are quoted in generated SQL.
- `DbSettings.EnquoteColumnNames` (default: true): If true, column names are quoted in generated SQL.

**Database Type:**
- `DbSettings.SetDbType(DatabaseType dbType)`: Sets the target database type (SQLServer, MySql, SQLite, Postgres) and configures SQL templates accordingly.

---

## Notes

- The project is for educational purposes and is not production-ready.
- Contributions and suggestions are welcome.
- See the source code for more advanced usage and extension points.
