using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/warehouse")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] ProductWarehouseDTO request)
    {
        try
        {
            int id = await _warehouseService.AddProductToWarehouseAsync(request);
            return Ok(new { Id = id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    
    [HttpPost("procedure")]
    public async Task<ActionResult> CreateProductWarehouseWithProcedure(ProductWarehouseDTO productWarehouseDto)
    {
        try
        {
            int generatedId = await _warehouseService.CreateProductWarehouseProcedureAsync(productWarehouseDto);
            return Ok(generatedId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            if (e.Message.Contains("IdWarehouse"))
                return NotFound("IdWarehouse not  found");
            
            if (e.Message.Contains("no order to fulfill"))
                return BadRequest("No order to fulfill");
            
            if (e.Message.Contains("IdProduct"))
                return NotFound("IdProduct not found");
            
            return StatusCode(500, "ERROR");
        }
    }
}
