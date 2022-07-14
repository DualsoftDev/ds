using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.IO;

namespace Dual.Common.Core
{
    //    var cronSpec = ((AppSettingsSection)Global.Configuration.GetSection("Mail")).Settings["CronSpec"].Value;


    /// <summary>
    /// App.config 파일을 분석.  필요시 수정 및 저장하기 위한 utility
    /// </summary>
    public static class AppConfigManager
    {
        /// <summary>
        /// 경로로 주어진 app.config 파일을 load.  타 exe 의 app.config loading
        /// </summary>
        public static Configuration LoadFile(string path)
        {
            ExeConfigurationFileMap configMap =
                new ExeConfigurationFileMap() { ExeConfigFilename = path };

            return ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        }

        /// <summary>
        /// <appSettings> 영역의 config section 만 반환
        /// </summary>
        public static AppSettingsSection GetAppSettingsSection(string path) => LoadFile(path).AppSettings;

        /// <summary>
        /// <appSettings> 영역의 config section 에서 (key, value) tuple 반환.  read-only 로 사용해야 함.
        /// </summary>
        public static IEnumerable<(string, string)> GetAppSettingsKeyValueTuples(string path)
        {
            var settings = LoadFile(path).AppSettings.Settings;
            return settings.AllKeys.Select(k => (k, settings[k].Value));
        }


        static IEnumerable<(string, ConfigurationSection)> getAllNamedSections(Configuration cfg)
        {
            foreach (var kv in GetAllNamedSections(cfg.Sections))
                yield return (kv.Key, kv.Value);


            ConfigurationSectionGroupCollection sectionGroups = cfg.SectionGroups;
            foreach (var key in sectionGroups.Keys.Cast<string>())
            {
                ConfigurationSectionGroup sectionGroup = sectionGroups[key];
                Console.WriteLine($"{sectionGroup.Name}");
                foreach (var kv in GetAllNamedSections(sectionGroup.Sections))
                    yield return ($"{key}/{kv.Key}", kv.Value);
            }
        }
        static IEnumerable<(string, ConfigurationSection)> getAllNamedSections(ConfigurationSectionCollection sections)
        {
            foreach (var key in sections.Keys.Cast<string>())
            {
                var section = sections[key];
                var typ = section.GetType();
                switch (section)
                {
                    case AppSettingsSection sec:
                        if (sec.Settings.Count > 0)
                            yield return (key, section);
                        break;
                    case ConnectionStringsSection _:
                        yield return (key, section);
                        break;

                    // NameValueSectionHandler 인 경우에도 DefaultSection 로 바뀌어서 들어옴.
                    // app.config 에서 NameValueSectionHandler 는 obsolete.  AppSettingsSection 를 이용할 것.
                    case DefaultSection _:
                    case IgnoreSection _:
                    case ProtectedConfigurationSection _:
                        break;

                    default:
                        Console.WriteLine($"Unknown section type {typ.FullName}");

                        switch (typ.FullName)
                        {
                            case "System.Diagnostics.SystemDiagnosticsSection":
                            case "System.Windows.WindowsFormsSection":
                                break;

                            case "Akka.Configuration.Hocon.AkkaConfigurationSection":
                                yield return (key, section);
                                break;
                            default:
                                break;

                        }
                        break;
                }
            }
        }

        /// <summary>
        /// <appSettings> 영역 및 <configSections> 구조 아래의 (<sectionGroup> 포함) <section> 목록을 모두 모아서 반환
        /// </summary>
        public static Dictionary<string, ConfigurationSection> GetAllNamedSections(Configuration cfg) =>
            getAllNamedSections(cfg).ToDictionary(kv => kv.Item1, kv => kv.Item2);

        public static Dictionary<string, ConfigurationSection> GetAllNamedSections(ConfigurationSectionCollection sections) =>
            getAllNamedSections(sections).ToDictionary(kv => kv.Item1, kv => kv.Item2);

        public static Dictionary<string, ConfigurationSection> GetAllNamedSections(string path) =>
            GetAllNamedSections(LoadFile(path));


        /// <summary>
        /// AppSettingsSection 의 settings 에서 주어진 key 에 해당하는 value 를 반환
        /// </summary>
        public static string GetValue(this KeyValueConfigurationCollection settings, string key, string defaultValue="")
            => settings.AllKeys.Contains(key) ? settings[key].Value : defaultValue;
        public static int GetIntValue(this KeyValueConfigurationCollection settings, string key, int defaultValue = 0)
        {
            var v = settings.GetValue(key);
            return v.Any() ? int.Parse(v) : defaultValue;
        }


#if RISK_SONARQUBE
#if DEBUG
        static AppConfigManager()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            File.WriteAllText(tmp, _sampleXml);
            var sections = GetAllNamedSections(tmp).ToArray();

            // sections[0] : "connectionStrings"
            // sections[1] : "appSettings"
            // sections[2] : "SpeedSection"
            // sections[3] : "SectionGroup/Section1"
            // sections[4] : "SectionGroup/Section2"

            Console.WriteLine();
        }
        private const string _sampleXml =
@"<?xml version = ""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name = ""SpeedSection"" type=""System.Configuration.AppSettingsSection"" />
    <sectionGroup name = ""SectionGroup"" >
      <section name = ""Section1"" type=""System.Configuration.AppSettingsSection"" />
      <section name = ""Section2"" type=""System.Configuration.AppSettingsSection"" />
    </sectionGroup>
  </configSections>

  <SpeedSection>
    <add key = ""PrinterSpeed"" value=""120"" />
    <add key = ""CameraSpeed"" value=""150"" />
  </SpeedSection>

  <SectionGroup>
    <Section1>
      <add key = ""Name"" value=""John"" />
      <add key = ""Age"" value=""33"" />
    </Section1>
    <Section2>
      <add key = ""City"" value=""Seoul"" />
      <add key = ""Contury"" value=""Korea"" />
    </Section2>
  </SectionGroup>


  <appSettings>
    <add key = ""Language"" value=""Ruby"" />
    <add key = ""Version"" value=""1.9.3"" />
  </appSettings>
</configuration>
";

#endif // DEBUG
#endif // RISK_SONARQUBE
    }
}
