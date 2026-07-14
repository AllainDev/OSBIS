using OSBIS.Common;
using OSBIS.Models.Entities;
using OSBIS.Models.Enums;
using OSBIS.Repositories.Interfaces;
using OSBIS.Services.Interfaces;
using Serilog;

namespace OSBIS.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuditLogService _auditLogService;

        public ReviewService(IUnitOfWork uow, IAuditLogService auditLogService)
        {
            _uow = uow;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResult<Review>> GetByProductAsync(int productId, int pageNumber, int pageSize)
        {
            return await _uow.Reviews.GetByProductAsync(productId, pageNumber, pageSize);
        }

        public async Task<IReadOnlyList<Review>> GetByOrderAsync(int orderId)
        {
            return await _uow.Reviews.GetByOrderAsync(orderId);
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            return await _uow.Reviews.GetAverageRatingAsync(productId);
        }

        public async Task<bool> CanReviewAsync(int userId, int orderId, int productId)
        {
            // Check đã review chưa
            if (await _uow.Reviews.ExistsAsync(orderId, productId, userId))
                return false;

            // Check order thuộc user và đã Delivered/Completed
            var order = await _uow.Orders.GetWithDetailsAsync(orderId);
            if (order == null || order.UserId != userId) return false;

            if (order.OrderStatus != OrderStatus.Delivered && order.OrderStatus != OrderStatus.Completed)
                return false;

            // Check productId có trong order không
            return order.OrderDetails.Any(d => d.ProductId == productId);
        }

        public async Task<ReviewResult> CreateReviewAsync(int userId, int orderId, int productId, byte rating, string? comment)
        {
            try
            {
                if (rating < 1 || rating > 5)
                    return new ReviewResult { Success = false, Message = "Đánh giá phải từ 1 đến 5 sao." };

                var canReview = await CanReviewAsync(userId, orderId, productId);
                if (!canReview)
                    return new ReviewResult { Success = false, Message = "Bạn không thể đánh giá sản phẩm này." };

                await _uow.BeginTransactionAsync();

                var review = new Review
                {
                    UserId = userId,
                    OrderId = orderId,
                    ProductId = productId,
                    Rating = rating,
                    Comment = comment?.Trim(),
                    ReviewDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _uow.Reviews.AddAsync(review);
                await _uow.CommitTransactionAsync();

                await _auditLogService.LogAsync(
                    userId, null,
                    AuditAction.ReviewCreated,
                    description: $"Review created for product {productId} - {rating} stars",
                    isSuccess: true,
                    controller: "Customer/Review",
                    actionName: "Create"
                );

                return new ReviewResult { Success = true, Message = "Đánh giá thành công.", Review = review };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "CreateReview failed");
                return new ReviewResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }

        public async Task<ReviewResult> UpdateReviewAsync(int reviewId, int userId, byte rating, string? comment)
        {
            try
            {
                if (rating < 1 || rating > 5)
                    return new ReviewResult { Success = false, Message = "Đánh giá phải từ 1 đến 5 sao." };

                await _uow.BeginTransactionAsync();

                var review = await _uow.Reviews.GetByIdAsync(reviewId);
                if (review == null || review.UserId != userId)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ReviewResult { Success = false, Message = "Không tìm thấy đánh giá." };
                }

                review.Rating = rating;
                review.Comment = comment?.Trim();
                review.UpdatedAt = DateTime.UtcNow;
                _uow.Reviews.Update(review);

                await _uow.CommitTransactionAsync();

                return new ReviewResult { Success = true, Message = "Cập nhật đánh giá thành công.", Review = review };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "UpdateReview failed");
                return new ReviewResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }

        public async Task<ReviewResult> DeleteReviewAsync(int reviewId, int userId)
        {
            try
            {
                await _uow.BeginTransactionAsync();

                var review = await _uow.Reviews.GetByIdAsync(reviewId);
                if (review == null || review.UserId != userId)
                {
                    await _uow.RollbackTransactionAsync();
                    return new ReviewResult { Success = false, Message = "Không tìm thấy đánh giá." };
                }

                _uow.Reviews.Remove(review);
                await _uow.CommitTransactionAsync();

                return new ReviewResult { Success = true, Message = "Đã xóa đánh giá." };
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync();
                Log.Error(ex, "DeleteReview failed");
                return new ReviewResult { Success = false, Message = "Có lỗi xảy ra." };
            }
        }
    }
}
