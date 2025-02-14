namespace Dual.Web.Blazor.Client.Components;

public static class ClientExtension
{
    public static string ToStringBrief(this DateTime date)
    {
        if (date.Year == DateTime.Now.Year)
            return date.ToString(@"MM\/dd HH:mm:ss");
        else
            return date.ToString(@"YY\/MM\/dd HH:mm:ss");
    }
    public static string ToStringBrief(this TimeSpan ts) => ts.ToString(@"d\일\ hh\:mm\:ss");

    public static string ToStringBrief(this object oDateTime)
    {
        switch(oDateTime)
        {
            case null:
                return null;
            case DateTime date:
                return date.ToStringBrief();
            case TimeSpan ts:
                //Console.WriteLine($"---------------------- ORIGINAL timespan = {ts}");
                return ts.ToStringBrief();
            default:
                return oDateTime.ToString();
        }
    }
}

/*
    <DxGridDataColumn FieldName=@nameof(Action.Started) CaptionAlignment=GridTextAlignment.Right TextAlignment=GridTextAlignment.Right>
        <CellDisplayTemplate>
            @context.GetRowValue(nameof(Action.Started)).ToStringBrief()
        </CellDisplayTemplate>
    </DxGridDataColumn>

 */
