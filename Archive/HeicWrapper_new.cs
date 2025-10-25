using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Openize.Heic.Decoder;

namespace PhotoSift
{
    /// <summary>
    /// High-performance wrapper class for loading HEIC/HEIF images
    /// Uses Windows native WIC support first, then falls back to Openize.Heic
    /// </summary>
    public static class HeicWrapper
    {
        // Simple cache for frequently accessed images
        private static readonly ConcurrentDictionary<string, WeakReference> _cache = 
            new ConcurrentDictionary<string, WeakReference>();
        
        private const int MaxCacheSize = 20; // Conservative cache size

        /// <summary>
        /// Load a HEIC/HEIF file optimized for display
        /// </summary>
        /// <param name="filePath">Path to the HEIC/HEIF file</param>
        /// <param name="maxSize">Maximum dimension for the loaded image (0 = no limit)</param>
        /// <param name="quickPreview">If true, prioritize speed over quality</param>
        /// <returns>Bitmap containing the image data</returns>
        public static Bitmap LoadHeic(string filePath, int maxSize = 0, bool quickPreview = false)
        {
            try
            {
                // Check cache first
                string cacheKey = $"{filePath}_{maxSize}_{quickPreview}";
                if (_cache.TryGetValue(cacheKey, out var weakRef) && weakRef.IsAlive)
                {
                    if (weakRef.Target is Bitmap cachedBitmap)
                    {
                        return new Bitmap(cachedBitmap); // Return a copy
                    }
                }

                Bitmap result = null;
                
                // Try Windows native first (much faster and uses less CPU)
                try
                {
                    result = LoadWithWindowsNative(filePath, maxSize, quickPreview);
                }
                catch
                {
                    // Windows native failed, fall back to Openize
                    result = LoadWithOpenize(filePath, maxSize, quickPreview);
                }
                
                if (result != null)
                {
                    CacheImage(cacheKey, result);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load HEIC/HEIF image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load a HEIC/HEIF file and convert it to a System.Drawing.Bitmap (legacy method)
        /// </summary>
        /// <param name="filePath">Path to the HEIC/HEIF file</param>
        /// <returns>Bitmap containing the image data</returns>
        public static Bitmap LoadHeic(string filePath)
        {
            return LoadHeic(filePath, 0, false);
        }

        /// <summary>
        /// Load HEIC/HEIF from byte array and convert it to a System.Drawing.Bitmap
        /// </summary>
        /// <param name="data">Byte array containing HEIC/HEIF data</param>
        /// <returns>Bitmap containing the image data</returns>
        public static Bitmap LoadHeicFromBytes(byte[] data)
        {
            // For byte arrays, use Openize.Heic directly
            return LoadWithOpenizeFromBytes(data);
        }

        /// <summary>
        /// Load a HEIC file optimized for quick preview
        /// </summary>
        /// <param name="filePath">Path to the HEIC/HEIF file</param>
        /// <returns>Optimized bitmap for quick preview</returns>
        public static Bitmap LoadHeicPreview(string filePath)
        {
            return LoadHeic(filePath, 512, true);
        }

        /// <summary>
        /// Check if the file extension indicates a HEIC/HEIF file
        /// </summary>
        /// <param name="filePath">File path to check</param>
        /// <returns>True if the file is likely a HEIC/HEIF file</returns>
        public static bool IsHeicFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".heic" || extension == ".heif";
        }

        /// <summary>
        /// Get basic information about a HEIC file without fully loading it
        /// </summary>
        /// <param name="filePath">Path to the HEIC/HEIF file</param>
        /// <returns>Tuple with width, height, and whether thumbnails are available</returns>
        public static (int width, int height, bool hasThumbnails) GetHeicInfo(string filePath)
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    var heicImage = HeicImage.Load(fileStream);
                    return (heicImage.Width, heicImage.Height, heicImage.GetThumbnailIds()?.Length > 0);
                }
            }
            catch
            {
                return (0, 0, false);
            }
        }

        #region Private Methods

