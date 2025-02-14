[<AutoOpen>]
module TestSampleClasses

[<AllowNullLiteral>]
type Person(name:string, age:int) =
    member x.Name = name
    member x.Age = age

type Student(name:string, age:int) =
    inherit Person(name, age)

type Teacher(name:string, age:int) =
    inherit Person(name, age)

let p1:Person = Teacher("teacher", 32)
let p2:Person = Student("student", 32)

let sp1 = Some p1
let sp2 = Some p2
