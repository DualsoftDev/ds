namespace Engine.CodeGen

[<AutoOpen>]
module CodeGen =
    type Initializer = {
            succeed:bool;
            from:string;
            body:string;
            error:string;
        }