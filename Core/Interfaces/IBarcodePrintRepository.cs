using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.Entities;

namespace Web.Core.Interfaces
{
    public interface IBarcodePrintRepository
    {
        Task<List<BarcodePrint>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<BarcodePrint?> GetByIdAsync(int id);
        Task<List<BarcodePrint>> GetByBarcodeAsync(string barcode);
        Task<List<BarcodePrint>> GetByDateRangeAsync(System.DateTime startDate, System.DateTime endDate);
        Task<int> GetTotalCountAsync();
    }
}
