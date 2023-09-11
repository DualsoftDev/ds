using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHMI.Shared;

public class DsModelLoader
{
    public static bool storeModel(byte[] model)
    {
        try
        {
            File.WriteAllBytes("tmp.zip", model);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
