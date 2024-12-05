using System;

namespace MemberService.Models
{
    public class Product
    {
        public int Id { get; set; } // 商品 ID，自動遞增
        public string Code { get; set; } // 商品編號
        public string Category { get; set; } // 商品分類
        public string Name { get; set; } // 商品名稱
        public DateTime Created_Time { get; set; } // 商品創建時間
        public int Price { get; set; } // 商品價格（整數）
        public string Base64Image { get; set; } // 商品圖片（Base64 字串）
        public int locked {  get; set; } // 是否上架，0為已下架，1為已上架
    }
}
