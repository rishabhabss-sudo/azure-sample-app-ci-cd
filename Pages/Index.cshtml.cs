using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

public class IndexModel : PageModel
{
	private readonly AppDbContext _context;
	private readonly BlobServiceClient _blobServiceClient;
	private readonly IConfiguration _configuration;
	private readonly ILogger<IndexModel> _logger;
	public IndexModel(AppDbContext context, BlobServiceClient blobServiceClient,
		IConfiguration configuration, ILogger<IndexModel> logger)
	{
		_context = context;
		_blobServiceClient = blobServiceClient;
		_configuration = configuration;
		_logger = logger;
	}

	public List<Product>? Products { get; set; }

	[BindProperty]
	public string? OrderId { get; set; }

	[BindProperty]
	public string? Status { get; set; }


	//public void OnGet()
	//{
	//}
	//public async Task OnPostAsync()
	//{
	//	Products = await _context.Products.AsNoTracking().ToListAsync();
	//}

	[BindProperty]
	public IFormFile? ImageFile { get; set; }

	public List<string>? ImageUrls { get; set; }

	public async Task<IActionResult> OnPostAsync()
	{
		Products = await _context.Products.AsNoTracking().ToListAsync();

		if (ImageFile == null) return Page();

		var container = _blobServiceClient.GetBlobContainerClient(
			_configuration["BlobStorage:ContainerName"]);

		var blobClient = container.GetBlobClient(
			Guid.NewGuid() + Path.GetExtension(ImageFile.FileName));

		using var stream = ImageFile.OpenReadStream();
		await blobClient.UploadAsync(stream, overwrite: true);

		return RedirectToPage();
	}

	public async Task<IActionResult> OnPostLoadImagesAsync()
	{
		ImageUrls = new List<string>();

		var container = _blobServiceClient.GetBlobContainerClient(
			_configuration["BlobStorage:ContainerName"]);

		var rawSas = _configuration["BlobStorage:SasToken"];

		// ensure only ONE ?
		var sasToken = rawSas.StartsWith("?")
			? rawSas
			: "?" + rawSas;

		await foreach (var blob in container.GetBlobsAsync())
		{
			var blobUri = container.GetBlobClient(blob.Name).Uri;
			ImageUrls.Add(blobUri + sasToken);
		}

		return Page();
	}
	public async Task<IActionResult> OnPostCallFunctionAsync()
	{
		try
		{
			var payload = new
			{
				orderId = OrderId,
				status = Status
			};

			var functionUrl = _configuration["AzureFunctionUrl"];

			using var client = new HttpClient();
			var content = new StringContent(
				JsonSerializer.Serialize(payload),
				Encoding.UTF8,
				"application/json");

			var response = await client.PostAsync(functionUrl, content);
			var responseBody = await response.Content.ReadAsStringAsync();

			return new JsonResult(new
			{
				status = (int)response.StatusCode,
				body = responseBody
			});
		}
		catch (System.Exception ex)
		{
			_logger.LogError(ex, "Azure Function call failed");
			return new JsonResult(new { error = ex.Message });
		}
	}
}

