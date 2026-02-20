using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.Entities;

namespace Web.Core.Interfaces
{
    public interface IPlantRepository
    {
        Task<List<PlantConfiguration>> GetAllAsync(string searchTerm, string plantType, bool? isActive);
        Task<PlantConfiguration?> GetByIdAsync(int id);
        Task AddAsync(PlantConfiguration plant);
        Task UpdateAsync(PlantConfiguration plant);
        Task DeleteAsync(int id);
        Task ToggleStatusAsync(int id, bool isActive);
    }
}
