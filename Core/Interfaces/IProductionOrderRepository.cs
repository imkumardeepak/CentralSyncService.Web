using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.Entities;

namespace Web.Core.Interfaces
{
    public interface IProductionOrderRepository
    {
        Task<List<ProductionOrder>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<ProductionOrder?> GetByIdAsync(int id);
        Task<List<ProductionOrder>> GetByPlantCodeAsync(string plantCode);
        Task<List<ProductionOrder>> GetByOrderNoAsync(int orderNo);
        Task<List<ProductionOrder>> GetPendingOrdersAsync();
        Task<int> GetTotalCountAsync();
    }
}
