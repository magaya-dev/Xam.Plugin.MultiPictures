
using System;
using System.Threading.Tasks;
using Android.Media;
using Java.IO;
using Plugin.MultiPictures.Utils;

namespace Plugin.MultiPictures.Droid.Listeners
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        private readonly MediaOptions _mediaOptions;

        private readonly ICamera2 _camera2;

        public event EventHandler<MediaFile> ImageAvailable;

        public ImageAvailableListener(ICamera2 fragment, MediaOptions mediaOptions)
        {
            _camera2 = fragment ?? throw new ArgumentNullException("fragment");
            _mediaOptions = mediaOptions ?? throw new ArgumentNullException("mediaOptions");
        }

        public void OnImageAvailable(ImageReader reader)
        {
            var filename = string.Format("IMG_{0}.jpg", DateTime.Now.ToString("yyyyMMdd_HHmmss_ff"));
            string path = System.IO.Path.Combine(CrossMultiPictures.Current.MediaFolderPath(_mediaOptions), filename);
            
            _camera2.BackgroundHandler.Post(new ImageSaver(reader.AcquireNextImage(), new File(path), _mediaOptions, _camera2.DeviceRotation, ImageAvailable));
        }

        // Saves a JPEG {@link Image} into the specified {@link File}.
        private class ImageSaver : Java.Lang.Object, Java.Lang.IRunnable
        {
            // The JPEG image
            private readonly Image _image;

            // The file we save the image into.
            private readonly File _file;

            private readonly MediaOptions _mediaOptions;

            private event EventHandler<MediaFile> CallBack;

            private readonly DeviceRotation _currentRotation;

            public ImageSaver(Image image, File file, MediaOptions mediaOptions, DeviceRotation rotation, EventHandler<MediaFile> callBack)
            {
                _image = image ?? throw new ArgumentNullException("image");
                _file = file ?? throw new ArgumentNullException("file");
                _mediaOptions = mediaOptions ?? throw new ArgumentNullException("mediaOptions");
                _currentRotation = rotation;

                CallBack = callBack;
            }

            public void Run()
            {
                var action = new Action(async () =>
                {
                    await RunAsync();
                });

                action.Invoke();
            }

            private async Task RunAsync()
            {
                var buffer = _image.GetPlanes()[0].Buffer;
                byte[] bytes = new byte[buffer.Remaining()];
                buffer.Get(bytes);

                using (var output = new FileOutputStream(_file))
                {
                    try
                    {
                        output.Write(bytes);
                        output.Close();
                        output.Dispose();

                        bytes = null;
                        buffer.Clear();
                        buffer.Dispose();

                        var mediaFile = new MediaFile(_file.AbsolutePath);

                        await CrossMultiPictures.Current.ResizeAndRotateAsync(mediaFile, _mediaOptions, _currentRotation);

                        CallBack?.Invoke(this, mediaFile);
                    }
                    catch (IOException ex)
                    {
                        ex.PrintStackTrace();
                    }
                    finally
                    {
                        _image.Close();
                    }
                }
            }
        }
    }
}
