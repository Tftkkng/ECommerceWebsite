// Creating a dummy ErrorViewModel.cs file with the provided content.
namespace ECommerceWebsite.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}