        /// <summary>
        /// Try to load HEIC using Windows native WIC support (much faster)
        /// </summary>
        private static Bitmap LoadWithWindowsNative(string filePath, int maxSize, bool quickPreview)
        {
            // Try using Image.FromFile first - Windows 10+ has native HEIC support
            using (var tempImage = Image.FromFile(filePath))
            {
                if (tempImage != null)
                {
                    int targetMaxSize = maxSize;
                    
                    // For quick preview, use smaller size to reduce CPU usage
                    if (quickPreview && (targetMaxSize == 0 || targetMaxSize > 512))
                    {
                        targetMaxSize = 512;
                    }
                    
                    // If maxSize is specified, resize efficiently
                    if (targetMaxSize > 0 && (tempImage.Width > targetMaxSize || tempImage.Height > targetMaxSize))
                    {
                        var scale = Math.Min((double)targetMaxSize / tempImage.Width, (double)targetMaxSize / tempImage.Height);
                        var newWidth = (int)(tempImage.Width * scale);
                        var newHeight = (int)(tempImage.Height * scale);
                        
                        var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
                        using (var graphics = Graphics.FromImage(resized))
                        {
                            // Use faster interpolation for quick preview
                            graphics.InterpolationMode = quickPreview ? 
                                InterpolationMode.Bilinear : InterpolationMode.HighQualityBicubic;
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.SmoothingMode = SmoothingMode.HighSpeed;
                            graphics.DrawImage(tempImage, 0, 0, newWidth, newHeight);
                        }
                        return resized;
                    }
                    else
                    {
                        return new Bitmap(tempImage);
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Load HEIC using Openize.Heic library with optimizations
        /// </summary>
        private static Bitmap LoadWithOpenize(string filePath, int maxSize, bool quickPreview)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var heicImage = HeicImage.Load(fileStream);
                
                // For quick preview, try thumbnails first
                if (quickPreview)
                {
                    var thumbnail = TryLoadThumbnail(heicImage, maxSize > 0 ? Math.Min(maxSize, 512) : 512);
                    if (thumbnail != null)
                        return thumbnail;
                }
                
                // Load main image with size constraints
                return LoadMainImage(heicImage, maxSize, quickPreview);
            }
        }

        /// <summary>
        /// Load HEIC from byte array using Openize.Heic
        /// </summary>
        private static Bitmap LoadWithOpenizeFromBytes(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var heicImage = HeicImage.Load(stream);
                return LoadMainImage(heicImage, 0, false);
            }
        }

        /// <summary>
        /// Try to load thumbnail from HEIC image
        /// </summary>
        private static Bitmap TryLoadThumbnail(HeicImage heicImage, int maxSize = 512)
        {
            try
            {
                var thumbnailIds = heicImage.GetThumbnailIds();
                if (thumbnailIds?.Length > 0)
                {
                    // Use the first available thumbnail
                    var thumbnailImage = heicImage.GetThumbnail(thumbnailIds[0]);
                    var bitmap = ConvertToBitmap(thumbnailImage);
                    
                    // If thumbnail is larger than maxSize, resize it
                    if (maxSize > 0 && (bitmap.Width > maxSize || bitmap.Height > maxSize))
                    {
                        return ResizeBitmap(bitmap, maxSize, true);
                    }
                    
                    return bitmap;
                }
            }
            catch
            {
                // Thumbnail loading failed
            }
            
            return null;
        }

        /// <summary>
        /// Load the main HEIC image with optimizations
        /// </summary>
        private static Bitmap LoadMainImage(HeicImage heicImage, int maxSize, bool quickPreview)
        {
            var bitmap = ConvertToBitmap(heicImage);
            
            // Apply size constraints if needed
            if (maxSize > 0 && (bitmap.Width > maxSize || bitmap.Height > maxSize))
            {
                return ResizeBitmap(bitmap, maxSize, quickPreview);
            }
            
            return bitmap;
        }

        /// <summary>
        /// Convert HeicImage to Bitmap efficiently
        /// </summary>
        private static Bitmap ConvertToBitmap(HeicImage heicImage)
        {
            var width = heicImage.Width;
            var height = heicImage.Height;
            
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), 
                ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            
            try
            {
                // Get RGB data from HEIC
                var rgbData = heicImage.GetByteArray(HeicColorspace.Rgb, HeicChroma.Interleaved_Rgb);
                
                // Copy data efficiently
                int stride = bitmapData.Stride;
                int pixelSize = 3; // RGB = 3 bytes per pixel
                
                unsafe
                {
                    byte* ptr = (byte*)bitmapData.Scan0;
                    int srcIndex = 0;
                    
                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + (y * stride);
                        for (int x = 0; x < width; x++)
                        {
                            if (srcIndex + 2 < rgbData.Length)
                            {
                                // BGR format for bitmap (reverse of RGB)
                                row[x * pixelSize + 0] = rgbData[srcIndex + 2]; // B
                                row[x * pixelSize + 1] = rgbData[srcIndex + 1]; // G
                                row[x * pixelSize + 2] = rgbData[srcIndex + 0]; // R
                                srcIndex += 3;
                            }
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            
            return bitmap;
        }

        /// <summary>
        /// Resize bitmap efficiently
        /// </summary>
        private static Bitmap ResizeBitmap(Bitmap original, int maxSize, bool quickPreview)
        {
            var scale = Math.Min((double)maxSize / original.Width, (double)maxSize / original.Height);
            var newWidth = (int)(original.Width * scale);
            var newHeight = (int)(original.Height * scale);
            
            var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            
            using (var graphics = Graphics.FromImage(resized))
            {
                // Use faster settings for quick preview
                if (quickPreview)
                {
                    graphics.InterpolationMode = InterpolationMode.Bilinear;
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                }
                else
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                }
                
                graphics.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            
            original.Dispose();
            return resized;
        }

        /// <summary>
        /// Cache an image using weak references
        /// </summary>
        private static void CacheImage(string key, Bitmap bitmap)
        {
            if (bitmap == null) return;
            
            // Clean up cache if too large
            if (_cache.Count > MaxCacheSize)
            {
                CleanupCache();
            }
            
            _cache.AddOrUpdate(key, new WeakReference(bitmap), (k, v) => new WeakReference(bitmap));
        }

        /// <summary>
        /// Clean up dead cache entries
        /// </summary>
        private static void CleanupCache()
        {
            var deadKeys = new List<string>();
            
            foreach (var kvp in _cache)
            {
                if (!kvp.Value.IsAlive)
                {
                    deadKeys.Add(kvp.Key);
                }
            }
            
            foreach (var key in deadKeys)
            {
                _cache.TryRemove(key, out _);
            }
            
            // If still too many, remove some more
            if (_cache.Count > MaxCacheSize)
            {
                var keysToRemove = _cache.Keys.Take(_cache.Count - MaxCacheSize / 2).ToList();
                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        #endregion
    }
}
