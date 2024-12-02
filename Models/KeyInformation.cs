using System.ComponentModel.DataAnnotations.Schema;

namespace MemberService
{
    public class Key
    {
        public int Id { get; set; }
        [Column("key")] // 對應到資料表中的 key 欄位
        public string? KeyValue { get; set; } // 使用 KeyValue 作為 C# 屬性名稱
        public int? Times { get; set; }
        public string? Used { get; set; }
        public string? Type { get; set; }
        public string? Locked { get; set; }
        public string? Creator { get; set; }
        public string? Reseller { get; set; }
        public int Price { get; set; }

    }
   }