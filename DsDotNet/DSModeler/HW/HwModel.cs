using System.ComponentModel;
using static PrologueModule;

namespace DSModeler.HW
{
    public class HwModel
    {
        public HwModel() { }
        public HwModel(int num, string name, int modelId, string type, string company)
        {
            Number = num;
            Name = name;
            ModelId = modelId;
            Type = type;
            Company = company;
        }

        public int Number { get; set; }
        public string Name { get; set; }
        public int ModelId { get; set; }
        public string Type { get; set; }
        public string Company { get; set; }
        public string Description { get; set; }
        [Browsable(false)]
        public string ToTextRegister => $"{Company};{Name}";
    }

    public static class HwModels
    {
        private static List<HwModel> list = new List<HwModel>();
        public static List<HwModel> List => list.Any() ? list : GetList();
        private static List<HwModel> GetList()
        {
            List<HwModel> lstM = new List<HwModel>();
            int getNumber() => lstM.Count + 1;
            //LS PLC 등록
            PLCHwModel.Models.Iter(m =>
            {
                lstM.Add(new HwModel()
                {
                    Number = getNumber(),
                    Name = m.Name,
                    ModelId = m.Id,
                    Type = m.Type.ToString(),
                    Company = Company.LSE,
                    Description = m.IsIEC ? "IEC Type" : ""
                });
            });

            //PAIX 등록
            lstM.Add(new HwModel(getNumber(), "NMC2", 1, "Ethernet", Company.PAIX));
            lstM.Add(new HwModel(getNumber(), "NMF", 2, "EtherCat", Company.PAIX));
            lstM.Add(new HwModel(getNumber(), "NMF", 3, "WMX", Company.PAIX));
            list = lstM;
            return lstM;
        }

        public static List<HwModel> GetListByCompany(string company)
        {
            return List.Where(w => w.Company == company).ToList();
        }
        public static int GetModelNumberByRegs()
        {
            var runHWDevice = DSRegistry.GetValue(RegKey.RunHWDevice)?.ToString();
            var model = List.Where(w => w.ToTextRegister == runHWDevice).FirstOrDefault();
            if (model == null)
                return 0;
            else
                return model.Number;
        }

        public static HwModel GetModelByNumber(int num)
        {
            var runHWDevice = DSRegistry.GetValue(RegKey.RunHWDevice)?.ToString();
            var model = List.Where(w => w.Number == num).FirstOrDefault();
            return model;
        }
    }


}


