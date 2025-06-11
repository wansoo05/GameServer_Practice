using GameServer_01.Data;
using Newtonsoft.Json.Linq;

namespace GameServer_01.Interfaces
{
    public interface IUserService
    {
        Task<(bool IsSuccess, string? UID, string? ErrorMsg)> RegisterAsync(
            string uid,
            string email,
            string deviceId,
            string provider);

        Task<(bool IsFound, JObject? data, string? ErrorMsg)> LoginAsync(string uid);
        Task<bool> DeleteLocalUserAsync(string firebaseUid);
    }
}
