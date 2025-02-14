namespace Dual.Common.Base.FS


open System
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Collections.Generic

module SampleDataTypes =
    type PersonRecord = {
        Name: string
        Age: int
    }

    [<AllowNullLiteral>]
    type Person(name:string, age:int) =
        member x.Name = name
        member x.Age = age

    type Student(name:string, age:int) =
        inherit Person(name, age)

    type Teacher(name:string, age:int) =
        inherit Person(name, age)

    /// F# Discriminated Union
    type GenderDU =
        | Male
        | Female
        | Other

    type GenderEnum =
        | Male = 0
        | Female = 1
        | Other = 2

    type Human =
        | DuStudent of Student
        | DuTeacher of Teacher
        static member CreateStudent(name, age) = DuStudent (new Student(name, age))
        static member CreateTeacher(name, age) = DuTeacher (new Teacher(name, age))

