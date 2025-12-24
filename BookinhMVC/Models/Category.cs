using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [MaxLength(100)]
        [Display(Name = "Tên Danh mục")]
        public string CategoryName { get; set; }

        [MaxLength(100)]
        [Display(Name = "Slug Danh mục")]
        public string Slug { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation Property: Một Category có nhiều Article
        public ICollection<Article> Articles { get; set; }
    }
}