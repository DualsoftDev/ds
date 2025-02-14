namespace Dual.Common.Base.FS

open Newtonsoft.Json
open System.Text
open System.Xml
open Newtonsoft.Json.Converters
open System
open System.Reflection

/// 기존 Newtonsoft.Json.JsonConverterAttribute 와 충돌 방지
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
[<AllowNullLiteral>]
type CustomJsonConverterAttribute(converterTypeName: string) =
    inherit Attribute()
    member val ConverterTypeName = converterTypeName with get

type EmJson =
    static let collectConverters () =
        // 현재 로드된 어셈블리에서 CustomJsonConverterAttribute가 있는 모든 클래스 검색
        let assemblies = AppDomain.CurrentDomain.GetAssemblies()
        let converterTypes =
            assemblies
            |> Seq.collect (fun asm -> asm.GetTypes())
            |> Seq.choose (fun t ->
                let attr = t.GetCustomAttribute<CustomJsonConverterAttribute>()
                if attr <> null then
                    let converterType = Type.GetType(attr.ConverterTypeName)
                    if converterType <> null then Some converterType else None
                else None)
            |> Seq.distinct
            |> Seq.toList

        // 변환기 인스턴스 생성
        converterTypes
        |> List.map (fun t -> Activator.CreateInstance(t) :?> JsonConverter)


    static let formatting = Newtonsoft.Json.Formatting.Indented
    static let defaultSettings =
        JsonSerializerSettings(
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            // [<JsonConstructor>] 로 marking 된 default constructor 가 private 인 경우에도 생성자 호출을 허용
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        )
        //|> tee (fun settings ->
        //    // 자동 수집된 컨버터 추가
        //    let converters = collectConverters()
        //    for c in converters do
        //        settings.Converters.Add(c)
        //)
    static member val DefaultSettings = defaultSettings

    static member ToJson(obj:obj):string = JsonConvert.SerializeObject(obj, formatting, defaultSettings)
    static member ToJson(obj:obj, settings:JsonSerializerSettings):string = JsonConvert.SerializeObject(obj, formatting, settings)
    static member FromJson<'t>(json:string) = JsonConvert.DeserializeObject<'t>(json, defaultSettings)
    static member FromJson<'t>(json:string, settings:JsonSerializerSettings) = JsonConvert.DeserializeObject<'t>(json, settings)


    static member ToXml(obj:obj, rootName:string):string =
        let json = EmJson.ToJson(obj)
        let xml = EmJson.JsonToXmlDocument(json, rootName)
        xml.OuterXml
    static member ToXml(obj:obj) = EmJson.ToXml(obj, "Root")

    /// src 객체를 Newtonsoft.Json serialize 를 이용해서 복사후 반환
    static member Duplicate<'t>(src:'t, settings:JsonSerializerSettings): 't =
        let json = EmJson.ToJson(src, settings)
        EmJson.FromJson<'t>(json, settings)

    /// src 객체를 Newtonsoft.Json serialize 를 이용해서 복사후 반환
    static member Duplicate<'t>(src:'t): 't =
        let settings = JsonSerializerSettings(TypeNameHandling = TypeNameHandling.Auto)
        EmJson.Duplicate(src, settings)

    static member Compare(obj1, obj2):int = JsonConvert.SerializeObject(obj1).CompareTo(JsonConvert.SerializeObject(obj2))
    static member Compare(obj1, obj2, settings:JsonSerializerSettings):int = JsonConvert.SerializeObject(obj1, settings).CompareTo(JsonConvert.SerializeObject(obj2, settings))
    static member IsEqual(obj1, obj2):bool = EmJson.Compare(obj1, obj2) = 0

    /// <summary>
    /// Check whether a given object is serializable or not.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <returns>True if serializable, otherwise false</returns>
    static member IsSerializable (obj: obj) : bool =
        try
            // JSON 직렬화 시도
            let jsonString = JsonConvert.SerializeObject(obj)
            let bytes = Encoding.UTF8.GetBytes(jsonString)
            true
        with
        | ex ->
            // 오류 메시지 출력
            System.Console.WriteLine($"Your object cannot be serialized. The reason is: {ex}")
            false


    /// JSON 문자열로부터 XmlDocuent 생성해서 반환
    static member JsonToXmlDocument(json:string, rootName:string): XmlDocument =
        // XmlNodeConverter 설정
        let xmlConverter = XmlNodeConverter()
        xmlConverter.DeserializeRootElementName <- rootName
        xmlConverter.WriteArrayAttribute <- true // 배열 처리를 명시적으로 설정

        // JsonSerializer 생성
        let serializer = JsonSerializer()
        serializer.Converters.Add(xmlConverter)

        // JSON -> XmlDocument 변환
        use stringReader = new System.IO.StringReader(json)
        use jsonReader = new JsonTextReader(stringReader)
        let xmlDoc = serializer.Deserialize<XmlDocument>(jsonReader)
        xmlDoc

    /// JSON 문자열로부터 Xml 문자열 생성해서 반환
    static member JsonToXml(json:string, rootName:string): string = EmJson.JsonToXmlDocument(json, rootName).OuterXml
