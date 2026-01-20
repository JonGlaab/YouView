using Microsoft.AspNetCore.Mvc.RazorPages;
using YouView.Data;
using Microsoft.EntityFrameworkCore;

public class StripeSuccessModel : PageModel
{
    private readonly YouViewDbContext _context;

    public StripeSuccessModel(YouViewDbContext context)
    {
        _context = context;
    }

    public string UserName { get; private set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user != null)
            {
                UserName = user.UserName;
            }
        }
    }
}