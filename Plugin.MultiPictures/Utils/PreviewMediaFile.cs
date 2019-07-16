using System.IO;
using Xamarin.Forms;

namespace Plugin.MultiPictures.Utils
{
    public class PreviewMediaFile
    {
        public string OriginalFilePath { get; set; }

        public ImageSource ImageSource { get; set; }

        public PreviewMediaFile(string path, byte[] content)
        {
            OriginalFilePath = path;
            ImageSource = ImageSource.FromStream(() => new MemoryStream(content));
        }
    }
}
