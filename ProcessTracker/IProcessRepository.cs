namespace ProcessTracker;

// I define the contract every repository must fulfil
// Coding to an interface means I can swap implementations without touching the API
public interface IProcessRepository
{
    Task<List<BusinessProcess>> GetAllAsync();
    Task<BusinessProcess?>      GetByIdAsync(int id);
    Task<BusinessProcess>       CreateAsync(BusinessProcess process);
    Task<BusinessProcess?>      UpdateAsync(int id, BusinessProcess updated, string changedBy = "USER");
    Task<bool>                  DeleteAsync(int id);
}
