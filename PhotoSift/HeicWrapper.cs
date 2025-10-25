using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
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
        private const int QuickPreviewSize = 2048; // Size for very fast previews during scrolling
        
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
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            // For quick previews, always try the cache first
            if (quickPreview)
            {
                var cachedPreview = TryGetCachedPreview(filePath);
                if (cachedPreview != null)
                {
                    Console.WriteLine($"Loaded HEIC from cache: {Path.GetFileName(filePath)}");
                    return cachedPreview;
                }
            }

            // --- WIC DECODER (FAST PATH) ---
            try
            {
                // Attempt to use the Windows Imaging Component (WIC) decoder first.
                var bitmap = LoadWithWIC(filePath, maxSize, quickPreview);
                if (bitmap != null)
                {
                    Console.WriteLine($"Loaded HEIC with WIC decoder: {Path.GetFileName(filePath)}");
                    if (quickPreview)
                    {
                        CachePreview(filePath, bitmap);
                    }
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WIC decoder failed for {Path.GetFileName(filePath)}: {ex.Message}. Falling back to GDI+.");
            }

            // --- GDI+ DECODER (FALLBACK PATH 1) ---
            try
            {
                // Attempt to use the fast, built-in Windows decoder first.
                using (var image = Image.FromFile(filePath))
                {
                    Console.WriteLine($"Loaded HEIC with GDI+ decoder: {Path.GetFileName(filePath)}");
                    var processedImage = ProcessLoadedImage(image, maxSize, quickPreview);
                    if (quickPreview)
                    {
                        CachePreview(filePath, processedImage);
                    }
                    return processedImage;
                }
            }
            catch (OutOfMemoryException)
            {
                // This is the expected exception when GDI+ doesn't support the format.
                // Fall through to the Openize.Heic library.
                Console.WriteLine($"GDI+ decoder failed for {Path.GetFileName(filePath)}. Falling back to Openize library.");
            }
            catch (Exception ex)
            {
                // Some other error occurred with the native loader.
                Console.WriteLine($"GDI+ decoder threw an unexpected error for {Path.GetFileName(filePath)}: {ex.Message}. Falling back.");
            }

            // --- OPENIZE.HEIC DECODER (FALLBACK PATH 2) ---
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    Console.WriteLine($"Loaded HEIC with OPENIZE decoder: {Path.GetFileName(filePath)}");
                    var heicImage = HeicImage.Load(fileStream);
                    Bitmap resultBitmap;
                    if (quickPreview)
                    {
                        resultBitmap = LoadQuickPreview(heicImage);
                        if (resultBitmap != null)
                            CachePreview(filePath, resultBitmap);
                    }
                    else
                    {
                        resultBitmap = LoadMainImageOptimized(heicImage, maxSize);
                    }
                    return resultBitmap;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HEIC Loading ERROR for {filePath}: {ex.Message}");
                return null;
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
            if (data == null || data.Length == 0)
                return null;

            try
            {
                using (var stream = new MemoryStream(data))
                {
                    var heicImage = HeicImage.Load(stream);
                    return LoadMainImageOptimized(heicImage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HEIC from bytes ERROR: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load a HEIC file optimized for quick preview (loads thumbnail or downsampled image)
        /// </summary>
        /// <param name="filePath">Path to the HEIC/HEIF file</param>
        /// <returns>Optimized bitmap for quick preview</returns>
        public static Bitmap LoadHeicPreview(string filePath)
        {
            return LoadHeic(filePath, 0, true);
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
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var heicImage = HeicImage.Load(fileStream);
                    return ((int)heicImage.Width, (int)heicImage.Height, false); // Thumbnails not directly supported in this version of the logic
                }
            }
            catch
            {
                return (0, 0, false);
            }
        }

        #region Private Performance Optimization Methods

        /// <summary>
        /// Load HEIC file using Windows Imaging Component (WIC) directly
        /// This bypasses GDI+ limitations and accesses the Microsoft Store HEIF extension
        /// </summary>
        private static Bitmap LoadWithWIC(string filePath, int maxSize = 0, bool quickPreview = false)
        {
            try
            {
                // Create a BitmapDecoder using WIC
                var uri = new Uri(filePath, UriKind.Absolute);
                var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                
                if (decoder.Frames.Count > 0)
                {
                    var frame = decoder.Frames[0];
                    
                    // Determine target size
                    int targetMaxSize = maxSize;
                    if (quickPreview)
                    {
                        targetMaxSize = QuickPreviewSize;
                    }
                    else if (targetMaxSize == 0)
                    {
                        targetMaxSize = MaxDisplaySize;
                    }
                    
                    // Apply scaling if needed
                    BitmapSource processedFrame = frame;
                    if (targetMaxSize > 0 && (frame.PixelWidth > targetMaxSize || frame.PixelHeight > targetMaxSize))
                    {
                        double scale = Math.Min((double)targetMaxSize / frame.PixelWidth, (double)targetMaxSize / frame.PixelHeight);
                        int newWidth = (int)(frame.PixelWidth * scale);
                        int newHeight = (int)(frame.PixelHeight * scale);
                        
                        var transform = new ScaleTransform(scale, scale);
                        processedFrame = new TransformedBitmap(frame, transform);
                    }
                    
                    // Convert WPF BitmapSource to GDI+ Bitmap
                    return ConvertBitmapSourceToBitmap(processedFrame);
                }
            }
            catch (Exception ex)
            {
                // WIC failed - this is expected if the HEIF extension isn't properly installed/registered
                Console.WriteLine($"WIC decoder error: {ex.Message}");
                return null;
            }
            
            return null;
        }

        /// <summary>
        /// Convert WPF BitmapSource to GDI+ Bitmap
        /// </summary>
        private static Bitmap ConvertBitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            // Ensure the pixel format is compatible
            if (bitmapSource.Format != System.Windows.Media.PixelFormats.Bgr24 &&
                bitmapSource.Format != System.Windows.Media.PixelFormats.Bgra32)
            {
                // Convert to a compatible format
                var formatConvertedBitmap = new FormatConvertedBitmap();
                formatConvertedBitmap.BeginInit();
                formatConvertedBitmap.Source = bitmapSource;
                formatConvertedBitmap.DestinationFormat = System.Windows.Media.PixelFormats.Bgr24;
                formatConvertedBitmap.EndInit();
                bitmapSource = formatConvertedBitmap;
            }

            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), bitmapData.Scan0, bitmapData.Height * bitmapData.Stride, bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        /// <summary>
        /// Process a loaded image from the native decoder, resizing if necessary.
        /// </summary>
        // private static Bitmap ProcessLoadedImage(Image image, int maxSize, bool quickPreview)
        // {
        //     int targetMaxSize = maxSize;
        //     if (quickPreview && (targetMaxSize == 0 || targetMaxSize > ThumbnailSize))
        //     {
        //         targetMaxSize = QuickPreviewSize;
        //     }
        //     else if (targetMaxSize == 0)
        //     {
        //         targetMaxSize = MaxDisplaySize;
        //     }

        //     // If no resizing is needed, we still draw it to a new bitmap to ensure a standard pixel format
        //     // and to decouple from the original image stream.
        //     if (image.Width <= targetMaxSize && image.Height <= targetMaxSize)
        //     {
        //         var newBitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        //         using (var graphics = Graphics.FromImage(newBitmap))
        //         {
        //             graphics.DrawImage(image, 0, 0, image.Width, image.Height);
        //         }
        //         return newBitmap;
        //     }

        //     // If resizing is needed, use the existing ResizeBitmap function
        //     var tempBitmap = new Bitmap(image);
        //     var resized = ResizeBitmap(tempBitmap, targetMaxSize, quickPreview);
        //     tempBitmap.Dispose();
        //     return resized;
        // }

        // ...existing code...
        /// <summary>
        /// Process a loaded image (resize if needed) and ensure correct pixel format
        /// </summary>
        private static Bitmap ProcessLoadedImage(Image image, int maxSize, bool quickPreview)
        {
            // Determine target size:
            int targetMaxSize;
            if (maxSize > 0)
            {
                targetMaxSize = maxSize;
            }
            else if (quickPreview)
            {
                targetMaxSize = ThumbnailSize; // small for scrolling
            }
            else
            {
                targetMaxSize = MaxDisplaySize; // full viewing
            }

            // If image already fits, copy to standard pixel format and return
            if (image.Width <= targetMaxSize && image.Height <= targetMaxSize)
            {
                var bmp = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                }
                return bmp;
            }

            // Otherwise resize while preserving aspect ratio
            int newWidth, newHeight;
            if (image.Width > image.Height)
            {
                newWidth = targetMaxSize;
                newHeight = (int)(image.Height * (targetMaxSize / (double)image.Width));
            }
            else
            {
                newHeight = targetMaxSize;
                newWidth = (int)(image.Width * (targetMaxSize / (double)image.Height));
            }

            var tmp = new Bitmap(image);
            var resized = ResizeBitmap(tmp, Math.Max(newWidth, newHeight), quickPreview);
            tmp.Dispose();
            return resized;
        }

        /// <summary>
        /// Try to get a cached preview image
        /// </summary>
        private static Bitmap TryGetCachedPreview(string filePath)
        {
            if (_previewCache.TryGetValue(filePath, out var weakRef))
            {
                if (weakRef.IsAlive && weakRef.Target is Bitmap bitmap)
                {
                    // Return a clone to avoid issues with the caller disposing a cached image
                    return (Bitmap)bitmap.Clone();
                }
                else
                {
                    // Clean up dead reference
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
                if (_previewCache.Count >= MaxCacheSize)
                {
                    CleanupPreviewCache();
                }
                // Cache a clone of the bitmap so the original can be disposed by the caller
                _previewCache[filePath] = new WeakReference(bitmap.Clone());
            }
        }

        /// <summary>
        /// Clean up old cache entries
        /// </summary>
        private static void CleanupPreviewCache()
        {
            foreach (var key in _previewCache.Keys.ToList())
            {
                if (_previewCache.TryGetValue(key, out var weakRef) && !weakRef.IsAlive)
                {
                    _previewCache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Load a very quick preview optimized for scrolling performance
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <returns>Quick preview bitmap</returns>
        private static Bitmap LoadQuickPreview(HeicImage heicImage)
        {
            // For quick preview, we load the full image and resize it to a small thumbnail size
            var fullBitmap = ConvertToBitmap(heicImage);
            var preview = ResizeBitmap(fullBitmap, QuickPreviewSize, true);
            fullBitmap.Dispose();
            return preview;
        }

        /// <summary>
        /// Try to load a suitable thumbnail from the HEIC image
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <param name="maxSize">Maximum size for the thumbnail</param>
        /// <returns>Thumbnail bitmap or null if none suitable</returns>
        private static Bitmap TryLoadThumbnail(HeicImage heicImage, int maxSize = ThumbnailSize)
        {
            // This is a placeholder as the library doesn't directly expose thumbnails in a simple way.
            // The performance strategy is now based on resizing the main image.
            return null;
        }

        /// <summary>
        /// Load the main image with size optimization
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <param name="maxSize">Maximum size for the image (0 = use default MaxDisplaySize)</param>
        /// <returns>Optimized bitmap</returns>
        private static Bitmap LoadMainImageOptimized(HeicImage heicImage, int maxSize = 0)
        {
            int targetMaxSize = (maxSize > 0) ? maxSize : MaxDisplaySize;
            var originalWidth = (int)heicImage.Width;
            var originalHeight = (int)heicImage.Height;

            if (originalWidth > targetMaxSize || originalHeight > targetMaxSize)
            {
                var fullBitmap = ConvertToBitmap(heicImage);
                var resized = ResizeBitmap(fullBitmap, targetMaxSize, false);
                fullBitmap.Dispose();
                return resized;
            }
            
            return ConvertToBitmap(heicImage);
        }

        /// <summary>
        /// Load a downsampled version of the image for better performance
        /// </summary>
        /// <param name="heicImage">The loaded HEIC image</param>
        /// <param name="downsampleFactor">Factor to downsample by (e.g., 2 for 50%, 4 for 25%)</param>
        /// <returns>Downsampled bitmap</returns>
        private static Bitmap LoadDownsampledImage(HeicImage heicImage, int downsampleFactor)
        {
            // The library does not support downsampling on decode in the way this method intended.
            // Instead, we load the full image and resize it.
            var fullBitmap = ConvertToBitmap(heicImage);
            int newWidth = fullBitmap.Width / downsampleFactor;
            int newHeight = fullBitmap.Height / downsampleFactor;
            var resized = ResizeBitmap(fullBitmap, Math.Max(newWidth, newHeight), true);
            fullBitmap.Dispose();
            return resized;
        }

        /// <summary>
        /// Creates a Bitmap from raw RGB data, handling color channel swap and stride.
        /// </summary>
        private static unsafe Bitmap CreateBitmapFromRgb(byte[] rgbData, int width, int height)
        {
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            try
            {
                byte* pScan0 = (byte*)bitmapData.Scan0;
                int stride = bitmapData.Stride;
                int bytesPerRow = width * 3;

                for (int y = 0; y < height; y++)
                {
                    byte* pDestRow = pScan0 + (y * stride);
                    int srcRowOffset = y * bytesPerRow;

                    for (int x = 0; x < width; x++)
                    {
                        int srcOffset = srcRowOffset + x * 3;
                        int destOffset = x * 3;
                        
                        // Swap R and B channels while copying
                        pDestRow[destOffset + 0] = rgbData[srcOffset + 2]; // Blue
                        pDestRow[destOffset + 1] = rgbData[srcOffset + 1]; // Green
                        pDestRow[destOffset + 2] = rgbData[srcOffset + 0]; // Red
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
        /// Convert HeicImage to Bitmap efficiently (full resolution)
        /// </summary>
        private static Bitmap ConvertToBitmap(HeicImage heicImage)
        {
            var rgbData = heicImage.GetByteArray(Openize.Heic.Decoder.PixelFormat.Rgb24);
            return CreateBitmapFromRgb(rgbData, (int)heicImage.Width, (int)heicImage.Height);
        }

        /// <summary>
        /// Resize bitmap efficiently
        /// </summary>
        private static Bitmap ResizeBitmap(Bitmap original, int maxSize, bool quickPreview)
        {
            var scale = Math.Min((double)maxSize / original.Width, (double)maxSize / original.Height);
            var newWidth = (int)(original.Width * scale);
            var newHeight = (int)(original.Height * scale);
            
            var resized = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = quickPreview ? InterpolationMode.Low : InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = quickPreview ? SmoothingMode.HighSpeed : SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = quickPreview ? PixelOffsetMode.HighSpeed : PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = quickPreview ? CompositingQuality.HighSpeed : CompositingQuality.HighQuality;
                
                graphics.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            
            return resized;
        }

        #endregion
    }
}
