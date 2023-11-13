namespace Engine.CodeGenHMI

[<AutoOpen>]
module CodeGen =
    type Initializer =
        { success: bool
          from: string
          error: string
          body: obj }
