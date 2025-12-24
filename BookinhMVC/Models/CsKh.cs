using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    public class CsKh
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
