using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? DoctorId { get; set; }
        public string? Category { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Answer { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }

        public NguoiDung? User { get; set; }
        public BacSi? Doctor { get; set; }
    }
}