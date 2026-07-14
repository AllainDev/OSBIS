namespace OSBIS.Models.Enums
{
    /// <summary>
    /// Phương thức thanh toán
    /// </summary>
    public enum PaymentMethod : byte
    {
        COD = 0,            // Thanh toán khi nhận hàng
        BankTransfer = 1,   // Chuyển khoản ngân hàng
        VNPay = 2,          // VNPay
        MoMo = 3,           // MoMo
        ZaloPay = 4,        // ZaloPay
        CreditCard = 5      // Thẻ tín dụng
    }
}