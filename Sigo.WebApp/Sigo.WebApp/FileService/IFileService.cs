using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sigo.WebApp.FileService
{
    public interface IFileService
    {
        Task<string> UploadAsync(IFormFile file, string code);
    }
}