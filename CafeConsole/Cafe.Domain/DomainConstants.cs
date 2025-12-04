namespace Cafe.Domain;

public static class DomainConstants
{
    public const string Espresso = "Espresso";
    public const string Tea = "Tea";
    public const string HotChocolate = "Hot Chocolate";

    public const string Milk = "Milk";
    public const string Syrup = "Syrup";
    public const string ExtraShot = "Extra Shot";

    public const string RegularPricing = "Regular";
    public const string RegularPricingDescription = "Regular Pricing (No Discounts)";

    public const string HappyHourPricing = "Happy Hour";
    public static string HappyHourPricingDescription(decimal percentage) => $"Happy Hour Pricing ({percentage:P0} Discount)";
    public const decimal HappyHourDiscountRate = 0.2m;

    public const decimal EspressoPrice = 2.50m;
    public const decimal TeaPrice = 2.00m;
    public const decimal HotChocolatePrice = 3.00m;

    public const decimal MilkPrice = 0.40m;
    public const decimal SyrupPrice = 0.50m;
    public const decimal ExtraShotPrice = 0.80m;

    public const string UsdCurrency = "USD";

    public const string BeverageNameCannotBeEmpty = "Beverage name cannot be null or white space.";
    public const string BeverageCostMustBeGreaterThanZero = "Beverage cost must be greater than 0.";
    public const string SyrupFlavorRequired = "Syrup flavor is required.";
}
