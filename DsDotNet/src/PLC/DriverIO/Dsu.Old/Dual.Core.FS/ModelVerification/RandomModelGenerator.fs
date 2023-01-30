namespace Dual.Core.ModelVerrification

open System
open FSharpPlus
open QuickGraph
open Dual.Common
open Dual.Core
open Dual.Core.QGraph

module RandomModelGenerator =
    /// Type of the tasks
    type TaskType =
        | Root = 0   // Root node
        | Active = 1 // Include a single sync directed acyclic graph
        | System = 2 // Include multiple async directed acyclic graph
        | Device = 3 // Task from another system, cannot be described
        | Reference = 4 // Reference task

    /// Relation type between task nodes
    type EdgeType = 
        | Sync = 0
        | Async = 1
        | Inter = 2

    /// The class to hold task attributes
    type TaskNode (addr:string, parent:string, depth:int, index:int) =
        member val Addr = addr with get, set // Address of a task to be used as the key of hashmap
        member val Parent = parent with get, set // Address of Parent node
        member val Depth = depth with get, set // Depth of this node
        member val Index = index with get, set // Serial number of this node
        
        // Type of tasks
        member val NodeType:TaskType = TaskType.Active with get, set
        
        // Reset edge
        member val Reset:List<string> = List.Empty with get, set
        // Ports
        member val EdgeIn:List<Tuple<EdgeType, string>> = List.Empty with get, set
        member val EdgeOut:List<Tuple<EdgeType, string>> = List.Empty with get, set

        // Reference as
        //member val Refs = string with get, set

        // Number of system
        member val SystemIndex = 0 with get, set

        // Expressions
        //member val InExp = string with get, set
        //member val OutExp = string with get, set
        //member val ResetExp = string with get, set

        // Another nodes from same Parent
        member val Sibling:List<string> = List.Empty with get, set    
        // Child nodes
        member val Children:List<string> = List.Empty with get, set

        // Basic Status : Start, Reset, Ready, Going, Finish, Home
        // NORMAL and DEVICE task can have only a single Status list
        // SYSTEM task can have multiple Status list
        member val Status:List<bool> = List.Empty with get, set
  
    /// 랜덤 모델 생성 결과
    type RandomModelResult = {
        Edges: List<string * List<EdgeType * string * string>>
        Resets: List<string * string>
    }
  
    /// To generate color logs
    let consoleColor (fc : ConsoleColor) = 
        let current = Console.ForegroundColor
        Console.ForegroundColor <- fc
        {
            new IDisposable with
                member x.Dispose() = 
                    Console.ForegroundColor <- current 
        }

    /// Color log without line change
    let cprintf color str = 
        Printf.kprintf (fun s -> use c = consoleColor color in printf "%s" s) str

    /// Color log with line change
    let cprintfn color str = 
        Printf.kprintf (fun s -> use c = consoleColor color in printfn "%s" s) str

    /// Color generator function for colorful logs
    let generateColor taskType =
        match taskType with
        | TaskType.Root -> ConsoleColor.DarkYellow
        | TaskType.Active -> ConsoleColor.Green
        | TaskType.Device -> ConsoleColor.DarkRed
        | TaskType.Reference -> ConsoleColor.DarkMagenta
        | _ -> ConsoleColor.Cyan

    /// Log printing function
    let printLog (targetDepth:int) (numTask:int) (inputTask:TaskNode) =
        let color = generateColor inputTask.NodeType
        printfn "\nparent addr : %s" inputTask.Parent
        printfn "now node addr : %s" inputTask.Addr
        printf "now index : %d / now task type : " inputTask.Index
        cprintfn color "%A" inputTask.NodeType
        printfn "now depth : %d / target dpeth : %d" inputTask.Depth targetDepth
        printfn "number of children : %d" numTask
        printfn "status of task : %A" inputTask.Status
        
        if inputTask.Sibling.any() then
            printfn "siblings : %A" inputTask.Sibling
        if inputTask.EdgeIn.any() then
            printfn "sources : %A" inputTask.EdgeIn
        if inputTask.Reset.any() then
            printfn "reset : %A" inputTask.Reset
            
    // Roulette wheel for random ratio
    let makeRouletteArea (selection:Tuple<int, int>) (count:int) = 
        let (enumType, area) = selection
        let st = count - area
        let ed = count - 1
        let rouletteArea = [for idx in st .. ed do (idx, enumType);]
        (rouletteArea, rouletteArea.Length)

    let genRouletteWheel (selections:seq<Tuple<int, int>>) =
        let mutable count = 0
        selections
        |> Seq.map(
            fun (et, num) -> 
                count <- count + num
                makeRouletteArea (et, num) count)
        |> Seq.collect(fun (roulett, cnt) -> roulett)
        |> Map.ofSeq

    let sourceInput (rnd:Random) (inputTask:TaskNode) (sourceMap:byref<Map<int, string>>) = 
        let sourceIdx = rnd.Next(1, inputTask.Index) - 1
        sourceMap <- sourceMap.Add(sourceIdx, inputTask.Sibling.[sourceIdx])

    let makeRandomEdges (rnd:Random) (inputTask:TaskNode) (isSystem:bool) (sourceMap:byref<Map<int, string>>) = 
        let numSource = rnd.Next(1, inputTask.Index)
        if isSystem then
            for idx in 1 .. numSource do
                if rnd.Next(3) <> 0 then
                    sourceInput rnd inputTask &sourceMap
        else
            while sourceMap.Count <> numSource do
                sourceInput rnd inputTask &sourceMap

    /// Random task ordering function
    /// NORMAL : Can generate only "a single sync order"
    /// SYSTEM : "Multiple async orders" could be generated
    let orderGenerator (rnd:Random) (inputTask:TaskNode) (taskMap:byref<Map<string, TaskNode>>) = 
        let ParentType = 
            match inputTask.Depth with
            | 0 -> TaskType.Root
            | 1 -> TaskType.System
            | _ -> taskMap.TryFind(inputTask.Parent).Value.NodeType

        // Generate a single sync graph
        let mutable sourceMap:Map<int, string> = Map.empty

        if inputTask.Index <> 1 && inputTask.NodeType <> TaskType.System then
            let mutable isSystem = false
            if taskMap.[inputTask.Parent].NodeType = TaskType.System then
                isSystem <- true

            makeRandomEdges rnd inputTask isSystem &sourceMap
        
        let edgeTypeGachaRatio = seq {
            (EdgeType.Sync.GetHashCode(), 8)
            (EdgeType.Inter.GetHashCode(), 2)
        }

        sourceMap
        |> Seq.map (fun x ->
            let edgeType = 
                match ParentType with
                | TaskType.System -> EdgeType.Async
                | _ -> enum<EdgeType>((genRouletteWheel edgeTypeGachaRatio).TryFind(rnd.Next 10).Value)
            (edgeType, x.Value))
        |> Seq.toList

    /// Reset generation function
    let resetGenerator (rnd:Random) (inputTask:TaskNode) = 
        if inputTask.Status.[2] = true then
            if inputTask.Index = inputTask.Sibling.Length + 1 then 
                [inputTask.Addr]
            else
                [inputTask.Sibling.[rnd.Next(inputTask.Index, inputTask.Sibling.Length + 1) - 1]]
        else
            [inputTask.Sibling.[rnd.Next(0, inputTask.Index - 1)]]

    /// Random task tree generator
    let rec randomGenerator (multipleSystem:bool) (targetDepth:int) (inputTask:TaskNode) (taskMap:byref<Map<string, TaskNode>>) =
        // Make the logical basis to generate the random node
        let rnd = Random()
        // Check the root node to decide the random range
        let startNumber = 
            match inputTask.Depth with
            | 0 | 1 -> 2
            | _ -> 0
        // Random range 0 ~ 6 if now node is not the root node
        let preGeneratedNumber = rnd.Next (startNumber, 6)
        
        let objectTypeGachaRatio = seq {
            (TaskType.Active.GetHashCode(), 7)
            (TaskType.Device.GetHashCode(), 3)
            (TaskType.Reference.GetHashCode(), 1) // for reference task
        }
        // Assign system index into the task nodes
        inputTask.SystemIndex <-
            match inputTask.Depth with
            | 0 -> 0
            | 1 -> inputTask.Index
            | _ -> taskMap.[inputTask.Parent].SystemIndex
   
        // To randomly generate reference task
        let sysIdxChecker = 0
            //if inputTask.Depth > 1 && inputTask.SystemIndex > 1 then 1
            //else 0

        // Task type generation
        inputTask.NodeType <- 
            match inputTask.Depth with
            | 0 -> TaskType.Root
            | 1 -> TaskType.System
            | _ -> enum<TaskType>((genRouletteWheel objectTypeGachaRatio).TryFind(rnd.Next (10 + sysIdxChecker)).Value)
                
        // Decide the number of Children
        // One-child is not allowed
        let numTask =
            match multipleSystem, inputTask.Depth with
            | false, 0 -> 1
            | _, _ -> 
                match inputTask.NodeType, preGeneratedNumber with
                | TaskType.Device, _ | TaskType.Reference, _ -> 0
                | _, 1 -> preGeneratedNumber + 1 
                | _, _ -> preGeneratedNumber

        // Generate order graph
        inputTask.EdgeIn <- orderGenerator rnd inputTask &taskMap

        // Put the out edge into the source node
        for (eType, source) in inputTask.EdgeIn do
            taskMap.[source].EdgeOut <- List.append taskMap.[source].EdgeOut [(eType, inputTask.Addr)]

        // Generate initial Status
        let randomStatus = 
            if inputTask.Depth <> 0 && taskMap.[inputTask.Parent].NodeType = TaskType.System then 0
            elif inputTask.EdgeIn.IsEmpty <> true then 2 * rnd.Next(2) 
            else 0

        inputTask.Status <-
            [0 .. 5] |> List.map ((=) (2 + randomStatus))

        // Getting ordered graph Reset
        if inputTask.Depth > 0  && 
            inputTask.NodeType <> TaskType.System  &&
            taskMap.[inputTask.Parent].NodeType <> TaskType.System then
            inputTask.Reset <- resetGenerator rnd inputTask

        // Printing node generation result
        printLog targetDepth numTask inputTask
        
        taskMap <- taskMap.Add(inputTask.Addr, inputTask)

        // Create random number of child nodes
        if inputTask.Depth <> targetDepth && numTask <> 0 then
            inputTask.Children <- 
                [1 .. numTask] |> List.map (sprintf "%s_%d" inputTask.Addr)

            let newChildren = 
                inputTask.Children
                |> List.indexed
                |> List.map(fun (idx, c) -> TaskNode(c, inputTask.Addr, (inputTask.Depth + 1), idx + 1))

            for childTask in newChildren do
                childTask.Sibling <- 
                    inputTask.Children
                    |> List.filter ((<>) childTask.Addr)

                randomGenerator multipleSystem targetDepth childTask &taskMap
        else
            cprintfn ConsoleColor.DarkRed " - End of branch"
            
    /// Post processing to remove reset edges from another branches
    let rec sideChecker (node:string) (edges:List<Tuple<EdgeType, string>>) (isReverse:bool) (taskMap:byref<Map<string, TaskNode>>) =
        let mutable checker = 0
        for (_, target) in edges do
            if target <> node then
                let nextExges = 
                    match isReverse with
                    | true -> taskMap.[target].EdgeIn
                    | _ -> taskMap.[target].EdgeOut
                checker <- sideChecker node nextExges isReverse &taskMap
            else
                checker <- 1

        checker

    let checkResetSourceIsInSameBranch (node:string) (taskMap:byref<Map<string, TaskNode>>) = 
        let mutable res:List<int> = List.Empty
        for resetSource in taskMap.[node].Reset do
            let leftFind = sideChecker node taskMap.[resetSource].EdgeIn true &taskMap
            let rightFind = sideChecker node taskMap.[resetSource].EdgeOut false &taskMap

            res <- List.append res [leftFind + rightFind]

        res

    let postProcess (taskMap:byref<Map<string, TaskNode>>) =
        for node in taskMap do
            if node.Value.Depth > 2 then // Check the node is deeper then system level
                let checkedList = checkResetSourceIsInSameBranch node.Value.Addr &taskMap
                if checkedList.any() then
                    let newReset = 
                        node.Value.Reset |> List.zip checkedList
                        |> Seq.where(fun (i, e) -> i = 1)
                        |> Seq.map(fun (_, e) -> e)
                        |> Seq.toList
                    node.Value.Reset <- newReset
                    
    let executor multipleSystem targetDepth = 
        printfn "Random model generator"

        // Setup root node
        let rootNode = TaskNode("Root", "", 0, 1)

        // Dictionary of randomly generated tasks (address : task node)
        let mutable taskMap:Map<string, TaskNode> = Map.empty

        // The entry point of the random model generating sequence
        randomGenerator multipleSystem targetDepth rootNode &taskMap
        postProcess &taskMap
    
        // Checking the generation result
        printfn "\n\nModel generation result\n"
        taskMap
        |> Seq.iter(fun x ->
            let TaskType = x.Value.NodeType
            let valColor = generateColor TaskType
            let keyColor = if x.Value.Status.[2] = true then ConsoleColor.DarkGray else ConsoleColor.White
            cprintf keyColor "%s : " x.Key
            cprintf valColor "%A" TaskType
            cprintf ConsoleColor.Yellow "  Sources : "
            printf "%A" x.Value.EdgeIn
            cprintf ConsoleColor.DarkGreen "  Targets : "
            printf "%A" x.Value.EdgeOut
            cprintf ConsoleColor.DarkCyan "  Reset : "
            printfn "%A" x.Value.Reset)

    let executeAndMakeEdgeList multipleSystem targetDepth =
        // Setup root node
        let rootNode = TaskNode("Root", "", 0, 1)

        // Dictionary of randomly generated tasks (address : task node)
        let mutable taskMap:Map<string, TaskNode> = Map.empty

        // The entry point of the random model generating sequence
        randomGenerator multipleSystem targetDepth rootNode &taskMap
        postProcess &taskMap
    
        let edges =
            [
                for task in taskMap do
                    let grandChildren = [
                        for childNode in task.Value.Children do
                            for (taskType, source) in taskMap.[childNode].EdgeIn do
                                yield (taskType, source, taskMap.[childNode].Addr)
                    ]
                    yield task.Key, grandChildren
            ]

        let resets = 
            [
                for task in taskMap do
                    for r in task.Value.Reset do
                        yield r, task.Value.Addr
            ]

        { Edges = edges; Resets = resets }

    /// 랜덤 생성 모델 -> QgModel
    let convertRandomModel randomModel =
        let tupleToList t =
            if Reflection.FSharpType.IsTuple(t.GetType()) 
                then Some (Reflection.FSharpValue.GetTupleFields t |> Array.toList)
                else None

        /// 랜덤 모델 edges
        let edges = randomModel.Edges
        /// 랜덤 모델 resets
        let resets = randomModel.Resets |> Seq.map(fun (s, t) -> t, s) |> dict

        /// vertex reset 설정
        /// 
        let resetvertices (vs:QgVertex list) =
            vs 
            |> List.map(fun v -> 
                if resets.ContainsKey(v.Name) then
                    monad{
                        let! source = vs |> List.tryFind(fun ov -> ov.Name = resets.[v.Name]) 
                        v.Reset <- ns(source)
                    } |> ignore

                v
            ) 

        /// 랜덤 모델 edges에서 QgVertes 생성
        let vertices = 
            edges 
            |> List.map(fst >> QgVertex) 
            |> resetvertices

        let getVertex name =
            vertices |> List.tryFind(fun v -> v.Name = name)

        /// 랜덤모델 Edge -> QgEdge
        let toQgEdge (edges:List<EdgeType * string * string>) = 
            edges 
            |> List.map(fun (_, s, t) -> 
                monad {
                    let! dest   = getVertex t
                    let! source = getVertex s
                    QgEdge(source, dest) :> IEdge
                } 
            ) 
            |> List.choose id

        /// 입력 받은 타입별 QgEdge 분류
        let edgeByType (edges:List<string * List<EdgeType * string * string>>) (selectType) =  
            edges 
            |> List.where(snd >> List.any) 
            |> List.map(fun (name, e) -> 
                let e' = e |> List.filter(fun (ty, s, t) -> ty = selectType) |> toQgEdge
                name, e'
            )

        /// Vertex에 child vertices와 edges 입력
        let setChildren =
            edges
            |> List.map(fun (n, e) -> 
                let pv = getVertex n
                let cv = 
                    e 
                    |> List.map(fun (_, s, t) -> tupleToList (s, t))
                    |> List.choose id 
                    |> List.collect(fun s -> s) 
                    |> List.distinct 
                    |> List.cast<string> 
                    |> List.map(fun s -> getVertex s)
                    |> List.choose id 
                    |> Seq.ofList
                    |> Seq.cast<IVertex>
                let ce = e |> List.filter(fun (ty, s, t) -> ty = EdgeType.Sync) |> toQgEdge |> Seq.ofList |> Seq.cast<IEdge>
                
                monad{
                    let! parent = pv
                    parent.DAGs.Add(QgDAG(cv, ce))
                    parent
                }
            )
            |> List.choose id
        
        /// Graph -> QgModel
        let makeSimpleModel g =
            let dcg = makeDCG g []
            { createDefaultModel() with DAG = g; DCG = dcg; }

        let gs = setChildren |> List.map(fun v -> v.Edges |> Option.bind(fun e -> Some (v.Name, e.ToAdjacencyGraph()))) |> List.choose id |> List.head
        let model =  
            let name = fst gs 
            let graph = snd gs
            let reset name = 
                match resets.ContainsKey(name) with
                | true -> Some resets.[name]
                | false -> None
            { makeSimpleModel graph with Reset = Some "reset"; Title = Some name }


        model
