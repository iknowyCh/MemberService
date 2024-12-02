namespace MemberService.Models
{
    public class ForgotPasswordRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }    // 顧客填寫的臨時 Email
    }
}
