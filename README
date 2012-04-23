CSV Serializer for .NET
=======================

Features
---------

- Flattens object hierarchies
- Only includes members with DataMemberAttribute if the containing type has DataContractAttribute
- Supports anonymous types


Usage
-------

  using (var stream = File.Create("example.csv"))
  {
    Cameronism.Csv.Serializer.Serialize(stream, someIEnumerable);
  }