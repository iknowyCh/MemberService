namespace MemberService.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }  // 必填欄位
        public int? Times { get; set; }
        public int? Locked { get; set; }
        public string? IP { get; set; }        // 非必填欄位
        public string? HWID { get; set; }      // 非必填欄位
        public DateTime? Date { get; set; }
        public int Plan { get; set; }
        public int? ChangeTime { get; set; }
        public string? Creator { get; set; }   // 必填欄位
        public string? ResetToken { get; set; } // 密碼重設 Token
        public DateTime? TokenExpiration { get; set; } // Token 過期時間
    }


}
