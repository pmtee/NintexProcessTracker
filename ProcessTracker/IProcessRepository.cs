using ProcessTracker;

// I define the contract that any process repository must follow
// This allows me to swap implementations without changing the API endpoints
// Dependency injection in .NET relies on interfaces like this one
public interface IProcessRepository
{
    // I declare all operations the repository must support
    Task<List<BusinessProcess>> GetAllAsync();
    Task<BusinessProcess?> GetByIdAsync(int id);
    Task<BusinessProcess> CreateAsync(BusinessProcess process);
    Task<BusinessProcess?> UpdateAsync(int id, BusinessProcess updated);
    Task<bool> DeleteAsync(int id);
}