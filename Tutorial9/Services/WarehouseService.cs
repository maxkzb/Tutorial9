using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IConfiguration _configuration;

    public WarehouseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductToWarehouseAsync(ProductWarehouseDTO productWarehouseDto)
    {
        if (productWarehouseDto.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        await using var conn = new SqlConnection(_configuration.GetConnectionString("Default"));
        await conn.OpenAsync();
        await using var tran = await conn.BeginTransactionAsync();

        try
        {
            // Check if product and warehouse exist, get matching order, get product price
            var cmd = new SqlCommand(@"
                SELECT 
                    (SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct),
                    (SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse),
                    (SELECT TOP 1 IdOrder FROM [Order] 
                     WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt),
                    (SELECT Price FROM Product WHERE IdProduct = @IdProduct)", conn, (SqlTransaction)tran);

            cmd.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
            cmd.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);
            cmd.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", productWarehouseDto.CreatedAt);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            if (reader.GetInt32(0) == 0)
                throw new ArgumentException("Product not found");
            if (reader.GetInt32(1) == 0)
                throw new ArgumentException("Warehouse not found");

            int? orderId = await reader.IsDBNullAsync(2) ? null : reader.GetInt32(2);
            if (orderId == null)
                throw new ArgumentException("Matching order not found");

            decimal price = reader.GetDecimal(3);
            await reader.CloseAsync();

            // Check if order already fulfilled
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @OrderId", conn, (SqlTransaction)tran);
            checkCmd.Parameters.AddWithValue("@OrderId", orderId);
            int exists = (int)await checkCmd.ExecuteScalarAsync();

            if (exists > 0)
                throw new InvalidOperationException("Order already fulfilled");

            // Update order with FulfilledAt
            var updateCmd = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @OrderId", conn, (SqlTransaction)tran);
            updateCmd.Parameters.AddWithValue("@OrderId", orderId);
            await updateCmd.ExecuteNonQueryAsync();

            // Insert into Product_Warehouse
            var insertCmd = new SqlCommand(@"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                OUTPUT INSERTED.IdProductWarehouse
                VALUES (@IdWarehouse, @IdProduct, @OrderId, @Amount, @TotalPrice, GETDATE())", conn, (SqlTransaction)tran);

            insertCmd.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);
            insertCmd.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
            insertCmd.Parameters.AddWithValue("@OrderId", orderId);
            insertCmd.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
            insertCmd.Parameters.AddWithValue("@TotalPrice", productWarehouseDto.Amount * price);
            
            var newId = (int)await insertCmd.ExecuteScalarAsync();
            await tran.CommitAsync();

            return newId;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }
    public async Task<int> CreateProductWarehouseProcedureAsync(ProductWarehouseDTO productWarehouseDto)
    {
        using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            int Id = 0;
            await sqlConnection.OpenAsync();

            using (var command = new SqlCommand("AddProductToWarehouse", sqlConnection))
            {
                command.Parameters.AddWithValue("@IdProduct", productWarehouseDto.IdProduct);
                command.Parameters.AddWithValue("@IdWarehouse", productWarehouseDto.IdWarehouse);
                command.Parameters.AddWithValue("@Amount", productWarehouseDto.Amount);
                command.Parameters.AddWithValue("@CreatedAt", productWarehouseDto.CreatedAt);

                command.CommandType = CommandType.StoredProcedure;

                var result= await command.ExecuteScalarAsync();
                Id = Convert.ToInt32(result);
            }
            return Id;
        }
        
    }
}
