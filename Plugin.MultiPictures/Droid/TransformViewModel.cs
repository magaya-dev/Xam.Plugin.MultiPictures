
using Android.Media;
using Plugin.MultiPictures.Utils;

namespace Plugin.MultiPictures.Droid
{
    public class TransformViewModel
    {
        public string FilePath { get; set; }

        public MediaOptions MediaOptions { get; set; }

        public ExifInterface Exif { get; set; }

        public DeviceRotation CurrentRotation { get; set; } = DeviceRotation.Unknown;
    }
}
