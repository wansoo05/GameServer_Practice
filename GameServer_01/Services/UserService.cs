using GameServer_01.Data;
using GameServer_01.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace GameServer_01.Services
{
    public class UserService : IUserService
    {
        private readonly GameDbContext _db;
        public UserService(GameDbContext db) => _db = db;

        public async Task<(bool IsSuccess, string? UID, string? ErrorMsg)> RegisterAsync(
            string uid,
            string email,
            string deviceId,
            string provider)
        {
            bool alreadyExists = await _db.Users.AnyAsync(u => u.FirebaseUID == uid);

            if (alreadyExists)
            {
                return (false, null, "User already registered");
            }

            // 2) 유저 엔티티 생성
            var newUser = new User
            {
                FirebaseUID = uid,
                PlayerID = " ",
                Email = email,
                Gold = 0,
                Gem = 0,
                Diamond = 0,
                DeviceId = deviceId,
                Provider = provider,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            // 3) DB에 유저 저장
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            // 4) 인벤토리 초기화
            //await InitializeInventoryAsync(newUser.FirebaseUID);

            // 5) 컨텐츠(예: 스테이지, 챕터) 세팅
            await InitializeContentsAsync(newUser.FirebaseUID);

            // 6) 튜토리얼/챕터 진행 상태 초기화
            //await InitializeProgressAsync(newUser.Id);

            return (true, newUser.FirebaseUID, null);
        }

        public async Task<(bool IsFound, JObject? data, string? ErrorMsg)> LoginAsync(string uid)
        {
            // 1) User 조회
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == uid);

            if (user == null)
            {
                // 유저가 없으면 실패
                return (false, null, "User not found.");
            }

            // 2) LastLoginAt 업데이트
            user.LastLoginAt = DateTime.UtcNow;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            // 3) Contents 조회 (별도 테이블)
            var contents = await _db.Contents
                .FirstOrDefaultAsync(c => c.UID == uid);

            // 5) 이제 User, Contents, Inventory 데이터를 사용해 JObject 생성
            var jo = new JObject
            {
                ["user"] = new JObject
                {
                    ["firebaseUid"] = user.FirebaseUID,
                    ["playerId"] = user.PlayerID ?? string.Empty,
                    ["provider"] = user.Provider,
                    ["deviceId"] = user.DeviceId ?? string.Empty,
                    ["email"] = user.Email,
                    ["gold"] = user.Gold,
                    ["gem"] = user.Gem,
                    ["diamond"] = user.Diamond,
                    ["createdAt"] = user.CreatedAt.ToString("o"),
                    ["lastLoginAt"] = user.LastLoginAt.ToString("o")
                },
                ["contents"] = new JObject
                {
                    ["chapterId"] = contents?.ChapterID ?? 0,
                    ["stageId"] = contents?.StageID ?? 0
                }
            };
            Console.WriteLine(contents?.ChapterID);
            return (true, jo, null);
        }

        public async Task<bool> DeleteLocalUserAsync(string firebaseUid)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUid);
            if (user == null)
                return false;
            var contents = await _db.Contents.FirstOrDefaultAsync(u => u.UID == firebaseUid);
            if (contents == null) return false;

            _db.Contents.Remove(contents);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task InitializeContentsAsync(string userId)
        {
            var contents = new Contents
            {
                UID = userId,
                StageID = 1,
                ChapterID = 101,
            };
            _db.Contents.Add(contents);
            await _db.SaveChangesAsync();
        }
    }
}
