using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Openize.Heic.Decoder;

namespace PhotoSift
{
    /// <summary>
    /// Wrapper class for loading HEIC/HEIF images
    /// Uses Windows native support first for best performance, falls back to Openize.Heic
    /// </summary>
    public static class HeicWrapper
    {
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
                // First try Windows native support (much faster and lower CPU usage)
                try
                {
                    using (var image = Image.FromFile(filePath))
                    {
                        return ProcessLoadedImage(image, maxSize, quickPreview);
                    }
                }
                catch (OutOfMemoryException)
                {
                    // This typically means the file format is not supported by GDI+
                    // Fall back to Openize.Heic
                }
                catch (ArgumentException)
                {
                    // Invalid image format, fall back to Openize.Heic
                }

                // Fallback to Openize.Heic if Windows native fails
                return LoadWithOpenize(filePath, maxSize, quickPreview);
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
                // Try native first
                using (var stream = new MemoryStream(data))
                using (var image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
            catch (OutOfMemoryException)
            {
                // Fall back to Openize.Heic
                using (var stream = new MemoryStream(data))
                {
                    var heicImage = HeicImage.Load(stream);
                    return ConvertToBitmap(heicImage);
                }
            }
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
                    return ((int)heicImage.Width, (int)heicImage.Height, false);
                }
            }
            catch
            {
                return (0, 0, false);
            }
        }

        #region Private Methods

        /// <summary>
        /// Process a loaded image (resize if needed, optimize for display)
        /// </summary>
        private static Bitmap ProcessLoadedImage(Image image, int maxSize, bool quickPreview)
        {
            // Determine target size
            int targetMaxSize = maxSize;
            if (quickPreview && (targetMaxSize == 0 || targetMaxSize > 512))
            {
                targetMaxSize = 512; // Limit size for quick preview to reduce CPU usage
            }

            // If no resizing is needed, we still draw it to a new bitmap to ensure a standard pixel format (24bppRgb)
            // and to decouple from the original image stream.
            if (targetMaxSize == 0 || (image.Width <= targetMaxSize && image.Height <= targetMaxSize))
            {
                var newBitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using (var graphics = Graphics.FromImage(newBitmap))
                {
                    graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                }
                return newBitmap;
            }

            // Calculate new dimensions maintaining aspect ratio
            double scale = Math.Min((double)targetMaxSize / image.Width, (double)targetMaxSize / image.Height);
            int newWidth = (int)(image.Width * scale);
            int newHeight = (int)(image.Height * scale);

            // Create resized bitmap
            var resized = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(resized))
            {
                // Use faster settings for quick preview to reduce CPU usage
                if (quickPreview)
                {
                    graphics.InterpolationMode = InterpolationMode.Bilinear;
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                }
                else
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                }

                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return resized;
        }

        /// <summary>
        /// Load HEIC using Openize.Heic library as fallback
        /// </summary>
        private static Bitmap LoadWithOpenize(string filePath, int maxSize, bool quickPreview)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var heicImage = HeicImage.Load(fileStream);
                
                // Load main image
                var bitmap = ConvertToBitmap(heicImage);
                
                // Apply size constraints if needed
                if (maxSize > 0 && (bitmap.Width > maxSize || bitmap.Height > maxSize))
                {
                    var resized = ResizeBitmap(bitmap, maxSize, quickPreview);
                    bitmap.Dispose(); // IMPORTANT: Dispose the original large bitmap
                    return resized;
                }
                
                return bitmap;
            }
        }

        /// <summary>
        /// Convert HeicImage to Bitmap efficiently, handling color channel swapping and memory stride.
        /// This method is marked as unsafe because it uses pointers for performance.
        /// </summary>
        private static unsafe Bitmap ConvertToBitmap(HeicImage heicImage)
        {
            var width = (int)heicImage.Width;
            var height = (int)heicImage.Height;

            // Get RGB data from HEIC image
            var rgbData = heicImage.GetByteArray(Openize.Heic.Decoder.PixelFormat.Rgb24);

            // Create a new bitmap to copy the data into.
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

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
                        
                        // Swap R and B channels while copying from RGB byte array to BGR bitmap
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
                // Use faster settings for quick preview
                if (quickPreview)
                {
                    graphics.InterpolationMode = InterpolationMode.Bilinear;
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
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
            
            // The caller is now responsible for disposing the original bitmap.
            return resized;
        }

        #endregion
    }
}
