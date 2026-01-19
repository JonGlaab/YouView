using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;
using Azure.Storage.Blobs;
using YouView.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args); 

// stripe keys
StripeConfiguration.ApiKey =
    builder.Configuration["Stripe:SecretKey"];

var connectionString = builder.Configuration.GetConnectionString("YouViewDbConnection") 
                       ?? throw new InvalidOperationException("Connection string 'YouViewDbConnection' not found.");

// Read the string from appsettings
var blobConnectionString = builder.Configuration["AzureStorage:ConnectionString"]
                           ?? builder.Configuration.GetConnectionString("AzureStorage")
                           ?? throw new InvalidOperationException("Connection string 'AzureStorage' not found.");

//Register the BlobServiceClient so you can use it in your Upload page
builder.Services.AddSingleton(x => new BlobServiceClient(blobConnectionString));
builder.Services.AddScoped<YouView.Services.BlobService>();

//Register db
builder.Services.AddDbContext<YouViewDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => {
     
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    }));
//register AI summary
builder.Services.AddScoped<AiService>();

//Register Identity
 builder.Services.AddDefaultIdentity<User>(options => {
         options.SignIn.RequireConfirmedAccount = false; // Todo: change to true after and remove this part
         options.Password.RequireDigit = false;
         options.Password.RequiredLength = 6;
         options.Password.RequireNonAlphanumeric = false;
         options.Password.RequireUppercase = false;
     })
     .AddEntityFrameworkStores<YouViewDbContext>();

 builder.Services.ConfigureApplicationCookie(options =>
 {
     options.LoginPath = "/login";
     options.LogoutPath = "/logout";
 });
// ffmepg register
 builder.Services.AddScoped<YouView.Services.VideoProcessor>();
 
// Add services to the container.
builder.Services.AddRazorPages();

// add email
builder.Services.AddTransient<EmailService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1L * 1024 * 1024 * 1024; // 1 GB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1L * 1024 * 1024 * 1024; // 1 GB
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Startup failed: " + ex);
    throw;
}