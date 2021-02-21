# graph-ql-poco

An experiment with GraphQL and a custom Dapper based SQL resolver which defines the GraphQL schema and queries a DB based on introspection of the model.

Given the model:

```csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Customer Customer { get; set; }
}
```

And the setup:

```csharp
services.SetupGraphQLSchema(
    schema => schema
        .DefaultResolver(
            new SQLResolver(
                () => new SqlConnection(
                    @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=GraphQLTest;Integrated Security=SSPI"
                )
            )
        )
        .Add<Customer>(x => x
            .EntityConfig(e => e.Table("Customers").Key(f => f.Id))
            .QueryConfig(q => q.QueryableBy(f => f.Id))
        )
        .Add<Order>(x => x
            .EntityConfig(e => e.Table("Orders").Key(f => f.Id))
            .QueryConfig(q => q.QueryableBy(f => f.Id))
        )
);
```

Allows the following GraphQL query:

```graphql
customers {
  id
  name
  orders {
    id
    name
  }
}
```

or alternatively:

```graphql
orders {
  id
  name
  customer {
    id
    name
  }
}
```
