using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsWebApp.Shared;
public class RuntimeModelDto(string sourceDsZipPath, bool isCpuRunning)
{
    public string SourceDsZipPath => sourceDsZipPath;
    public bool IsCpuRunning => isCpuRunning;
}

