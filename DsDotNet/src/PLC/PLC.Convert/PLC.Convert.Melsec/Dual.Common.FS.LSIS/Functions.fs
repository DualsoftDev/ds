namespace Dual.Common.FS.LSIS


open System
open System.IO
open System.Threading
open System.Net
open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module Functions =
    /// 범용 string
    let toString obj = obj.ToString()

    let toDictionary pr = pr |> dict |> Dictionary

    /// Returns tuple of function execution result and duration
    let duration f =
        let timer = new System.Diagnostics.Stopwatch()
        //let timer = new HighResolutionTimer()
        timer.Start()

        let result = f()
        timer.Stop()
        (result, timer.ElapsedMilliseconds)

    /// Executes f twice
    let twice f = f >> f

    /// Function composition
    let compose f g = f >> g

    /// Function(s) composition : http://www.fssnip.net/S/title/Composing-a-list-of-functions
    let composeFunctions fs = Seq.reduce (>>) fs
    // e.g composeFunctions [(*) 2; (+) 7; (*) 3; (+) 3] 3

    /// repeat f n times
    let rec ntimes (n: int) f =
        if n = 0 then (fun x -> x)
        else
            let g = ntimes (n-1) f
            compose f g


    /// Returns function which get successor
    let successor = fun x -> x + 1

    /// Returns function which get predecessor
    let predecessor = fun x -> x - 1


    /// Y-combinator, or Sage bird
    let rec Y f x = f (Y f) x

    let dos2unix (str:string) =
        str.Replace("\r\n", "\n")


    let tee f x =
        f x |> ignore
        x

    let escapeQuote(s:string) = s.Replace("'", @"\'")
    let singleQuote(s:string) = sprintf "'%s'" s
    let doubleQuote(s:string) = sprintf "\"%s\"" s




    /// <summary>
    /// Awaits dotnet Task, asynchronosely
    /// </summary>
    /// <param name="taskf">Task<'a> generating function.  Not Task<'a> itself.</param>
    let awaitDotNetTask taskf =
        async {
            return! Async.AwaitTask (taskf())
        }

    /// Waits dotnet Task, synchornosly
    let waitDotNetTask taskf =
        awaitDotNetTask taskf |> Async.RunSynchronously



    /// 주어진 action f 를 주어진 시간 limitMilli 내에 수행.  실패하면 exception raise
    /// C# 구현은 ToolsDateTime.ExecuteWithTimeLimit 참고
    let withTimeLimit f (limitMilli:int) (description:string) =
        let cts = new CancellationTokenSource();
        cts.CancelAfter(limitMilli);
        let task = Async.StartAsTask(async{return f()}, cancellationToken=cts.Token)
        if not (task.Wait(limitMilli)) then
            failwithlogf "Timeout(%d ms) expired on %s." limitMilli description

        task.Result




    /// <summary>
    /// General type casting : http://stackoverflow.com/questions/18928268/f-numeric-type-casting
    /// e.g
    ///     cast<int> (box 1.1) -- converts object(->float) to int
    ///     cast<int> 1.23 -- converts float to int
    ///     cast<int> "123" -- converts string to int
    ///     cast<int> "1.23" -- crash!!!
    ///     cast<float> "1.23" |> cast<int> -- converts string -> float -> int
    /// </summary>
    /// <param name="typecast"></param>
    /// <param name="x"></param>
    let cast<'a> input = System.Convert.ChangeType(input, typeof<'a>) :?> 'a

    /// <summary>
    /// type casting : http://stackoverflow.com/questions/18928268/f-numeric-type-casting
    /// e.g
    ///     tryCast<int> (box 1.1) -- Some(1)
    ///     tryCast<int> 1.23 -- Some(1)
    ///     tryCast<int> "123" -- Some(123)
    ///     tryCast<int> "1.23" -- None
    /// </summary>
    /// <param name="typecast"></param>
    /// <param name="x"></param>
    let tryCast<'a> input =
      try Some(cast<'a> input)
      with _ -> None




    let tracef  fmt = Printf.kprintf Trace.Write fmt
    let tracefn fmt = Printf.kprintf Trace.WriteLine fmt

    let private cprintfWith endl c fmt =
        // http://stackoverflow.com/questions/27004355/wrapping-printf-and-still-take-parameters
        // https://blogs.msdn.microsoft.com/chrsmith/2008/10/01/f-zen-colored-printf/
        Printf.kprintf
            (fun s ->
                let old = System.Console.ForegroundColor
                try
                    System.Console.ForegroundColor <- c;
                    System.Console.Write (s + endl)
                finally
                    System.Console.ForegroundColor <- old)
            fmt

    let cprintf c fmt = cprintfWith "" c fmt
    let cprintfn c fmt = cprintfWith "\n" c fmt


    let printfBlack fmt = cprintf ConsoleColor.Black fmt
    let printfnBlack fmt = cprintfn ConsoleColor.Black fmt
    let printfDarkBlue fmt = cprintf ConsoleColor.DarkBlue fmt
    let printfnDarkBlue fmt = cprintfn ConsoleColor.DarkBlue fmt
    let printfDarkGreen fmt = cprintf ConsoleColor.DarkGreen fmt
    let printfnDarkGreen fmt = cprintfn ConsoleColor.DarkGreen fmt
    let printfDarkCyan fmt = cprintf ConsoleColor.DarkCyan fmt
    let printfnDarkCyan fmt = cprintfn ConsoleColor.DarkCyan fmt
    let printfDarkRed fmt = cprintf ConsoleColor.DarkRed fmt
    let printfnDarkRed fmt = cprintfn ConsoleColor.DarkRed fmt
    let printfDarkMagenta fmt = cprintf ConsoleColor.DarkMagenta fmt
    let printfnDarkMagenta fmt = cprintfn ConsoleColor.DarkMagenta fmt
    let printfDarkYellow fmt = cprintf ConsoleColor.DarkYellow fmt
    let printfnDarkYellow fmt = cprintfn ConsoleColor.DarkYellow fmt
    let printfGray fmt = cprintf ConsoleColor.Gray fmt
    let printfnGray fmt = cprintfn ConsoleColor.Gray fmt
    let printfDarkGray fmt = cprintf ConsoleColor.DarkGray fmt
    let printfnDarkGray fmt = cprintfn ConsoleColor.DarkGray fmt
    let printfBlue fmt = cprintf ConsoleColor.Blue fmt
    let printfnBlue fmt = cprintfn ConsoleColor.Blue fmt
    let printfGreen fmt = cprintf ConsoleColor.Green fmt
    let printfnGreen fmt = cprintfn ConsoleColor.Green fmt
    let printfCyan fmt = cprintf ConsoleColor.Cyan fmt
    let printfnCyan fmt = cprintfn ConsoleColor.Cyan fmt
    let printfRed fmt = cprintf ConsoleColor.Red fmt
    let printfnRed fmt = cprintfn ConsoleColor.Red fmt
    let printfMagenta fmt = cprintf ConsoleColor.Magenta fmt
    let printfnMagenta fmt = cprintfn ConsoleColor.Magenta fmt
    let printfYellow fmt = cprintf ConsoleColor.Yellow fmt
    let printfnYellow fmt = cprintfn ConsoleColor.Yellow fmt
    let printfWhite fmt = cprintf ConsoleColor.White fmt
    let printfnWhite fmt = cprintfn ConsoleColor.White fmt




    let consoleColorChanger(color) =
        let crBackup = System.Console.ForegroundColor
        System.Console.ForegroundColor <- color
        let disposable =
            { new IDisposable with
                member x.Dispose() = System.Console.ForegroundColor <- crBackup }
        disposable




    //let printfnd fmt = printfn fmt

    (*
     * http://stackoverflow.com/questions/11559440/how-to-manage-debug-printing-in-f
     * http://www.fssnip.net/M
     * Akka actor 와 함께 사용하면, release version 에서 메시지가 제대로 동작하지 않음.
     *)

    // this has the same type as printf, but it doesn't print anything
    let private fakePrintf fmt =
        fprintf System.IO.StreamWriter.Null fmt


    #if DEBUG
    let printfnd fmt =
        printfn fmt
    let printfd fmt =
        printf fmt
    #else
    let printfnd fmt =
        fakePrintf fmt
    let printfd fmt =
        fakePrintf fmt
    #endif

    let getIpAddresses() =
        Dns.GetHostAddresses(Dns.GetHostName())
            |> Seq.map(fun ip -> ip.ToString())
            |> Seq.filter(fun ip -> ip.Contains("."))

    let getIpAddress() : string =
        getIpAddresses() |> Seq.head

    let getIpAddressFromHostname(hostname:string) =
        let ipaddress =
            Dns.GetHostAddresses(hostname)
            |> Seq.find(fun ip -> ip.AddressFamily = Sockets.AddressFamily.InterNetwork)
        ipaddress.ToString()

    let ipAddressEqual (a:string) (b:string) =
        try
            match a, b with
            | null, _ | _, null | "", _ | _, "" -> false
            | a, b when a = b -> true
            | _ ->
                match IPAddress.TryParse(a), IPAddress.TryParse(b) with
                | (true, _), (true, _) -> failwithlog "Check it again!!!"
                | (true, _), (false, _) ->
                    a = getIpAddressFromHostname(b)
                | (false, _), (true, _) ->
                    b = getIpAddressFromHostname(a)
                | (false, _), (false, _) ->
                    getIpAddressFromHostname(a) = getIpAddressFromHostname(b)
        with exn ->
            false


    let inClosedRange (value:'a) ((min:'a), (max:'a)) =
        min <= value && value <= max

    //let min a b = if a < b then a else b
    //let max a b = if a > b then a else b


    let removeNewline msg:string =
        Text.RegularExpressions.Regex.Replace(msg, "[\\r\\n]*$", "")


    let clip n s e = min e (max n s)

    // https://stackoverflow.com/questions/42800373/f-pipe-forward-first-argument
    /// Argument 의 순서 치환
    /// [1; 2; 3] |> (flip List.append) [4; 5; 6]   ==> [1; 2; 3; 4; 5; 6]
    /// [1; 2; 3] |> List.append [4; 5; 6]   ==> [4; 5; 6; 1; 2; 3]
    let flip f x y = f y x

    // by Tomas Petricek
    // http://www.fssnip.net/2V/title/Dynamic-operator-using-Reflection
    // 또 다른 T.Petricek 의 자료 : http://www.fssnip.net/2U/title/Dynamic-operator-using-Dynamic-Language-Runtime
    open System.Reflection
    open Microsoft.FSharp.Reflection


    let (?) (o : obj) name : 'R =
        // Various flags that specify what members can be called
        // NOTE: Remove 'BindingFlags.NonPublic' if you want a version
        // that can call only public methods of classes
        let staticFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Static
        let instanceFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Instance
        let ctorFlags = instanceFlags
        let inline asMethodBase (a : #MethodBase) = a :> MethodBase
        // The return type is a function, which means that we want to invoke a method
        if FSharpType.IsFunction(typeof<'R>) then
            // Get arguments (from a tuple) and their types
            let argType, resType = FSharpType.GetFunctionElements(typeof<'R>)
            // Construct an F# function as the result (and cast it to the
            // expected function type specified by 'R)
            FSharpValue.MakeFunction(typeof<'R>,
                                     fun args ->
                                         // We treat elements of a tuple passed as argument as a list of arguments
                                         // When the 'o' object is 'System.Type', we call static methods
                                         let methods, instance, args =
                                             let args =
                                                 // If argument is unit, we treat it as no arguments,
                                                 // if it is not a tuple, we create singleton array,
                                                 // otherwise we get all elements of the tuple
                                                 if argType = typeof<unit> then [||]
                                                 elif not (FSharpType.IsTuple(argType)) then [| args |]
                                                 else FSharpValue.GetTupleFields(args)
                                             // Static member call (on value of type System.Type)?
                                             if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then
                                                 let methods =
                                                     (unbox<Type> o).GetMethods(staticFlags) |> Array.map asMethodBase
                                                 let ctors =
                                                     (unbox<Type> o).GetConstructors(ctorFlags) |> Array.map asMethodBase
                                                 Array.concat [ methods; ctors ], null, args
                                             else o.GetType().GetMethods(instanceFlags) |> Array.map asMethodBase, o, args

                                         // A simple overload resolution based on the name and the number of parameters only
                                         // TODO: This doesn't correctly handle multiple overloads with same parameter count
                                         let methods =
                                             [ for m in methods do
                                                   if m.Name = name && m.GetParameters().Length = args.Length then yield m ]

                                         // If we find suitable method or constructor to call, do it!
                                         match methods with
                                         | [] -> failwithf "No method '%s' with %d arguments found" name args.Length
                                         | _ :: _ :: _ ->
                                             failwithf "Multiple methods '%s' with %d arguments found" name args.Length
                                         | [ :? ConstructorInfo as c ] -> c.Invoke(args)
                                         | [ m ] -> m.Invoke(instance, args))
            |> unbox<'R>
        else
            // The result type is not an F# function, so we're getting a property
            // When the 'o' object is 'System.Type', we access static properties
            let typ, flags, instance =
                if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then unbox o, staticFlags, null
                else o.GetType(), instanceFlags, o

            // Find a property that we can call and get the value
            let prop = typ.GetProperty(name, flags)
            if prop = null && instance = null then
                // The syntax can be also used to access nested types of a type
                let nested = typ.Assembly.GetType(typ.FullName + "+" + name)
                // Return nested type if we found one
                if nested = null then failwithf "Property or nested type '%s' not found in '%s'." name typ.Name
                elif not ((typeof<'R>).IsAssignableFrom(typeof<System.Type>)) then
                    let rname = (typeof<'R>.Name)
                    failwithf "Cannot return nested type '%s' as a type '%s'." nested.Name rname
                else
                    nested
                    |> box
                    |> unbox<'R>
            else
                // Call property and return result if we found some
                let meth = prop.GetGetMethod(true)
                if prop = null then failwithf "Property '%s' found, but doesn't have 'get' method." name
                try
                    meth.Invoke(instance, [||]) |> unbox<'R>
                with _ -> failwithf "Failed to get value of '%s' property (of type '%s')" name typ.Name




    // test dynamic operator
    //
    //// Create type that provides access to some types
    //type Mscorlib private () =
    //    static let asm = Assembly.Load("mscorlib")
    //    static member Random = asm.GetType("System.Random")
    //    static member Console = asm.GetType("System.Console")
    //do
    //
    //    // Dynamically invoke constructor with seed=1
    //    let rnd : obj = Mscorlib.Random?``.ctor``(1)
    //    // Invoke method without argument
    //    let resf : float = rnd?NextDouble()
    //    // Invoke method with argument
    //    let resi : int = rnd?Next(10)
    //
    //    // Dynamically get value of a static property
    //    let bg : ConsoleColor = Mscorlib.Console?BackgroundColor
    //
    //
    //
    //
    //
    //    // Dynamically invoke 'Next' method of 'Random' type
    //    let o = box (new Random())
    //    let a : int = o?Next(10)
    //    ()


    #if INTERACTIVE
    /// http://www.fssnip.net/2U/title/Dynamic-operator-using-Dynamic-Language-Runtime
    /// see http://www.fssnip.net/2V/title/Dynamic-operator-using-Reflection also.
    /// Dynamic operator using Dynamic Language Runtime
    // Rreference C# implementation of dynamic operations
    #r "Microsoft.CSharp.dll"
    open System
    open System.Runtime.CompilerServices
    open Microsoft.CSharp.RuntimeBinder

    // Simple implementation of ? operator that works for instance
    // method calls that take a single argument and return some result
    let (?) (inst:obj) name (arg:'T) : 'R =
      // TODO: For efficient implementation, consider caching of call sites
      // Create dynamic call site for converting result to type 'R
      let convertSite =
        CallSite<Func<CallSite, Object, 'R>>.Create
          (Binder.Convert(CSharpBinderFlags.None, typeof<'R>, null))

      // Create call site for performing call to method with the given
      // name and a single parameter of type 'T
      let callSite =
        CallSite<Func<CallSite, Object, 'T, Object>>.Create
          (Binder.InvokeMember
            ( CSharpBinderFlags.None, name, null, null,
              [| CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
                 CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) |]))

      // Run the method call using second call site and then
      // convert the result to the specified type using first call site
      convertSite.Target.Invoke
        (convertSite, callSite.Target.Invoke(callSite, inst, arg))

    // Example of using the operator
    // Dynamically invoke 'Next' method of 'Random' type

    let o = box (new Random())
    let a : int = o?Next(10)
    #endif



    /// http://www.fssnip.net/s4/title/Reinventing-the-Reader-Monad
    /// Reinventing the Reader Monad
    /// Alternative solution to the problem in Scott Wlaschin's "Reinventing the Reader Monad" article (but without monads).
    /// Applies the specified function to all items in the list.
    /// If any of the function calls results in a Failure, the
    /// map function returns failure immediately.
    let mapOrFail f items =
        let rec loop acc items =
            match items with
            | x::xs ->
                match f x with
                | Some r -> loop (r::acc) xs
                | None -> None
            | [] -> Some(List.rev acc)
        loop [] items


    #if INTERACTIVE
    let getPurchaseInfo (custId:CustId)  : Result<ProductInfo list> =

        // Open api connection (I'm not calling 'Close' at the end
        // because this is should be done done automatically, as the
        // client implements IDisposable)
        use api = new ApiClient()
        api.Open()

        // Get product ids purchased by customer id
        let productIdsResult = api.Get<ProductId list> custId
        match productIdsResult with
        | Success productIds ->
            productIds |> mapOrFail (api.Get<ProductInfo>)
        | Failure err ->
            Failure err
    #endif



    // http://www.fssnip.net/rJ/title/Faster-operator-for-range-generation
    // ----  Faster .. operator for range generation
    open System.Collections.Generic

    /// A helper function that generates a sequence for the specified range.
    /// (This takes the step and also an operator to use for checking at the end)
    let inline rangeStepImpl (lo:^T) (hi:^T) (step:^T) geq =
      { new IEnumerable< ^T > with
          member x.GetEnumerator() =
            let current = ref (lo - step)
            { new IEnumerator< ^T > with
                member x.Current = current.Value
              interface System.Collections.IEnumerator with
                member x.Current = box current.Value
                member x.MoveNext() =
                  if geq current.Value hi then false
                  else current.Value <- current.Value + step; true
                member x.Reset() = current.Value <- lo - step
              interface System.IDisposable with
                member x.Dispose() = ()  }
        interface System.Collections.IEnumerable with
          member x.GetEnumerator() = (x :?> IEnumerable< ^T >).GetEnumerator() :> _ }

    /// A helper function that generates a sequence for the specified range of
    /// int or int64 values. This is notably faster than using `lo .. step .. hi`.
    let inline rangeStep (lo:^T) (step:^T) (hi:^T) =
      if lo <= hi then rangeStepImpl lo hi step (>=)
      else rangeStepImpl lo hi step (<=)

    /// A helper function that generates a sequence for the specified range of
    /// int or int64 values. This is notably faster than using `lo .. hi`.
    let inline range (lo:^T) (hi:^T) =
      if lo <= hi then rangeStepImpl lo hi LanguagePrimitives.GenericOne (>=)
      else rangeStepImpl lo hi LanguagePrimitives.GenericOne (<=)

    #if INTERACTIVE
    #time

    // The following takes about 350-400ms on my machine
    seq { for x in 0 .. 5000000 -> 0 } |> Seq.length
    seq { for x in 0L .. 5000000L -> 0 } |> Seq.length
    seq { for x in 0. .. 5000000. -> 0 } |> Seq.length
    seq { for x in 0 .. 10 .. 50000000 -> 0 } |> Seq.length
    seq { for x in 0L .. 10L .. 50000000L -> 0 } |> Seq.length
    seq { for x in 0. .. 10. .. 50000000. -> 0 } |> Seq.length

    // The following takes about 75ms on my machine
    seq { for x in range 0 5000000 -> 0 } |> Seq.length
    seq { for x in range 0L 5000000L -> 0 } |> Seq.length
    seq { for x in range 0. 5000000. -> 0 } |> Seq.length
    seq { for x in rangeStep 0 10 50000000 -> 0 } |> Seq.length
    seq { for x in rangeStep 0L 10L 50000000L -> 0 } |> Seq.length
    seq { for x in rangeStep 0. 10. 50000000. -> 0 } |> Seq.length

    // Oh look, I can override the default operators :)
    let inline (..) a b = range a b
    let inline (.. ..) a s b = rangeStep a s b
    #endif


    /// f 로 주어진 lambda 함수를 실행하고 그 결과가 answer 값과 같지 않거나 예외가 발생하면 errmsg 로그를 출력하고 fail.
    let execFunc<'T when 'T: equality> (f: unit->'T) (answer:'T) (errmsg:string) =
        try
            let code = f()
            if code <> answer then
                failwithlogf "%s: Incorrect return value: (%A != %A)" errmsg answer code
                None
            else
                Some code
        with exn ->
            failwithlogf "Exception: %s:\r\n%O" errmsg exn
            None

    /// f 로 주어진 lambda 함수를 실행하고 예외가 발생하면 errmsg 로그를 출력하고 fail.
    let execAction(f: unit->unit) (errmsg:string) =
        try
            f()
        with exn ->
            failwithlogf "Exception while %s:\r\n%O" errmsg exn



    // http://codebetter.com/matthewpodwysocki/2008/11/26/object-oriented-f-encapsulation-with-object-expressions/
    /// counterSample() 호출시마다 증가된  count 값을 반환
    let counterSample =
      let count = ref 0     // reference cell
      fun () -> incr count; !count



    /// 수식 계산 : http://www.fssnip.net/1D/title/Parsing-string-expressions-the-lazy-way
    /// DataTable 에 Compute 기능이 있다!!!
    let evaluateExpression =
        let dt = new System.Data.DataTable()
        fun expr -> System.Convert.ToDouble(dt.Compute(expr,""))

    // evaluateExpression "(1+5)*7/((3+(2-1))/(7-3))";;
    // val it : float = 42.0



    let NullableToOption (n : System.Nullable<_>) =
       if n.HasValue
       then Some n.Value
       else None


    // http://www.fssnip.net/gu/title/Cooperative-cancellation-in-Async-workflows
    type Cancellable() =
        static member Do(f:CancellationToken->unit, ct:CancellationToken) =
            let comp = async { f(ct) }
            Async.Start(comp, ct)

        static member Do(act:System.Action<CancellationToken>, ct:CancellationToken) =
            let comp =
                let f(ct) = act.Invoke(ct)
                async { f(ct) }
            Async.Start(comp, ct)


            // A unique overload for method 'Do' could not be determined based on type information prior to this program point. A type annotation may be needed. Candidates:
            // static member Cancellable.Do : act:Action<CancellationToken> * ct:CancellationToken -> 'a,
            // static member Cancellable.Do : f:(CancellationToken -> unit) * ct:CancellationToken -> unit

            //        let f (ct:CancellationToken) = act.Invoke(ct)
            //        Cancellable.Do(f, ct)

    /// 실행파일이 존재하는 경로명 반환
    let getBinDir() =
        let entry = System.Reflection.Assembly.GetEntryAssembly()
        Path.GetDirectoryName(entry.Location)

    /// 실행파일이 존재하는 경로에서 주어진 파일의 full path 반환
    let getFullFilePathOnBin file = Path.Combine(getBinDir(), file)
