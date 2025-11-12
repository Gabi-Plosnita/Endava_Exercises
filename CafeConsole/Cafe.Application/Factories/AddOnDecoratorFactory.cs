using Cafe.Domain;

namespace Cafe.Application;

public sealed class AddOnDecoratorFactory : IAddOnDecoratorFactory
{
    private const decimal MilkPrice = 0.40m;
    private const decimal SyrupPrice = 0.50m;
    private const decimal ExtraShotPrice = 0.80m;

    public IBeverage Create(IBeverage baseBeverage, AddOnSelection selection)
    {
        return selection.AddOnType switch
        {
            AddOnType.Milk => new MilkDecorator(baseBeverage, "milk", MilkPrice),

            AddOnType.ExtraShot => new ExtraShotDecorator(baseBeverage, "extra shot", ExtraShotPrice),

            AddOnType.Syrup => new SyrupDecorator(
                baseBeverage,
                name: "syrup",
                cost: SyrupPrice,
                flavor: selection.Flavour ?? "vanilla"),

            _ => throw new ArgumentException(nameof(selection.AddOnType), $"Unsupported add-on type {selection.AddOnType}")
        };
    }
}