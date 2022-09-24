// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Runtime.CompilerServices
open System
open System.Globalization

[<AutoOpen>]
module TextUtil = 
 
    [<Extension>]
    type NameUtil =
        /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
        /// 적법하지 않으면 double quote 로 감싸주어야 한다.
        [<Extension>] static member IsValidIdentifier (identifier:string) = 
                        let isHangul(ch:char) = Char.GetUnicodeCategory(ch) = UnicodeCategory.OtherLetter;
                        let isValidStart(ch:char) = ch = '_' || Char.IsLetter(ch) || isHangul(ch);
                        let isValid(ch:char) = isValidStart(ch) || Char.IsDigit(ch);
                        if (identifier = null || identifier = "")
                        then failwithf "Name ArgumentNullException"
                        else 
                            if (identifier = "_") then true
                            else
                                 let chars = identifier.ToCharArray()
                                 let okHead = chars |> Seq.head |> isValidStart
                                 let okTail = chars |> Seq.tail 
                                                    |> Seq.filter(fun char -> isValid(char)|>not) 
                                                    |> Seq.isEmpty

                                 okHead && okTail

        [<Extension>] static member IsQuotationRequired (identifier:string) = 
                       NameUtil.IsValidIdentifier(identifier)|>not
        [<Extension>] static member GetValidName (identifier:string) = 
                       if(NameUtil.IsValidIdentifier(identifier))
                       then identifier else $"\"{identifier}\""



                       
        [<Extension>] static member Combine    (nameComponents:string seq)  = 
                        nameComponents |> Seq.map (fun name -> if NameUtil.IsQuotationRequired(name) then $"\"{name}\"" else name)
                                       |> String.concat "."
    
        