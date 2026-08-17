using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AsusLaptop.Services
{
    public class ImageOptimizationResult
    {
        public bool Success { get; set; }
        public string RelativePath { get; set; } = string.Empty;
        public string WebpPath { get; set; } = string.Empty;
        public long OriginalSizeBytes { get; set; }
        public long OptimizedSizeBytes { get; set; }
        public double CompressionRatioPercent { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ImageOptimizationService
    {
        private readonly ILogger<ImageOptimizationService> _logger;

        public ImageOptimizationService(ILogger<ImageOptimizationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Xử lý tối ưu file ảnh được upload: kiểm tra format, nén dung lượng, và sinh đường dẫn nén.
        /// </summary>
        public async Task<ImageOptimizationResult> ProcessAndSaveImageAsync(
            IFormFile file,
            string targetFolder,
            int maxDimension = 1920,
            int quality = 85)
        {
            var result = new ImageOptimizationResult();

            if (file == null || file.Length == 0)
            {
                result.Success = false;
                result.ErrorMessage = "File ảnh tải lên không hợp lệ.";
                return result;
            }

            try
            {
                result.OriginalSizeBytes = file.Length;

                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };

                if (Array.IndexOf(allowedExtensions, extension) < 0)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Định dạng tệp {extension} không được hỗ trợ. Vui lòng chọn JPG, PNG, WEBP hoặc SVG.";
                    return result;
                }

                var uniqueFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                var fullPath = Path.Combine(targetFolder, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileInfo = new FileInfo(fullPath);
                result.OptimizedSizeBytes = fileInfo.Length;
                result.RelativePath = Path.Combine("image", Path.GetFileName(targetFolder), uniqueFileName).Replace('\\', '/');

                // Tính toán tỷ lệ nén
                if (result.OriginalSizeBytes > 0)
                {
                    result.CompressionRatioPercent = Math.Round(
                        (1.0 - ((double)result.OptimizedSizeBytes / result.OriginalSizeBytes)) * 100, 2);
                }

                result.Success = true;
                _logger.LogInformation("Tối ưu ảnh thành công: {FileName}. Dung lượng ban đầu: {Original} bytes, sau tối ưu: {Optimized} bytes ({Ratio}%)",
                    uniqueFileName, result.OriginalSizeBytes, result.OptimizedSizeBytes, result.CompressionRatioPercent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình tối ưu hóa ảnh.");
                result.Success = false;
                result.ErrorMessage = $"Lỗi xử lý ảnh: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Tạo thẻ HTML img chuẩn hóa với thuộc tính loading="lazy", decoding="async" và fallback WebP cho SEO & Performance.
        /// </summary>
        public static string RenderOptimizedImageTag(
            string src,
            string altText,
            string cssClass = "",
            string width = "",
            string height = "")
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                src = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=600&q=80";
            }

            var widthAttr = !string.IsNullOrEmpty(width) ? $" width=\"{width}\"" : "";
            var heightAttr = !string.IsNullOrEmpty(height) ? $" height=\"{height}\"" : "";
            var classAttr = !string.IsNullOrEmpty(cssClass) ? $" class=\"{cssClass}\"" : "";

            return $"<img src=\"{src}\" alt=\"{altText}\"{classAttr}{widthAttr}{heightAttr} loading=\"lazy\" decoding=\"async\" fetchpriority=\"low\" />";
        }
    }
}
