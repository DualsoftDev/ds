using Microsoft.Extensions.Configuration;

namespace Dual.Common.AppSettings.Sample
{
    public class Contact
    {
        public Contact()
            : this("", 0, "")
        {
        }
        public Contact(string name, int age, string phone)
        {
            Name = name;
            Age = age;
            Phone = phone;
        }

        public string Name { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }
    }

    public static class TestMe
    {
        static void X()
        {
            var config = JsonSetting.Configure("appsettings-sample.json");
            var cf = config.GetValue<string>("confing-filename");
            var pi = config.GetValue<double>("pi");
            var exclude = config.GetSection("exclude").Get<string[]>();
            var contacts = config.GetSection("contacts").Get<Sample.Contact[]>();

            // path from environment varaible
            var path = config.GetValue<string>("PATH");
        }
    }
}
