namespace Dual.Web.Blazor.Client.Theme;
public enum Theme
{
    Undefined,
    DevExpressDark,
    DevExpressBerry,
    DevExpressPurple,
    DevExpressWhite,

    BootstrapCerulean,
    BootstrapCyborg,
    BootstrapFlatly,
    BootstrapJournal,
    BootstrapLitera,
    BootstrapLumen,
    BootstrapLux,
    BootstrapPulse,
    BootstrapSimplex,
    BootstrapSolar,
    BootstrapSuperhero,
    BootstrapUnited,
    BootstrapYeti
}

public static class ThemeExtension
{
    public static string GetThemeName(this Theme theme)
    {
        return theme switch
        {
            Theme.Undefined          => "blazing-dark",
            Theme.DevExpressDark     => "blazing-dark",
            Theme.DevExpressBerry    => "blazing-berry",
            Theme.DevExpressPurple   => "purple",
            Theme.DevExpressWhite    => "office-white",
            Theme.BootstrapCerulean  => "cerulean",
            Theme.BootstrapCyborg    => "cyborg",
            Theme.BootstrapFlatly    => "flatly",
            Theme.BootstrapJournal   => "journal",
            Theme.BootstrapLitera    => "litera",
            Theme.BootstrapLumen     => "lumen",
            Theme.BootstrapLux       => "lux",
            Theme.BootstrapPulse     => "pulse",
            Theme.BootstrapSimplex   => "simplex",
            Theme.BootstrapSolar     => "solar",
            Theme.BootstrapSuperhero => "superhero",
            Theme.BootstrapUnited    => "united",
            Theme.BootstrapYeti      => "yeti",
            _ => throw new Exception($"Unknown theme: {theme}")
        };
    }
}
