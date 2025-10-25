using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Openize.Heic.Decoder;

namespace PhotoSift
{
    /// <summary>
    /// High-performance wrapper class for loading HEIC/HEIF images using Openize.Heic
    /// Optimized for scrolling performance and memory efficiency
    /// </summary>
    public static class HeicWrapper
    {
        private const int MaxDisplaySize = 2048; // Maximum size for display optimization
        private const int ThumbnailSize = 512;   // Preferred thumbnail size
        private const int QuickPreviewSize = 256; // Size for very fast previews during scrolling
        private const int MicroPreviewSize = 128; // Ultra-fast micro previews
        
        // Cache for small preview images to speed up scrolling
        private static readonly ConcurrentDictionary<string, WeakReference> _previewCache = 
            new ConcurrentDictionary<string, WeakReference>();
        
        private const int MaxCacheSize = 50; // Maximum number of cached previews

        /// <summary>
        /// Load a HEIC/HEIF file optimized for the requested display size
        /// Uses smart caching and progressive loading for maximum performance
        /// </summary>
        /// <param name="filePath">Path to the HEIC/HEIF file</param>
        /// <param name="maxSize">Maximum dimension for the loaded image (0 = no limit)</param>
        /// <param name="quickPreview">If true, prioritize speed over quality for scrolling</param>
        /// <returns>Bitmap containing the image data</returns>
        public static Bitmap LoadHeic(string filePath, int maxSize = 0, bool quickPreview = false)
        {
            try
            {
                // For quick preview during scrolling, try cache first
                if (quickPreview)
                {
                    var cached = TryGetCachedPreview(filePath);
                    if (cached != null)
                        return cached;
                }

                // Clean up old cache entries occasionally
                if (_previewCache.Count > MaxCacheSize)
                {
                    CleanupPreviewCache();
                }

                using (var fileStream = File.OpenRead(filePath))
                {
                    var heicImage = HeicImage.Load(fileStream);
                    
                    Bitmap result;
                    
                    if (quickPreview)
                    {
                        // For scrolling performance, load smallest viable image
                        result = LoadQuickPreview(heicImage);
                        
                        // Cache the quick preview for faster subsequent access
                        CachePreview(filePath, result);
                    }
                    else
                    {
                        // For normal viewing, try thumbnail first then full image
                        var thumbnail = TryLoadThumbnail(heicImage, maxSize > 0 ? maxSize : ThumbnailSize);
                        if (thumbnail != null)
                        {
                            result = thumbnail;
                        }
                        else
                        {
                            result = LoadMainImageOptimized(heicImage, maxSize);
                        }
                    }
                    
                    return result;
                }
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
            try
            {
                using (var stream = new MemoryStream(data))
                {
                    var heicImage = HeicImage.Load(stream);
                    
                    // Try to use thumbnail first for better performance
                    var thumbnail = TryLoadThumbnail(heicImage);
                    if (thumbnail != null)
                        return thumbnail;
                    
                    // If no suitable thumbnail, load main image with optimization
                    return LoadMainImageOptimized(heicImage);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load HEIC/HEIF image from bytes: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load a HEIC file optimized for quick preview (loads thumbnail or downsampled image)
        /// </summary>
        /// <param name="filePath">Path to the HEIC/HEIF file</param>
        /// <returns>Optimized bitmap for quick preview</returns>
        public static Bitmap LoadHeicPreview(string filePath)
        {
            return LoadHeic(filePath, QuickPreviewSize, true);
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

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".heic" || extension == ".heif" || extension == ".heics" || extension == ".heifs";
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
                    
                    // Check for thumbnails
                    bool hasThumbnails = false;
                    foreach (var frame in heicImage.AllFrames.Values)
                    {
                        if (frame.IsImage && !frame.IsHidden && frame.Width <= ThumbnailSize && frame.Height <= ThumbnailSize)
                        {
                            hasThumbnails = true;
                            break;
                        }
                    }
                    
                    return ((int)heicImage.Width, (int)heicImage.Height, hasThumbnails);
                }
            }
            catch
            {
                return (0, 0, false);
            }
        }

        #region Private Performance Optimization Methods

        /// <summary>
        /// Try to get a cached preview image
        /// </summary>
        private static Bitmap TryGetCachedPreview(string filePath)
        {
            if (_previewCache.TryGetValue(filePath, out var weakRef))
            {
                if (weakRef.IsAlive && weakRef.Target is Bitmap bitmap)
                {
                    try
                    {
                        // Test if bitmap is still valid by accessing a property
                        var _ = bitmap.Width;
                        // Return a copy to avoid disposal issues
                        return new Bitmap(bitmap);
                    }
                    catch
                    {
                        // Bitmap is disposed or invalid, remove from cache
                        _previewCache.TryRemove(filePath, out _);
                    }
                }
                else
                {
                    // Remove dead reference
                    _previewCache.TryRemove(filePath, out _);
                }
            }
            return null;
        }

        /// <summary>
        /// Cache a preview image using weak references
        /// </summary>
        private static void CachePreview(string filePath, Bitmap bitmap)
        {
            if (bitmap != null)
            {
                try
                {
                    // Test if bitmap is valid by accessing a property
                    var _ = bitmap.Width;
                    // Store a copy to avoid disposal issues
                    var copy = new Bitmap(bitmap);
                    _previewCache.AddOrUpdate(filePath, new WeakReference(copy), (k, v) => new WeakReference(copy));
                }
                catch
                {
                    // Bitmap is invalid, don't cache it
                }
            }
        }

        /// <summary>
        /// Clean up old cache entries
        /// </summary>
        private static void CleanupPreviewCache()
        {
            var deadKeys = new List<string>();
            foreach (var kvp in _previewCache)
            {
                if (!kvp.Value.IsAlive)
                {
                    deadKeys.Add(kvp.Key);
                }
                else if (kvp.Value.Target is Bitmap bmp)
                {
                    try
                    {
                        // Test if bitmap is still valid
                        var _ = bmp.Width;
                    }
                    catch
                    {
                        // Bitmap is disposed or invalid
                        deadKeys.Add(kvp.Key);
                    }
                }
            }
            
            foreach (var key in deadKeys)
            {
                _previewCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Load a very quick preview optimized for scrolling performance
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <returns>Quick preview bitmap</returns>
        private static Bitmap LoadQuickPreview(HeicImage heicImage)
        {
            try
            {
                // First try to find a very small thumbnail for ultra-fast loading
                foreach (var frame in heicImage.AllFrames.Values)
                {
                    if (!frame.IsImage || frame.IsHidden)
                        continue;
                    
                    var width = (int)frame.Width;
                    var height = (int)frame.Height;
                    
                    // Look for very small thumbnails first (priority for speed)
                    if (width <= QuickPreviewSize && height <= QuickPreviewSize && width >= 64 && height >= 64)
                    {
                        var quickPixels = frame.GetInt32Array(Openize.Heic.Decoder.PixelFormat.Argb32);
                        if (quickPixels != null)
                        {
                            return CreateBitmapFast(quickPixels, width, height);
                        }
                    }
                }
                
                // If no small thumbnail, create a heavily downsampled version
                var originalWidth = (int)heicImage.Width;
                var originalHeight = (int)heicImage.Height;
                
                // Calculate very aggressive downsampling for scrolling speed
                double scale = Math.Min((double)QuickPreviewSize / originalWidth, (double)QuickPreviewSize / originalHeight);
                scale = Math.Min(scale, 0.25); // Cap at 25% of original for speed
                
                int previewWidth = Math.Max(64, (int)(originalWidth * scale));
                int previewHeight = Math.Max(64, (int)(originalHeight * scale));
                
                // Load only a small center crop for maximum speed
                var cropX = Math.Max(0, (originalWidth - previewWidth * 2) / 2);
                var cropY = Math.Max(0, (originalHeight - previewHeight * 2) / 2);
                var cropRect = new Rectangle(cropX, cropY, 
                    Math.Min(previewWidth * 2, originalWidth), 
                    Math.Min(previewHeight * 2, originalHeight));
                
                var cropPixels = heicImage.GetInt32Array(Openize.Heic.Decoder.PixelFormat.Argb32, cropRect);
                var croppedBitmap = CreateBitmapFast(cropPixels, cropRect.Width, cropRect.Height);
                
                // If we need to scale it down further, do it quickly
                if (cropRect.Width > previewWidth || cropRect.Height > previewHeight)
                {
                    var scaledBitmap = ScaleBitmapFast(croppedBitmap, previewWidth, previewHeight);
                    croppedBitmap.Dispose();
                    return scaledBitmap;
                }
                
                return croppedBitmap;
            }
            catch
            {
                // If everything fails, return a minimal placeholder
                return new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
        }

        /// <summary>
        /// Try to load a suitable thumbnail from the HEIC image
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <param name="maxSize">Maximum size for the thumbnail</param>
        /// <returns>Thumbnail bitmap or null if none suitable</returns>
        private static Bitmap TryLoadThumbnail(HeicImage heicImage, int maxSize = ThumbnailSize)
        {
            try
            {
                // Look for auxiliary frames that might be thumbnails
                foreach (var frame in heicImage.AllFrames.Values)
                {
                    // Skip non-image frames
                    if (!frame.IsImage || frame.IsHidden)
                        continue;
                    
                    var width = (int)frame.Width;
                    var height = (int)frame.Height;
                    
                    // Check if this frame is a reasonable thumbnail size
                    if (width >= 128 && width <= maxSize && height >= 128 && height <= maxSize)
                    {
                        var pixels = frame.GetInt32Array(Openize.Heic.Decoder.PixelFormat.Argb32);
                        if (pixels != null)
                        {
                            return CreateBitmapFast(pixels, width, height);
                        }
                    }
                }
                
                return null; // No suitable thumbnail found
            }
            catch
            {
                return null; // Error loading thumbnail, fall back to main image
            }
        }

        /// <summary>
        /// Load the main image with size optimization
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <param name="maxSize">Maximum size for the image (0 = use default MaxDisplaySize)</param>
        /// <returns>Optimized bitmap</returns>
        private static Bitmap LoadMainImageOptimized(HeicImage heicImage, int maxSize = 0)
        {
            var originalWidth = (int)heicImage.Width;
            var originalHeight = (int)heicImage.Height;
            
            // Use provided maxSize or default
            int targetMaxSize = maxSize > 0 ? maxSize : MaxDisplaySize;
            
            // Check if we need to downsample for performance
            if (originalWidth > targetMaxSize || originalHeight > targetMaxSize)
            {
                return LoadDownsampledImage(heicImage, originalWidth, originalHeight, targetMaxSize);
            }
            
            // Load at original size using fast method
            var pixels = heicImage.GetInt32Array(Openize.Heic.Decoder.PixelFormat.Argb32);
            return CreateBitmapFast(pixels, originalWidth, originalHeight);
        }

        /// <summary>
        /// Load a downsampled version of the image for better performance
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <param name="originalWidth">Original image width</param>
        /// <param name="originalHeight">Original image height</param>
        /// <param name="maxSize">Maximum size for the downsampled image</param>
        /// <returns>Downsampled bitmap</returns>
        private static Bitmap LoadDownsampledImage(HeicImage heicImage, int originalWidth, int originalHeight, int maxSize = MaxDisplaySize)
        {
            // Calculate optimal size maintaining aspect ratio
            double scale = Math.Min((double)maxSize / originalWidth, (double)maxSize / originalHeight);
            int targetWidth = (int)(originalWidth * scale);
            int targetHeight = (int)(originalHeight * scale);
            
            // For very large images, load a region first for speed
            if (originalWidth > maxSize * 3 || originalHeight > maxSize * 3)
            {
                // Load center crop at a reasonable size
                var cropSize = Math.Min(maxSize * 2, Math.Min(originalWidth, originalHeight));
                var cropX = (originalWidth - cropSize) / 2;
                var cropY = (originalHeight - cropSize) / 2;
                var cropRect = new Rectangle(cropX, cropY, cropSize, cropSize);
                
                var croppedPixels = heicImage.GetInt32Array(Openize.Heic.Decoder.PixelFormat.Argb32, cropRect);
                var croppedBitmap = CreateBitmapFast(croppedPixels, cropSize, cropSize);
                
                // Scale the cropped version
                if (cropSize != targetWidth || cropSize != targetHeight)
                {
                    var scaledBitmap = ScaleBitmapFast(croppedBitmap, targetWidth, targetHeight);
                    croppedBitmap.Dispose();
                    return scaledBitmap;
                }
                
                return croppedBitmap;
            }
            else
            {
                // For moderately sized images, load full and scale
                var pixels = heicImage.GetInt32Array(Openize.Heic.Decoder.PixelFormat.Argb32);
                var fullBitmap = CreateBitmapFast(pixels, originalWidth, originalHeight);
                
                if (targetWidth != originalWidth || targetHeight != originalHeight)
                {
                    var scaledBitmap = ScaleBitmapFast(fullBitmap, targetWidth, targetHeight);
                    fullBitmap.Dispose();
                    return scaledBitmap;
                }
                
                return fullBitmap;
            }
        }

        /// <summary>
        /// Fast bitmap scaling using optimized Graphics settings
        /// </summary>
        private static Bitmap ScaleBitmapFast(Bitmap original, int targetWidth, int targetHeight)
        {
            var scaled = new Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(scaled))
            {
                // Optimized settings for speed vs quality balance
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                
                graphics.DrawImage(original, 0, 0, targetWidth, targetHeight);
            }
            return scaled;
        }

        /// <summary>
        /// Create a bitmap from pixel data using fast memory operations
        /// </summary>
        /// <param name="pixels">ARGB pixel data</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>Bitmap containing the image data</returns>
        private static Bitmap CreateBitmapFast(int[] pixels, int width, int height)
        {
            // Validate input
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length < width * height)
                throw new ArgumentException("Invalid pixel data or dimensions");
            
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            // Lock the bitmap's bits for direct memory access
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                // Calculate the actual number of pixels to copy (in case of partial arrays)
                int pixelsToCopy = Math.Min(pixels.Length, width * height);
                
                // Use Marshal.Copy for fastest memory transfer
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, pixelsToCopy);
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            return bitmap;
        }

        #endregion
    }
}
