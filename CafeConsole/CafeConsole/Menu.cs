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
            Console.WriteLine("Choose base: 1) Espresso, 2) Tea, 3) Hot Chocolate");
            var input = Console.ReadLine();
            var beverage = input switch
            {
                "1" => DomainConstants.Espresso,
                "2" => DomainConstants.Tea,
                "3" => DomainConstants.HotChocolate,
                _ => null
            };
            if (beverage != null)
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

            Console.WriteLine("Choose addons: 1) Milk 2) Syrup 3) Extra shot 0) Done");
            var input = Console.ReadLine();
            
            switch(input)
            {
                case "0":
                    {
                        break;
                    }
                case "1":
                    {
                        addOnKey = DomainConstants.Milk;
                        break;
                    }
                case "2":
                    {
                        addOnKey = DomainConstants.Syrup;
                        Console.WriteLine(ConsoleUIConstants.ChooseFlavourPrompt);
                        var flavour = Console.ReadLine();
                        args.Add(flavour);
                        break;
                    }
                case "3":
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

            if(validOptionSelected == false)
            {
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
            Console.WriteLine("Choose pricing strategy: 1) Regular, 2) Happy Hour");
            var input = Console.ReadLine();
            var strategy = input switch
            {
                "1" => DomainConstants.RegularPricing,
                "2" => DomainConstants.HappyHourPricing,
                _ => null
            };
            if (strategy != null)
            {
                return strategy;
            }
        }
    }

    private bool ShouldAskForOrder()
    {
        while(true)
        {
            Console.WriteLine("Choose action: 1) New order 0) Exit");
            var input = Console.ReadLine();
            bool? shouldAskForNewOrder = input switch
            {
                "0" => false,
                "1" => true,
                _ => null
            };
            if(shouldAskForNewOrder != null)
            {
                return shouldAskForNewOrder.Value;
            }
        }
    }
}
