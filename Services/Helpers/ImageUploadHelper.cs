using System.ComponentModel.DataAnnotations;

namespace OSBIS.Services.Helpers
{
    /// <summary>
    /// Kết quả upload ảnh.
    /// </summary>
    public class ImageUploadResult
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? ErrorMessage { get; set; }

        public static ImageUploadResult Ok(string url) => new() { Success = true, Url = url };
        public static ImageUploadResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };
    }

    /// <summary>
    /// Helper upload ảnh lên wwwroot. OWASP A10 (SSRF) + validate extension/size.
    /// </summary>
    public class ImageUploadHelper
    {
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private readonly string _webRootPath;

        public ImageUploadHelper(string webRootPath)
        {
            _webRootPath = webRootPath;
        }

        public async Task<ImageUploadResult> UploadProductImageAsync(
            IFormFile file, string subFolder = "products")
        {
            if (file == null || file.Length == 0)
                return ImageUploadResult.Fail("File rỗng");

            // OWASP A10 — chống upload file độc hại: check extension
            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return ImageUploadResult.Fail("Chỉ chấp nhận ảnh .jpg/.jpeg/.png/.webp");

            if (!AllowedContentTypes.Contains(file.ContentType))
                return ImageUploadResult.Fail("Content-type không hợp lệ");

            if (file.Length > MaxFileSize)
                return ImageUploadResult.Fail("Ảnh tối đa 5MB");

            // Tạo folder wwwroot/uploads/{subFolder}
            var folder = Path.Combine(_webRootPath, "uploads", subFolder);
            Directory.CreateDirectory(folder);

            // Tên file unique
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var absolutePath = Path.Combine(folder, fileName);

            try
            {
                using var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                return ImageUploadResult.Fail($"Lỗi ghi file: {ex.Message}");
            }

            // URL relative để lưu DB và hiển thị trên web
            var url = $"/uploads/{subFolder}/{fileName}";
            return ImageUploadResult.Ok(url);
        }

        public bool DeleteImage(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) return false;
            if (!relativeUrl.StartsWith("/uploads/")) return false;

            var absPath = Path.Combine(_webRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absPath)) return false;

            try
            {
                File.Delete(absPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
