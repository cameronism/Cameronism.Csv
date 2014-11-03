CSV Serializer for .NET
=======================

Features
---------

- Flattens object hierarchies
- Only includes members with DataMemberAttribute if the containing type has DataContractAttribute
- Supports anonymous types
- `IFlattener` allows external code to flatten an instance's member's values to an `IList<object>`
- Supports dynamic columns: column names and keys can be passed after enumerable to serialize


CSV Usage
---------

    using (var stream = File.Create("example.csv"))
    {
        Cameronism.Csv.Serializer.Serialize(stream, someIEnumerable);
    }


CSV With Dynamic Columns
------------------------

Column headings and keys can be provided to serialize after instance properties and fields.
Dynamic columns require an indexer, the indexer will be called once for every key for every
item in the enumerable.

    using (var stream = File.Create("extra-columns.csv"))
    {
        Cameronism.Csv.Serializer.Serialize(stream, anotherIEnumerable, new[] {
			new KeyValuePair<string, SomeType>("Gate", keeper),
			new KeyValuePair<string, SomeType>("Key", master),
			new KeyValuePair<string, SomeType>("Only", zuul),
		});
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
