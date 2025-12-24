using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookinhMVC.Models
{
    public class Article
    {
        [Key]
        public int ArticleId { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [MaxLength(255)]
        [Display(Name = "Tiêu đề Bài viết")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Slug không được để trống.")]
        [MaxLength(255)]
        [Display(Name = "URL Thân thiện (Slug)")]
        public string Slug { get; set; } // Dùng cho URL thân thiện

        [MaxLength(500)]
        [Display(Name = "Tóm tắt")]
        public string Summary { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        [Column(TypeName = "nvarchar(MAX)")]
        [Display(Name = "Nội dung chi tiết")]
        public string Content { get; set; }

        [MaxLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string FeatureImageUrl { get; set; }

        [Required]
        [Display(Name = "Ngày xuất bản")]
        [DataType(DataType.DateTime)]
        public DateTime PublishDate { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        public bool IsPublished { get; set; } = false;

        [Display(Name = "Lượt xem")]
        public int ViewsCount { get; set; } = 0;

        // --- Mối quan hệ (Foreign Keys) ---

        [Required(ErrorMessage = "Vui lòng chọn Tác giả.")]
        [Display(Name = "Tác giả")]
        public int AuthorId { get; set; }
        public NguoiDung Author { get; set; } // Navigation Property

        [Required(ErrorMessage = "Vui lòng chọn Danh mục.")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        public Category Category { get; set; } // Navigation Property
    }
}