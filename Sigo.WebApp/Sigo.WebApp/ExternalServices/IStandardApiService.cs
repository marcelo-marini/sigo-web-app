using System.Collections.Generic;
using System.Threading.Tasks;
using Sigo.WebApp.Models;

namespace Sigo.WebApp.ExternalServices
{
    public interface IStandardApiService
    {
        Task<IEnumerable<Standard>> GetStandardsAsync();
        Task<UpdateStandard> GetStandardByIdAsync(string id);
        Task<Standard> CreateStandardAsync(CreateStandard standard);
        Task<Standard> UpdateStandardAsync(UpdateStandard standard);
        Task DeleteStandardAsync(string id);
        Task<UserInfoViewModel> GetUserInfoAsync();
    }
}