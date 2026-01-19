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
            Metadata = new Dictionary<string, string>
            {
                { "userId", User.FindFirst("sub")?.Value ?? User.Identity.Name }
            },
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
                            Interval = "year"
                        }
                    },
                    Quantity = 1
                }
            },
            SuccessUrl = "http://localhost:5143/StripeSuccess",
            CancelUrl = "http://localhost:5143/StripeCancel"
        };

        var service = new SessionService();
        var session = service.Create(options);

        return Redirect(session.Url);
    }
}