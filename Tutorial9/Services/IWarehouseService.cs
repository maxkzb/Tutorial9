using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouseAsync(ProductWarehouseDTO productWarehouseDto);
    Task<int> CreateProductWarehouseProcedureAsync(ProductWarehouseDTO request);
}
