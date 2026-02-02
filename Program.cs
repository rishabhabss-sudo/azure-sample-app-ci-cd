using Azure;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(_ =>
{
	var config = builder.Configuration;

	var accountUrl = config["BlobStorage:AccountUrl"];
	var sasToken = config["BlobStorage:SasToken"];

	return new BlobServiceClient(
		new Uri(accountUrl),
		new AzureSasCredential(sasToken.Replace("?", ""))
	);
});

var app = builder.Build();

//// Configure pipeline
//if (!app.Environment.IsDevelopment())
//{
//	app.UseExceptionHandler("/Error");
//	app.UseHsts();
//}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

// Simple health endpoint (useful in Azure)
app.MapGet("/health", () => "App is running successfully on Azure!");

app.Run();

