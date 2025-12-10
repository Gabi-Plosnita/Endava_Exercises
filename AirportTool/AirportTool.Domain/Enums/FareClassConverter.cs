namespace AirportTool.Domain;

public static class FareClassConverter
{
    public static string FareClassToCode(FareClass fareClass)
    {
        return fareClass switch
        {
            FareClass.Economy => "Y",
            FareClass.PremiumEconomy => "M",
            FareClass.Business => "J",
            FareClass.First => "F",
            _ => throw new InvalidOperationException($"Unknown FareClass: {fareClass}")
        };
    }

    public static FareClass CodeToFareClass(string code)
    {
        return code switch
        {
            "Y" => FareClass.Economy,
            "M" => FareClass.PremiumEconomy,
            "J" => FareClass.Business,
            "F" => FareClass.First,
            _ => throw new InvalidOperationException($"Unknown FareClass value from DB: '{code}'")
        };
    }
}

