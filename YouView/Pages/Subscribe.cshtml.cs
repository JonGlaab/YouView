using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using YouView.Data;

public class SubscribeModel : PageModel
{
    private readonly YouViewDbContext _context;

    public SubscribeModel(YouViewDbContext context)
    {
        _context = context;
    }

    public bool IsPremium { get; set; }

    public string UserName { get; set; }

    public async Task OnGetAsync()
    {
        var username = User.Identity?.Name;

        if (!string.IsNullOrEmpty(username))
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (user != null)
            {
                IsPremium = user.IsPremium;
                UserName = user.UserName;
            }
        }
    }
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
            SuccessUrl = "http://youview.azurewebsites.net/StripeSuccess",
            CancelUrl = "http://youview.azurewebsites.net/StripeCancel"
        };

        var service = new SessionService();
        var session = service.Create(options);

        return Redirect(session.Url);
    }
}