# microrepository
Inspired by Dapper, petapoco, for learning purposes.
It is a simple tiny orm with repository pattern.
It work (a little bit) like entity framework (usage is relatively the same)

# initializing Repository
```csharp
public class DbContext : MicroRepository.Repository.Repositories
{
    public DbContext() : base("default") { }

    public Repository<Configuration> Configurations { get; set; }
    public Repository<Account> Accounts { get; set; }
    public Repository<Payment> Payments { get; set; }
}
```
Where  'default' is the name of the connection string. The default initialization uses a discovery service like EF. You can create your own and initialize on demand
```csharp
Repository<Configuration>
``` 
is the defaut usage. You can create yout own repo by implemeting IRepository
IRepository exposes 
```csharp
// for lambda query
SqlQueryableResult<TEntity> Elements { get; }
// add 
TEntity Add(TEntity item);
// remove
bool Remove(TEntity item);
// update 
TEntity Update(TEntity item);        
// find by primary kers
TEntity Find(params object[] orderedKeyValues);
// execute raw query
IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object parameter = null);            
```

# usage 
Get all entities 
```csharp
List<Account> accounts = new DbContext().Accounts.Elements;
```

Filtering example 
```csharp
List<Account> accounts = new DbContext().Accounts.Elements.Where(c => c.Email == email);
```
SqlQueryableResult conditions are translated into sql. So you can not update condition if result has been enumerated (like IQueryable)

# Available sql components 
```
Elements.AndRawSql("column=value"); => AND table.column=value
Elements.OrRawSql("column=value"); => OR table.column=value
Elements.In(c=>c.Column, columnValueArray) => AND table.column IN (columnValueArray)
Elements.OrIn(c=>c.Column, columnValueArray) => OR table.column IN (columnValueArray)
Elements.Any() => execute a select count and return count != 0 (with or without param)
Elements.Count() => Select Count(*) from table, with or without param 
Elements.Distinct() => add distinct to sql query
Elements.FirstOrDefault() => Add take 1 to sql query
Elements.GroupBy(c=>c.Column) => GROUP BY Table.Column
Element.Last() => Ienumerable implementation 
Elements.OrderBy(c=>c.Col) => ORDER BY Table.Col ASC
Elements.OrderByDescending(c=>c.Col) => ORDER BY Table.Col DESC
Elements.Skip(3) => OFFSET 3 for sqlite/mysql not implemented for sqlserver
Elements.Take(3) => LIMIT 3 / TOP 3
Elements.Where(c=>c.boolean == true && c=>c.int >= 10 && c.enum.HasFlag(v) => WHERE (table.boolean = true/1 AND table.int >= 10 AND (table.enum & v) = v)

Elements.Where(...).OrWhere(...) => WHERE (...) OR ( ... ) 
```
# Options
Buffering or not 
There is a DbFactory class for configuration.
DbFactory.Buffered
- true (default): executes and return a list 
- false : yield datareader results . Datareader is not closed.

Updating 
DbFactory.UpdateChangeOnly
- true (default) : generate sql for changed value only. no changes, no request.
- false : generate full update everytime

