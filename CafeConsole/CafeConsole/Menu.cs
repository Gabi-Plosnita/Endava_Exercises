using Cafe.Application;
using Cafe.Domain;

namespace Cafe.ConsoleUI;

public class Menu
{
    private readonly IOrderService _orderService;

    public Menu(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public void Run()
    {
        var shouldAskForOrder = true;

        while(shouldAskForOrder)
        {
            var beverage = ChooseBaseBeverage();
            var addOns = ChooseAddOns();
            var pricingStrategy = ChoosePricingStrategy();

            var orderRequest = new OrderRequest(
                BeverageKey: beverage,
                AddOns: addOns,
                PricingStrategyKey: pricingStrategy
            );

            var orderResult = _orderService.PlaceOrder(orderRequest);
            if (orderResult.IsSuccessful)
            {
                Console.WriteLine(ConsoleUIConstants.OrderPlacedSuccessfullyMessage);
            }
            else
            {
                Console.WriteLine(ConsoleUIConstants.FailedToPlaceOrderMessage);
                Console.WriteLine(orderResult);
            }

            shouldAskForOrder = ShouldAskForOrder();
        }
    }

    private string ChooseBaseBeverage()
    {
        while (true)
        {
            Console.WriteLine(ConsoleUIConstants.ChooseBaseBeverageMessage);
            var input = Console.ReadLine();
            var beverage = input switch
            {
                ConsoleUIConstants.EspressoOption => DomainConstants.Espresso,
                ConsoleUIConstants.TeaOption => DomainConstants.Tea,
                ConsoleUIConstants.HotChocolateOption => DomainConstants.HotChocolate,
                _ => null
            };
            if(beverage == null)
            {
                Console.WriteLine(ConsoleUIConstants.InvalidOptionMessage);
            }
            else
            {
                return beverage;
            }
        }
    }

    private List<AddOnSelection> ChooseAddOns()
    {
        var addOns = new List<AddOnSelection>();
        while (true)
        {
            var validOptionSelected = true;
            string addOnKey = null;
            var args = new List<object>();

            Console.WriteLine(ConsoleUIConstants.ChooseAddOnsMessage);
            var input = Console.ReadLine();
            
            switch(input)
            {
                case ConsoleUIConstants.DoneOption:
                    {
                        break;
                    }
                case ConsoleUIConstants.MilkOption:
                    {
                        addOnKey = DomainConstants.Milk;
                        break;
                    }
                case ConsoleUIConstants.SyrupOption:
                    {
                        addOnKey = DomainConstants.Syrup;
                        Console.WriteLine(ConsoleUIConstants.ChooseFlavourPrompt);
                        var flavour = Console.ReadLine();
                        args.Add(flavour);
                        break;
                    }
                case ConsoleUIConstants.ExtraShotOption:
                    {
                        addOnKey = DomainConstants.ExtraShot;
                        break;
                    }
                default:
                    {
                        validOptionSelected = false;
                        break;
                    }
            }

            if (!validOptionSelected)
            {
                Console.WriteLine(ConsoleUIConstants.InvalidOptionMessage);
                continue;
            }

            if(addOnKey == null)
            {
                break;
            }

            var addOnSelection = new AddOnSelection(addOnKey, args.ToArray());
            addOns.Add(addOnSelection);
        }
        return addOns;
    }

    private string ChoosePricingStrategy()
    {
        while (true)
        {
            Console.WriteLine(ConsoleUIConstants.ChoosePricingStrategyMessage);
            var input = Console.ReadLine();
            var strategy = input switch
            {
                ConsoleUIConstants.RegularPricingOption => DomainConstants.RegularPricing,
                ConsoleUIConstants.HappyHourPricingOption => DomainConstants.HappyHourPricing,
                _ => null
            };
            if(strategy == null)
            {
                Console.WriteLine(ConsoleUIConstants.InvalidOptionMessage);
            }
            else
            {
                return strategy;
            }
        }
    }

    private bool ShouldAskForOrder()
    {
        while(true)
        {
            Console.WriteLine(ConsoleUIConstants.AskForAnotherOrderMessage);
            var input = Console.ReadLine();
            bool? shouldAskForNewOrder = input switch
            {
                ConsoleUIConstants.NoOption => false,
                ConsoleUIConstants.YesOption => true,
                _ => null
            };
            if(shouldAskForNewOrder == null)
            {
               Console.WriteLine(ConsoleUIConstants.InvalidOptionMessage);
            }
            else
            {
                return shouldAskForNewOrder.Value;
            }
        }
    }
}
