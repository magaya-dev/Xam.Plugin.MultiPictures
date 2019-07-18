
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreGraphics;
using Plugin.MultiPictures.Utils;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using UIKit;
using Xamarin.Forms;

namespace Plugin.MultiPictures
{
    public class MultiPicturesImplementation : MultiPicturesAbstract
    {
        public override bool IsCameraAvailable { get; }

        public MultiPicturesImplementation()
        {
            RequestedRotation = DeviceRotation.Unknown;

            IsCameraAvailable = UIImagePickerController.IsCameraDeviceAvailable(UIImagePickerControllerCameraDevice.Front)
                                       | UIImagePickerController.IsCameraDeviceAvailable(UIImagePickerControllerCameraDevice.Rear);
        }

        public override string MediaFolderPath(MediaOptions mediaOptions)
        {
            var specialFolder = mediaOptions.UsePublicStorage == true ? Environment.SpecialFolder.MyPictures : Environment.SpecialFolder.MyDocuments;
            string path = Environment.GetFolderPath(specialFolder);

            var result = Path.Combine(path, mediaOptions.Directory);

            if (string.IsNullOrEmpty(mediaOptions.Directory) == false)
            {
                Directory.CreateDirectory(result);
            }

            return result;
        }

        public override Task<byte[]> ResizedStream(MediaFile file, MediaOptions mediaOptions)
        {
            var percent = 1.0f;
            switch (mediaOptions.PhotoSize)
            {
                case PhotoSize.Large:
                    percent = 0.75f;
                    break;
                case PhotoSize.Medium:
                    percent = 0.5f;
                    break;
                case PhotoSize.Small:
                    percent = 0.25f;
                    break;
                case PhotoSize.Custom:
                    percent = (float)mediaOptions.CustomPhotoSize / 100f;
                    break;
            }

            var image = new UIImage(file.Path);

            var result = Resize(image, percent, mediaOptions.CompressionQuality);

            return Task.FromResult(result);
        }

        protected override void BindPage(ContentPage page)
        {
            page.Appearing += delegate
            {
                RequestedRotation = DeviceRotation.Portrait;

                UIDevice.CurrentDevice.SetValueForKey(Foundation.NSNumber.FromNInt((int)UIInterfaceOrientation.Portrait), new Foundation.NSString("orientation"));
            };

            page.Disappearing += delegate
            {
                RequestedRotation = DeviceRotation.Unknown;

                UIDevice.CurrentDevice.SetValueForKey(Foundation.NSNumber.FromNInt((int)UIInterfaceOrientation.Unknown), new Foundation.NSString("orientation"));
            };
        }

        public override Task ResizeAndRotateAsync(MediaFile file, MediaOptions mediaOptions, DeviceRotation currentRotaion = DeviceRotation.Unknown)
        {
            throw new NotImplementedException();
        }

        protected override async Task<bool> HasCameraPermission()
        {
            var result = await HasPermission(Permission.Camera);

            return result;
        }

        protected override async Task<bool> HasStoragePermission()
        {
            var result = await HasPermission(Permission.Photos);

            return result;
        }

        #region Utils

        private byte[] Resize(UIImage sourceImage, float percent, float quality)
        {
            UIImage resultImage;
            var sourceSize = sourceImage.Size;

            var width = percent * sourceSize.Width;
            var height = percent * sourceSize.Height;

            UIGraphics.BeginImageContext(new CGSize(width, height));
            sourceImage.Draw(new CGRect(0, 0, width, height));

            resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            using (var imageData = resultImage.AsJPEG(quality))
            {
                var res = new byte[imageData.Length];
                Marshal.Copy(imageData.Bytes, res, 0, (int)imageData.Length);

                return res;
            }
        }

        private async Task<bool> HasPermission(Permission permission)
        {
            var permissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(permission);

            if (permissionStatus != PermissionStatus.Granted)
            {
                var result = await CrossPermissions.Current.RequestPermissionsAsync(permission);

                return result[permission] == PermissionStatus.Granted;
            }

            return true;
        }

        #endregion
    }
}
