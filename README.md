# microrepository
Inspired by Dapper, petapoco, for learning purposes.
It is a simple tiny orm with repository pattern.
It work (a little bit) like entity framework (usage is relatively the same)

# Initializing Repository  
With a explicit implementation 
```csharp
public class DbContext : MicroRepository.Repository.Repositories
{
    public DbContext() : base("default") { }

    public Repository<Configuration> Configurations { get; set; }
    public Repository<Account> Accounts { get; set; }
    public Repository<Payment> Payments { get; set; }
}
```
Where  'default' is the name of the connection string. The default initialization uses a discovery service like EF. You can create your own and initialize on demand.

Whith implicit implementation 

```csharp
public class DbContext : MicroRepository.Repository.Repositories
{
    public DbContext():base(new System.Data.SQLite.SQLiteConnection("Data Source=local.db;"))
    {

    }
}
```

You can create yout own repo by implemeting IRepository
IRepository exposes 
```csharp
 /// <summary>
        /// Elements - data queryable 
        /// </summary>
        EnumerableRepository<TEntity> Elements { get; }

        /// <summary>
        /// Add an element to database
        /// </summary>
        /// <param name="item">element to be added </param>
        /// <returns></returns>
        TEntity Add(TEntity item);

        /// <summary>
        /// Removes element from database 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool Remove(TEntity item);

        /// <summary>
        /// Updates element in database <seealso cref="RepositoryDiscoveryService.UpdateChangeOnly"/>
        /// </summary>
        /// <param name="item">item to upload </param>
        /// <returns>element updated from database</returns>
        TEntity Update(TEntity item);

        /// <summary>
        /// Find an element by its primary key
        /// class bust be decorated with KeyAttribute
        /// </summary>
        /// <param name="orderedKeyValues">primary key s</param>
        /// <returns>Found element </returns>
        TEntity Find(params object[] orderedKeyValues);

        /// <summary>
        /// Execute a raw query 
        /// </summary>
        /// <param name="sqlQuery">sql query</param>
        /// <param name="parameter">object parameter</param>
        /// <returns>Found element</returns>
        IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object parameter = null);         
```

# usage 
Get all entities 
```csharp
List<Account> accounts = new DbContext().Accounts.Elements;
Or 
List<Account> accounts = new DbContext().GetRepository<Account>().Elements;
```

Filtering example 
```csharp
List<Account> accounts = new DbContext().Accounts.Elements.Where(c => c.Email == email);
```
EnumerableRepository conditions are translated into sql. So you can not update condition if result has been enumerated (like IQueryable).

# Available sql components 
```
Elements.AndRawSql("column=value"); => AND column=value
Elements.OrRawSql("column=value"); => OR column=value
Elements.In(c=>c.Column, columnValueArray) => AND table.column IN (columnValueArray)
Elements.NotIn(c=>c.Column, columnValueArray) => AND table.column IN (columnValueArray)
Elements.OrIn(c=>c.Column, columnValueArray) => OR table.column IN (columnValueArray)
Elements.Any() => execute a select count and return count != 0 (with or without param)
Elements.LeftJoin<Entity2>((t1, t2)=>t1.Id == t2.t1Id) => Left join Table2 on Table1.Id = Table2.t1Id
Elements.Select(c=>c.Column) => Select Column 
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
# Methods 
```
string.contains => LIKE %pattern%
!string.containes => NOT LIKE
string.startwith => LIKE pattern%
string.endwith => LIKE %pattern
enum.Hasflag => (enum & value) = value
== null => ISNULL
!= null => IS NOT NULL
c.boolean => = true
!c.boolean => = false
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

