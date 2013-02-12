CSV Serializer for .NET
=======================

Features
---------

- Flattens object hierarchies
- Only includes members with DataMemberAttribute if the containing type has DataContractAttribute
- Supports anonymous types
- `IFlattener` allows external code to flatten an instance's member's values to an `IList<object>`


CSV Usage
---------

    using (var stream = File.Create("example.csv"))
    {
        Cameronism.Csv.Serializer.Serialize(stream, someIEnumerable);
    }

IFlattener Usage
-----------------

    var nestedInstance = new
        { 
            Hello = "World",
            Foo = new 
            {
                Bar = 42,
                Bop = Guid.Empty,
            },
        };

    IFlattener flattener = Cameronism.Csv.Serializer.CreateFlattener(nestedInstance.GetType());
    IList<object> values = flattener.Flatten(nestedInstance);

    Console.WriteLine(String.Join("\n", values));

    /*
    World
    42
    00000000-0000-0000-0000-000000000000
    */
