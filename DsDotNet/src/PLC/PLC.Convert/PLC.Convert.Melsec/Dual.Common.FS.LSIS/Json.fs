namespace Dual.Common.FS.LSIS
open Newtonsoft.Json
open System.IO

module JsonM =
    //// https://stackoverflow.com/questions/5780888/casting-interfaces-for-deserialization-in-json-net
    //type ConcreteTypeConverter<'TConcrete>() =
    //    inherit JsonConverter()
    //    override _.ReadJson(reader, objectType, existingValue, serializer) =
    //        serializer.Deserialize<'TConcrete>(reader);
    //    /// assume we can convert to anything for now
    //    override _.CanConvert(_) = true
    //    override _.WriteJson(writer, value, serializer) =
    //        //use the default serialization - it works fine
    //        serializer.Serialize(writer, value)


    let private jsonSerializeSettings =
        new JsonSerializerSettings(
            TypeNameHandling = TypeNameHandling.All,
            // Newtonsoft.Json.JsonSerializationException: Self referencing loop detected for property ....
            // https://stackoverflow.com/questions/13510204/json-net-self-referencing-loop-detected
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            //NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.Indented)


    // Newtonsoft.Json.JsonSerializeationException: 
    // 'Cannot preserve reference to array or readonly lists, or list created from a non-default constructor: System.Tuple`2[System.String, System.Object][].  Path'[0].Properties.$values'
    // https://stackoverflow.com/questions/30349695/could-not-create-an-instance-of-type-x-type-is-an-interface-or-abstract-class-a

    let serialize obj = JsonConvert.SerializeObject(obj, jsonSerializeSettings)
    let deserialize<'t> json = JsonConvert.DeserializeObject<'t>(json, jsonSerializeSettings)
    