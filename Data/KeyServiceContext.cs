using Microsoft.EntityFrameworkCore;
using MemberService.Models;

namespace MemberService.Data
{
    public class KeyServiceContext : DbContext
    {
        public KeyServiceContext(DbContextOptions<KeyServiceContext> options) : base(options) { }

        // 更新資料表名稱為 "member"
        public DbSet<User> member { get; set; }

        // 新增 Admins 資料表的映射
        public DbSet<Admin> Admins { get; set; } // 新增 Admins 資料表對應

        // 新增 KetInformation  資料表
        public DbSet<Key> key { get; set; }

        // 新增 Product 資料表
        public DbSet<Product> product { get; set; } 
    }
}
