using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Business.Services;
using RestaurantManagement.Common.Enums;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Policy = "KitchenOrManager")]
public class KitchenController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<KitchenController> _logger;

    public KitchenController(
        IOrderService orderService,
        ILogger<KitchenController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(KitchenStation? station = null)
    {
        // Hệ thống không sử dụng đối tượng đầu bếp - chuyển hướng về sơ đồ bàn vận hành
        return RedirectToAction("Index", "Floor");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderDetailId, OrderItemStatus status, KitchenStation? station = null)
    {
        var result = await _orderService.UpdateItemStatusAsync(orderDetailId, status);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error?.Description ?? "Không thể cập nhật trạng thái món.";
        }

        return RedirectToAction(nameof(Index), new { station });
    }
}
