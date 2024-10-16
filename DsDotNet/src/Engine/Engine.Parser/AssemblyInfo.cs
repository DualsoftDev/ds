using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Antlr4.Runtime;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customise this process see: https://aka.ms/assembly-info-properties


// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("2a959eec-ba9d-4c77-841b-d1dce28137b1")]
[assembly: InternalsVisibleTo("Engine.Sample, PublicKey=6b5d64113b30ff1b")]
[assembly: InternalsVisibleTo("Engine.Parser.FS, PublicKey=6b5d64113b30ff1b")]
[assembly: DebuggerDisplay("[Text={GetText()}]", Target = typeof(RuleContext))]