using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args); 

var connectionString = builder.Configuration.GetConnectionString("YouViewDbConnection") 
                       ?? throw new InvalidOperationException("Connection string 'YouViewDbConnection' not found.");

// Read the string from appsettings
var blobConnectionString = builder.Configuration.GetSection("AzureStorage")["ConnectionString"];

//Register db
builder.Services.AddDbContext<YouViewDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => {
     
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    }));

 //Register the BlobServiceClient so you can use it in your Upload page
builder.Services.AddSingleton(x => new BlobServiceClient(blobConnectionString));

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


// Add services to the container.
builder.Services.AddRazorPages();

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