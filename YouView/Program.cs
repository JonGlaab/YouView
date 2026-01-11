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

builder.Services.AddDbContext<YouViewDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the BlobServiceClient so you can use it in your Upload page
builder.Services.AddSingleton(x => new BlobServiceClient(blobConnectionString));

builder.Services.AddDefaultIdentity<User>(options => {
        options.SignIn.RequireConfirmedAccount = false; // Todo: change to true after and remove this part
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<YouViewDbContext>();

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

app.Run();
