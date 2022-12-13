namespace Engine.CodeGenHMI

[<AutoOpen>]
module CodeGen =
    type Initializer = {
            succeed:bool;
            from:string;
            error:string;
            body:obj;
        }