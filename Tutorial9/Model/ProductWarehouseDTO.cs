using System.Runtime.InteropServices.JavaScript;

namespace Tutorial9.Model;

public class ProductWarehouseDTO
{
    public int IdProduct { get; set; }
    public int IdWarehouse { get; set; }
    public int Amount { get; set; }
    public JSType.Date CreatedAt { get; set; }
}