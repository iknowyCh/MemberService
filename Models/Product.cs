namespace MemberService.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Price { get; set; }
        public string Base64Image { get; set; }
    }
}
