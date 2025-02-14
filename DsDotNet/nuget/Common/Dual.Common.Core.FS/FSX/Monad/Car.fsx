open System

type Wheel =
| Inch16
| Inch18

type Engine =
| Electric
| Gas of numberOfCylinders: int

type Doors =
| Base
| Electric
type Performance =
| Standard
| Sport
| Track


type Car = {
    Engine: Engine
    Wheels: Wheel
    Doors : Doors
    AC : bool
    Performance : Performance
    //Memo: string list
}

module Car =
    let baseCar = {
        Engine = Gas 4
        Wheels = Inch16
        Doors = Base
        AC = false
        Performance = Standard
        //Memo = []
    }

type CarBuilder(engine:Engine, wheel:Wheel) =
    new () = CarBuilder(Gas 4, Inch16)
    member _.Zero x =
        printfn $"Zero: x: {x}"
        { Car.baseCar with Engine = engine; Wheels = wheel }

    member _.Yield _ = Car.baseCar
    //member _.Yield x =
    //    printfn $"Yield: x: {x}"
    //    { Car.baseCar with Memo = [x]}

    [<CustomOperation("wheels")>]
    member _.Wheels(car, wheels) = { car with Wheels = wheels}

    [<CustomOperation("engine")>]
    member _.Engine(car, engine) = { car with Engine = engine}

    [<CustomOperation("doors")>]
    member _.Doors(car, doors) = { car with Doors = doors}

    [<CustomOperation("performance")>]
    member _.Performance(car, performance) = { car with Performance = performance}

    [<CustomOperation("ac")>]
    member _.AC(car) = { car with AC = true }

    member _.Delay(f) =
        printfn $"Starting delay"
        let result = f()
        printfn $"Ending delay: {result}"
        result

    member _.Combine(currentValueFromYield: 'a list, accumulatorFromDelay) =
        printfn $"Combine: currentValueFromYield: {currentValueFromYield}"
        printfn $"Combine: accumulatorFromDelay: {accumulatorFromDelay}"
        currentValueFromYield @ accumulatorFromDelay

    //member _.Run(valueOfLastDelay) =
    //    valueOfLastDelay |> List.toArray


let car (engine:Engine, wheel:Wheel) = CarBuilder(engine, wheel)
let car0 = CarBuilder()

car0 {
    wheels Inch18
    performance Track
    ac
}

car (Gas 16, Inch18) {()}


//car (Gas 16, Inch18) {
//    yield "aa"
//    yield "bb"
//}
