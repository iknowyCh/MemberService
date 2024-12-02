namespace MemberService.Models
{
    public class Admin
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Role { get; set; }
        public int Locked { get; set; }
        public string? Creator { get; set; } // 可選欄位，允許空值
        public int Coins { get; set; }
        public int Sell { get; set; }
        public int Days { get; set; }
        public int Weeks { get; set; }
        public int Months { get; set; }
    }
}
