using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBIS.Models.Enums;
using OSBIS.Models.ViewModels.Cart;

namespace OSBIS.Models.ViewModels.Checkout
{
    /// <summary>VM cho trang Checkout: cart items + shipping address + voucher + payment method + totals.</summary>
    public class CheckoutViewModel
    {
        public CartSummaryViewModel Cart { get; set; } = new();

        // Snap totals (re-compute tại server khi PlaceOrder)
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalWeight { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(500)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        [StringLength(100)]
        [Display(Name = "Người nhận")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã giảm giá")]
        public string? VoucherCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        // Bank info (lấy từ SystemConfig, hiển thị khi chọn BankTransfer)
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }

        // Voucher đã validate (filled programmatically)
        public string? VoucherError { get; set; }
        public decimal? VoucherDiscountPreview { get; set; }
    }
}
