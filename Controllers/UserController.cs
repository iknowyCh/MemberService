using Microsoft.AspNetCore.Mvc;
using MemberService.Data;
using MemberService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography; // 生成安全隨機數
using System.Net.Mail; //用於寄送 Email
using Microsoft.AspNetCore.Http; // 用於 Session 管理
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.AspNetCore.Identity.Data;

using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MemberService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly KeyServiceContext _context;

        public UsersController(KeyServiceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }



        // 註冊 API
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User model)
        {
            // 驗證傳入的資料
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { success = false, message = "請填寫完整的註冊資料" });
            }

            // 檢查用戶名稱是否已存在
            var existingUser = await _context.member.FirstOrDefaultAsync(m => m.Username == model.Username);
            if (existingUser != null)
            {
                return Conflict(new { success = false, message = "該用戶名稱已被註冊" });
            }

            // 設置預設值（例如創建者、計劃類型等）
            var newUser = new User
            {
                Username = model.Username,
                Password = model.Password,
                Plan = model.Plan > 0 ? model.Plan : 0, // 默認計劃類型
                Times = model.Times,
                Locked = 0, // 預設為未鎖定
                Creator = HttpContext.Session.GetString("UserRole") ?? "Unknown", // 從 Session 取得創建者
                Date = DateTime.UtcNow, // 設定當前時間
            };

            // 將用戶新增至資料表
            try
            {
                _context.member.Add(newUser);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "註冊成功，請登入！" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"註冊失敗：{ex.Message}");
            }
        }


        // 登入驗證 API
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            Console.WriteLine($"收到登入請求，使用者名稱: {model.Username}");

            if (string.IsNullOrEmpty(model?.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { success = false, message = "缺少登入資訊" });

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);
            if (admin != null)
            {
                SetSession(admin.Id, admin.Username);
                return Ok(new { success = true, role = "admin", message = "管理員登入成功" });
            }

            var member = await _context.member.FirstOrDefaultAsync(m => m.Username == model.Username && m.Password == model.Password);
            if (member != null)
            {
                SetSession(member.Id, member.Username);
                return Ok(new { success = true, role = "user", message = "一般用戶登入成功" });
            }

            return Unauthorized(new { success = false, message = "使用者名稱或密碼錯誤" });
        }

        // 登出 API
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { success = true, message = "登出成功" });
        }

        // 檢查 Session API
        [HttpGet("check-session")]
        public IActionResult CheckSession()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { success = false, message = "尚未登入" });

            return Ok(new { success = true, message = "已登入", userId });
        }

        // 獲取所有用戶資料的 API
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                var users = await _context.member.ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤：{ex.Message}");
            }
        }

        // 新增用戶 API
        [HttpPost]
        public async Task<ActionResult<User>> PostUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
                return BadRequest(new { success = false, message = "請求資料不完整，缺少必要欄位" });

            if (user.Plan == 1 && user.Times <= 0)
                return BadRequest(new { success = false, message = "次數用戶必須指定有效的次數" });

            if (user.Plan == 2 && user.Date == null)
                return BadRequest(new { success = false, message = "時效用戶必須指定有效的到期日期" });

            try
            {
                _context.member.Add(user);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetUserByUsername), new { username = user.Username }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"無法創建用戶：{ex.Message}");
            }
        }

        // 獲取指定創建者的用戶 API
        [HttpGet("users-by-creator")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersByCreator()
        {
            var creator = HttpContext.Session.GetString("UserRole");
            if (creator == null)
                return Unauthorized(new { success = false, message = "尚未登入" });

            var users = creator == "ss012932"
                ? await _context.member.ToListAsync()
                : await _context.member.Where(u => u.Creator == creator).ToListAsync();

            return Ok(users);
        }

        // 獲取當前登入使用者的 API
        [HttpGet("current-user")]
        public IActionResult GetCurrentUser()
        {
            var username = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { success = false, message = "未登入用戶。" });
            }

            return Ok(new { success = true, username });
        }


        // 獲取指定用戶資料 API
        [HttpGet("{username}")]
        public async Task<ActionResult<User>> GetUserByUsername(string username)
        {
            var user = await _context.member.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound(new { success = false, message = "找不到用戶" });

            return Ok(user);
        }

        // 更新用戶 API
        [HttpPut("{username}")]
        public async Task<IActionResult> UpdateUser(string username, [FromBody] User updatedUser)
        {
            var user = await _context.member.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound(new { success = false, message = "找不到用戶" });

            user.Password = updatedUser.Password;
            user.Plan = updatedUser.Plan;
            user.Times = updatedUser.Times;
            user.Date = updatedUser.Date;
            user.Locked = updatedUser.Locked;
            user.Creator = updatedUser.Creator;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "會員資料更新成功" });
            }

            catch (Exception ex)
            {
                return StatusCode(500, $"無法更新用戶 資料：{ex.Message}");
            }
        }

        // 刪除用戶 API
        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteUser(string username)
        {
            var user = await _context.member.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound(new { success = false, message = "找不到用戶" });

            _context.member.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "會員已刪除" });
        }

        // 獲取 Key 資料表的 API
        [HttpGet("key")]
        public async Task<ActionResult<IEnumerable<Key>>> GetKeys()
        {
            try
            {
                Console.WriteLine("開始從資料庫中取得 key 資料...");

                // 從 Session 取得登入用戶角色
                var creator = HttpContext.Session.GetString("UserRole");
                if (creator == null)
                {
                    return Unauthorized(new { success = false, message = "尚未登入" });
                }

                List<Key> keys;

                if (creator == "ss012932")
                {
                    // ss012932 可以查看所有卡號
                    keys = await _context.key.ToListAsync();
                    Console.WriteLine($"成功取得 {keys.Count} 筆資料 (查看所有卡號)");
                }
                else
                {
                    // 其他使用者只能查看自己創建的卡號
                    keys = await _context.key.Where(k => k.Creator == creator).ToListAsync();
                    Console.WriteLine($"成功取得 {keys.Count} 筆資料 (查看自己的卡號)");
                }

                return Ok(keys);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"錯誤：{ex.Message}");
                return StatusCode(500, $"無法獲取資料：{ex.Message}");
            }
        }

        // 設定 Session
        private void SetSession(int userId, string username)
        {
            HttpContext.Session.SetInt32("UserId", userId);
            HttpContext.Session.SetString("UserRole", username);
        }

        // 鎖定卡號
        [HttpPut("key/{id}/lock")]
        public async Task<IActionResult> LockKey(int id)
        {
            var key = await _context.key.FindAsync(id);
            if (key == null) return NotFound();

            key.Locked = "1"; // 將 Locked 設為 '1'
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 解除卡號鎖定
        [HttpPut("key/{id}/unlock")]
        public async Task<IActionResult> UnlockKey(int id)
        {
            var key = await _context.key.FindAsync(id);
            if (key == null) return NotFound();

            key.Locked = "0"; // 將 Locked 設為 '0'
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 刪除卡號
        [HttpDelete("key/{id}")]
        public async Task<IActionResult> DeleteKey(int id)
        {
            var key = await _context.key.FindAsync(id);
            if (key == null) return NotFound(new { success = false, message = "找不到該卡號" });

            _context.key.Remove(key);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "卡號已刪除" });
        }

        // 獲取所有管理員 API
        [HttpGet("admins")]
        public async Task<ActionResult<IEnumerable<Admin>>> GetAdmins()
        {
            try
            {
                // 從 Session 中獲取當前登入的使用者帳號
                var currentUser = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(currentUser))
                {
                    return Unauthorized(new { success = false, message = "尚未登入" });
                }

                List<Admin> admins;

                // 如果是特殊帳號 "ss012932"，返回所有管理員資料
                if (currentUser == "ss012932")
                {
                    admins = await _context.Admins.ToListAsync();
                }
                else
                {
                    // 否則，只返回 creator 為當前登入帳號的資料
                    admins = await _context.Admins.Where(a => a.Creator == currentUser).ToListAsync();
                }

                return Ok(admins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"無法獲取管理員資料：{ex.Message}");
            }
        }


        // 新增管理員
        [HttpPost("admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] Admin admin)
        {
            if (admin == null || string.IsNullOrEmpty(admin.Username) || string.IsNullOrEmpty(admin.Password))
                return BadRequest(new { success = false, message = "請求資料不完整，缺少必要欄位" });

            try
            {
                _context.Admins.Add(admin); // 假設 `_context.Admins` 是你的管理員資料表
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "管理員已成功新增！" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"新增管理員失敗：{ex.Message}");
            }
        }


        // 刪除管理員的 API
        [HttpDelete("admins/{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound(new { success = false, message = "找不到該管理員" });
            }

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "管理員已刪除" });
        }

        // 獲取特定管理員資料的 API
        [HttpGet("admins/{username}")]
        public async Task<ActionResult<Admin>> GetAdmin(string username)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
            if (admin == null)
            {
                return NotFound(new { success = false, message = "找不到管理員" });
            }
            return Ok(admin);
        }

        // 編輯管理員資料的 API
        [HttpPut("admins/{username}")]
        public async Task<IActionResult> UpdateAdmin(string username, [FromBody] Admin updatedAdmin)
        {
            if (updatedAdmin == null || username != updatedAdmin.Username)
            {
                return BadRequest(new { success = false, message = "請求資料不正確" });
            }

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
            if (admin == null)
            {
                return NotFound(new { success = false, message = "找不到管理員" });
            }

            // 更新管理員的資料
            admin.Password = updatedAdmin.Password;
            admin.Role = updatedAdmin.Role;
            admin.Locked = updatedAdmin.Locked;
            admin.Coins = updatedAdmin.Coins;
            admin.Sell = updatedAdmin.Sell;
            admin.Days = updatedAdmin.Days;
            admin.Weeks = updatedAdmin.Weeks;
            admin.Months = updatedAdmin.Months;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "管理員資料更新成功" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { success = false, message = $"更新失敗：{ex.Message}" });
            }
        }

        // 忘記密碼 - 生成重設密碼的 Token
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] Models.ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email))
                return BadRequest(new { success = false, message = "請輸入有效的帳號和 Email。" });

            var user = await _context.member.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return NotFound(new { success = false, message = "找不到用戶。" });

            // 生成 Reset Token
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                user.ResetToken = Convert.ToBase64String(tokenData);
            }

            user.TokenExpiration = DateTime.UtcNow.AddMinutes(3); // 確保 TokenExpiration 保存為 UTC 時間
            await _context.SaveChangesAsync();

            Console.WriteLine($"Token Expiration saved as: {user.TokenExpiration}");

            // 構建重設密碼的 URL
            var resetUrl = $"http://localhost:4200/newpassword?token={user.ResetToken}&username={user.Username}";

            // 發送 Email
            try
            {
                // 建立一個新的 MailMessage 物件，負責構建電子郵件訊息
                var mailMessage = new MailMessage
                {
                    // 設定發送者的電子郵件地址和顯示名稱
                    From = new MailAddress("your-email@gmail.com", "Please Reset Your Password！"),
                    Subject = "重設您的密碼",
                    Body = $@"
                            <p>親愛的 {user.Username}，您好</p>
                            <p>請點擊以下連結重設密碼：</p>
                            <a href='{resetUrl}'>進入重置密碼頁面</a>
                            <p>該連結有效期為 3 分鐘。</p>",
                    // 將 Body 設定為 HTML 格式，確保可以顯示 HTML 標籤
                    IsBodyHtml = true
                };

                mailMessage.To.Add(request.Email);

                using (var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("hsinyen320@gmail.com", "abby hcdd llmm afsr"),
                    EnableSsl = true,
                })
                {
                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "無法發送電子郵件。", error = ex.Message });
            }

            return Ok(new { success = true, message = "重設密碼的連結已發送至您的信箱。" });
        }


        // 驗證 ResetToken 並重設密碼
        [HttpGet("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest(new { success = false, message = "Token 無效。" });

            var user = await _context.member.FirstOrDefaultAsync(u => u.ResetToken == token);

            if (user == null)
                return NotFound(new { success = false, message = "Token 無效或不存在。" });

            // 確保時間使用 UTC
            if (user.TokenExpiration == null || user.TokenExpiration < DateTime.UtcNow)
            {
                return BadRequest(new { success = false, message = "此連結已失效，請重新寄送重設密碼信件。" });
            }

            return Ok(new { success = true, message = "Token 有效。" });
        }


        // 驗證 ResetToken
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
                return BadRequest(new { success = false, message = "請提供完整的請求資料。" });

            var user = await _context.member.FirstOrDefaultAsync(u => u.ResetToken == model.Token);
            if (user == null)
                return BadRequest(new { success = false, message = "Token 無效或已過期。" });

            if (user.TokenExpiration == null || user.TokenExpiration < DateTime.UtcNow)
                return BadRequest(new { success = false, message = "此連結已失效，請重新寄送重設密碼信件。" });

            user.Password = model.NewPassword;
            user.ResetToken = null;
            user.TokenExpiration = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "密碼重設成功。" });
        }

        // 獲取所有商品
        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            try
            {
                var products = await _context.product.ToListAsync();
                return Ok(products); // 返回所有商品資料
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"無法獲取商品資料: {ex.Message}" });
            }
        }

        // 新增商品
        [HttpPost("products")]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            if (product == null || string.IsNullOrEmpty(product.Name) || product.Price <= 0)
            {
                return BadRequest(new { success = false, message = "請提供有效的商品資料" });
            }

            try
            {
                _context.product.Add(product); // 新增商品
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "商品新增成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"新增商品失敗: {ex.Message}" });
            }
        }

        // 編輯商品
        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            if (updatedProduct == null)
            {
                return BadRequest(new { success = false, message = "請提供有效的商品資料" });
            }

            var product = await _context.product.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { success = false, message = "商品不存在" });
            }

            // 更新商品屬性
            product.Code = updatedProduct.Code;
            product.Category = updatedProduct.Category;
            product.Name = updatedProduct.Name;
            product.Price = updatedProduct.Price;
            product.Base64Image = updatedProduct.Base64Image;

            try
            {
                await _context.SaveChangesAsync(); // 儲存變更
                return Ok(new { success = true, message = "商品更新成功" });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"更新商品失敗： {ex.Message}" });
            }
        }

        // 刪除商品
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.product.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { success = false, message = "商品不存在" });
            }

            _context.product.Remove(product);

            try
            {
                await _context.SaveChangesAsync(); // 從資料表刪除商品
                return Ok(new { success = true, message = "商品刪除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"刪除商品失敗： {ex.Message}" });
            }
        }
    }
}
