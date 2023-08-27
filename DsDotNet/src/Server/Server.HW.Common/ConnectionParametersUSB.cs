using System;

namespace Server.HW.Common
{
    public abstract class ConnectionParametersUSB : IConnectionParametersUSB
    {
        public int VendorID { get; }
        public int ProductID { get; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

        public ConnectionParametersUSB(int vendorID, int productID)
        {
            VendorID = vendorID;
            ProductID = productID;
        }
    }
}
