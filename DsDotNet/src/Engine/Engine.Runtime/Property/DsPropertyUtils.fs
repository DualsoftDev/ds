namespace Engine.Runtime

open System
open Microsoft.FSharp.Core
open System.Collections
open System.ComponentModel

[<AutoOpen>]
module NullableUtils =

    let toNullable (opt: 'T option) : Nullable<'T> =
        match opt with
        | Some value -> Nullable(value)
        | None -> Nullable()

    let toNull (opt: string option) : string =
        match opt with
        | Some value -> value
        | None -> null

    let fromNullable (nullable: Nullable<'T>) : 'T option =
        if nullable.HasValue then Some nullable.Value else None

    let fromNull (str: string) : string option =
        if str <> null then Some str else None

[<AutoOpen>]
module PropertyUtils =
    // 제네릭 컬렉션 항목을 확장 가능하게 만드는 컨버터
    type ExpandableCollectionConverter() =
        inherit CollectionConverter()

        override this.GetPropertiesSupported(_context) = true

        override this.GetProperties(context, value, attributes) =
            match value with
            | :? IList as list when list.Count > 0 ->
                let properties = 
                    [| for i in 0 .. list.Count - 1 -> ExpandableItemPropertyDescriptor(list, i) :> PropertyDescriptor |]
                PropertyDescriptorCollection(properties)
            | _ -> base.GetProperties(context, value, attributes)

    // 제네릭 개별 항목을 확장 가능하게 만드는 PropertyDescriptor
    and ExpandableItemPropertyDescriptor(list: IList, index: int) =
        inherit PropertyDescriptor(sprintf "Item[%d]" index, null)

        override this.CanResetValue(_comp) = false
        override this.ComponentType = list.GetType()
        override this.GetValue(_comp) = list.[index]
        override this.IsReadOnly = false
        override this.PropertyType = list.[index].GetType()
        override this.ResetValue(_comp) = ()
        override this.SetValue(_comp, value) = list.[index] <- value
        override this.ShouldSerializeValue(_comp) = true
        
   
    // 제네릭 ObservableBindingList: 컬렉션 변경을 감지하여 이벤트 발생
    [<TypeConverter(typeof<ExpandableCollectionConverter>)>]
    type ExpandableBindingList<'T>()  =
        inherit BindingList<'T>()

