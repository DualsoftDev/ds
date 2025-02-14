using Microsoft.Extensions.Configuration;

using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;

namespace Dual.Common.AppSettings;

public static class JsonSetting
{
    public static IConfiguration Configure(string appSettingJsonPath) => CreateBuilder(appSettingJsonPath).Build();
    public static IConfigurationBuilder CreateBuilder(string appSettingJsonPath) => ConfigJsonSetting(new ConfigurationBuilder(), appSettingJsonPath);
    public static IConfigurationBuilder ConfigJsonSetting(this IConfigurationBuilder builder, string basePath, string appSettingJsonPath)
    {
        builder.SetBasePath(basePath)
            .AddJsonFile(appSettingJsonPath, optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRIONMENT") ?? "Production"}.json", optional: true)   //, reloadOnChange: true)
            .AddEnvironmentVariables()
            ;
        return builder;
    }
    public static IConfigurationBuilder ConfigJsonSetting(this IConfigurationBuilder builder, string appSettingJsonPath) => ConfigJsonSetting(builder, Directory.GetCurrentDirectory(), appSettingJsonPath);

    /// <summary>
    /// IConfiguration 특정 섹션을 클래스에 바인딩.
    /// </summary>
    public static T GetSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);  // 섹션을 바인딩
        return section;
    }

    /// <summary>
    /// appsettings.json 파일의 특정 섹션을 클래스에 바인딩.
    /// <br/> caller site 에서 Microsoft.Extensions.Configuration.Abstractions 참조없이 사용하기 위함
    /// </summary>
    public static T GetSectionEx<T>(string baseDir, string appSettingJson, string sectionName) where T : new()
    {
        IConfiguration configuration = new ConfigurationBuilder().ConfigJsonSetting(baseDir, appSettingJson).Build();
        return configuration.GetSection<T>(sectionName);
        //var section = new T();
        //configuration.GetSection(sectionName).Bind(section);  // 섹션을 바인딩
        //return section;
    }

    public static T GetSectionEx<T>(string appSettingJsonPath, string sectionName) where T : new()
    {
        IConfiguration configuration = new ConfigurationBuilder().ConfigJsonSetting(appSettingJsonPath).Build();
        return configuration.GetSection<T>(sectionName);
        //var section = new T();
        //configuration.GetSection(sectionName).Bind(section);  // 섹션을 바인딩
        //return section;
    }



    /// <summary>
    /// keyPath: e.g "AppSettings:Preferences:Platform"
    /// </summary>
    public static string GetRawValue(string appSettingJsonPath, string keyPath)
    {
        IConfiguration configuration = JsonSetting.Configure(appSettingJsonPath);
        return configuration.GetSection(keyPath).Value;
    }

    // AppSettings 객체를 json 파일에 저장
    public static void SaveSettings<T>(string appSettingJsonPath, string sectionName, T settings)
    {
        var json = File.ReadAllText(appSettingJsonPath);
        var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        // 해당 섹션을 업데이트
        jsonObj[sectionName] = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(settings));

        // 수정된 json을 파일에 다시 저장
        string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        File.WriteAllText(appSettingJsonPath, output);
    }


}
