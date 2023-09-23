namespace DsMemoryService

open System
open System.Runtime.InteropServices
open System.IO
open Microsoft.Win32.SafeHandles

module MMFSecurity =

    let PAGE_READONLY = 0x02u
    let PAGE_READWRITE = 0x04u
    let PAGE_WRITECOPY = 0x08u
    let PAGE_EXECUTE_READ = 0x20u
    let PAGE_EXECUTE_READWRITE = 0x40u
    let PAGE_EXECUTE_WRITECOPY = 0x80u

    [<DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)>]
    extern bool ConvertStringSecurityDescriptorToSecurityDescriptor
        (string StringSecurityDescriptor, int StringSDRevision, IntPtr& SecurityDescriptor, IntPtr SecurityDescriptorSize)

    [<StructLayout(LayoutKind.Sequential)>]
    type SECURITY_ATTRIBUTES = 
        struct
            val mutable nLength : int
            val mutable lpSecurityDescriptor : IntPtr
            val mutable bInheritHandle : int
        end

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern IntPtr LocalFree(IntPtr hMem)

    [<DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)>]
    extern IntPtr CreateFileMapping
        (SafeFileHandle  hFile,  SECURITY_ATTRIBUTES& lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName)

    let CreateFileMappingApplySecurity(device: string) =
        let basePath = IOMapApi.MemoryUtilImpl.BasePath
        let filePath = Path.Combine(basePath, device)
        
        // Guard: Check if the file exists before attempting to open
        if not (File.Exists(filePath)) then
            failwith ("File not found: " + filePath)
        
        use fs = new FileStream(filePath, FileMode.Open)

        let sddl = "D:(A;OICI;GA;;;WD)"
        let mutable pSecurityDescriptor = IntPtr.Zero
        if ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, 1, &pSecurityDescriptor, IntPtr.Zero) then
            let mutable secAttr = SECURITY_ATTRIBUTES()
            secAttr.nLength <- Marshal.SizeOf(typeof<SECURITY_ATTRIBUTES>)
            secAttr.lpSecurityDescriptor <- pSecurityDescriptor
            secAttr.bInheritHandle <- 0

            let handle = CreateFileMapping(fs.SafeFileHandle, &secAttr, PAGE_READWRITE, 0u, uint fs.Length, IOMapApi.MemoryUtilImpl.getMMFName device)
            if handle = IntPtr.Zero then
                printfn "CreateFileMapping kernel32 Error: %d" (Marshal.GetLastWin32Error())
            else
                printfn "Memory mapped file created successfully!"

            LocalFree(pSecurityDescriptor) |> ignore

        pSecurityDescriptor
