using Microsoft.AspNetCore.Mvc;
using global::InventoryService.Services;
using Shared.Models;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<Product>> GetProduct(int productId)
        {
            var product = await _inventoryService.GetProductAsync(productId);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost("refresh-stock-count")]
        public async Task<IActionResult> ReduceStock([FromBody] ReduceStockRequest request)
        {
            var result = await _inventoryService.ReduceStockAsync(request.ProductId, request.Quantity, "Manual Adjustment");
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }
    }

    public class ReduceStockRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
