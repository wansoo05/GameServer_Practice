using GameServer_01.Data;
using GameServer_01.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace GameServer_01.Services
{
    public class ContentsService : IContentsService
    {
        private readonly GameDbContext _db;
        public ContentsService(GameDbContext db) => _db = db;

        public async Task<(bool IsFound, JObject? data, string? ErrorMsg)> ChapterClearAsync(string uid, uint chapterId, ulong gold, ulong gem)
        {
            // 1) DB 조회
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == uid);
            var contents = await _db.Contents
                .FirstOrDefaultAsync(c => c.UID == uid);


            if (user == null || contents == null)
            {
                // 유저가 없으면 실패
                return (false, null, "User not found.");
            }

            contents.ChapterID = chapterId;
            user.Gold = gold;
            user.Gem = gem;
            
            _db.Contents.Update(contents);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            JObject jo = new JObject
            {
                ["message"] = "Success"
            };

            return (true, jo, null);
        }

        public async Task<(bool IsFound, JObject? data, string? ErrorMsg)> StageClearAsync(string uid, uint stageId, ulong gold, ulong gem)
        {
            // 1) DB 조회
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == uid);
            var contents = await _db.Contents
                .FirstOrDefaultAsync(c => c.UID == uid);


            if (user == null || contents == null)
            {
                // 유저가 없으면 실패
                return (false, null, "User not found.");
            }

            contents.StageID = stageId;
            user.Gold = gold;
            user.Gem = gem;

            _db.Contents.Update(contents);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            JObject jo = new JObject
            {
                ["message"] = "Success"
            };

            return (true, jo, null);
        }
    }
}
