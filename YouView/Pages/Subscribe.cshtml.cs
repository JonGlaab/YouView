using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Stripe.Checkout;

public class SubscribeModel : PageModel
{
    public IActionResult OnPost()
    {
        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = 499, // $4.99
                        ProductData = new()
                        {
                            Name = "YouView Premium"
                        },
                        Recurring = new()
                        {
                            Interval = "month"
                        }
                    },
                    Quantity = 1
                }
            },
            SuccessUrl = "https://localhost:5001/StripeSuccess",
            CancelUrl = "https://localhost:5001/StripeCancel"
        };

        var service = new SessionService();
        var session = service.Create(options);

        return Redirect(session.Url);
    }
}