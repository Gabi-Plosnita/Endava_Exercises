using Cafe.Domain;

namespace Cafe.Application;

public class AddOnFactory : IAddOnDecoratorFactory
{
    private const decimal MilkPrice = 0.40m;
    private const decimal SyrupPrice = 0.50m;
    private const decimal ExtraShotPrice = 0.80m;

    public Result<IBeverage> Create(IBeverage baseBeverage, AddOnSelection selection)
    {
        var result = new Result<IBeverage>();

        if (baseBeverage == null)
        {
            result.AddError("Base beverage cannot be null.");
            return result;
        }

        IBeverage? decorator = null;
        switch (selection.AddOnType)
        {
            case AddOnType.Milk:
                {
                    decorator = new MilkDecorator(baseBeverage, "milk", MilkPrice);
                    break;
                }

            case AddOnType.ExtraShot:
                {
                    decorator = new ExtraShotDecorator(baseBeverage, "extra shot", ExtraShotPrice);
                    break;
                }

            case AddOnType.Syrup:
                {
                    if (string.IsNullOrWhiteSpace(selection.Flavour))
                    {
                        result.AddError("Syrup flavour must be specified.");
                        return result;
                    }
                    decorator = new SyrupDecorator(
                        baseBeverage,
                        name: "syrup",
                        cost: SyrupPrice,
                        flavor: selection.Flavour);
                    break;
                }
            default:
                {
                    result.AddError($"Unsupported add-on type: {selection.AddOnType}");
                    return result;
                }
        }

        result.Value = decorator;
        return result;
    }
};