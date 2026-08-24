using System.ComponentModel.DataAnnotations;

namespace PetFeast.Models.Identity
{
    public class UserAddress
    {
        public int UserAddressId { get; set; }

        public string UserId { get; set; } = "";

        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [Display(Name = "Tên người nhận")]
        public string ReceiverName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố.")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string City { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn phường/xã.")]
        [Display(Name = "Phường/Xã")]
        public string Ward { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể.")]
        [Display(Name = "Địa chỉ cụ thể")]
        public string AddressDetail { get; set; } = "";

        public bool IsDefault { get; set; } = false;
    }
}
