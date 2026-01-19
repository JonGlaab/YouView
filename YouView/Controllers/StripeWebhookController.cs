using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using YouView.Data;

[ApiController]
[Route("api/stripe/webhook")]
public class StripeWebhookController : ControllerBase
{
    private readonly YouViewDbContext _context;
    private readonly IConfiguration _config;

    public StripeWebhookController(YouViewDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = _config["Stripe:WebhookSecret"];
        
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret
            );
        }
        catch (StripeException)
        {
            return BadRequest();
        }
        
        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;

            var userId = session?.Metadata["userId"];

            if (userId != null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !user.IsPremium)
                {
                    user.IsPremium = true;
                    await _context.SaveChangesAsync();
                }
            }
        }
        return Ok();
    }
}