using System.IO;
using System.Threading.Tasks;

namespace destino_redacao_1000_api
{
    public interface IUploadFile
    {
        Task<string> UploadFileAsync(Stream fileStream, string keyName);
        Task<string> DeleteFileAsync(string keyName);        
    }
}