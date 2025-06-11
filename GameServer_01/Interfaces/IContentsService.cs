using Newtonsoft.Json.Linq;

namespace GameServer_01.Interfaces
{
    public interface IContentsService
    {
        Task<(bool IsFound, JObject? data, string? ErrorMsg)> ChapterClearAsync(string uid, uint chapterId, ulong gold, ulong gem);
        Task<(bool IsFound, JObject? data, string? ErrorMsg)> StageClearAsync(string uid, uint stageId, ulong gold, ulong gem);
    }
}
