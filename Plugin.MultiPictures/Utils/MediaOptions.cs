
namespace Plugin.MultiPictures.Utils
{
    public class MediaOptions
    {
        /// <summary>
        /// Directory name
        /// </summary>
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        // Get or set if the image should be stored public
        /// </summary>
        public bool UsePublicStorage { get; set; } = false;

        /// <summary>
        /// Gets or sets the size of the photo.
        /// </summary>
        /// <value>The size of the photo.</value>
        public PhotoSize PhotoSize { get; set; } = PhotoSize.Full;

        int customPhotoSize = 100;
        /// <summary>
        /// The custom photo size to use, 100 full size (same as Full),
        /// and 1 being smallest size at 1% of original
        /// Default is 100
        /// </summary>
        public int CustomPhotoSize
        {
            get { return customPhotoSize; }
            set
            {
                if (value > 100)
                    customPhotoSize = 100;
                else if (value < 1)
                    customPhotoSize = 1;
                else
                    customPhotoSize = value;
            }
        }

        int quality = 100;
        /// <summary>
        /// The compression quality to use, 0 is the maximum compression (worse quality),
        /// and 100 minimum compression (best quality)
        /// Default is 100
        /// </summary>
        public int CompressionQuality
        {
            get { return quality; }
            set
            {
                if (value > 100)
                    quality = 100;
                else if (value < 0)
                    quality = 0;
                else
                    quality = value;
            }
        }

        /// <summary>
        /// Saves metadate/exif data from the original file.
        /// </summary>
        public bool SaveMetaData { get; set; } = true;
    }
}
