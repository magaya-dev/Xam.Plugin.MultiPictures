
using System;
using System.IO;
using System.Linq;
using AVFoundation;
using CoreGraphics;
using CoreVideo;
using Foundation;
using Plugin.MultiPictures.Apple;
using Plugin.MultiPictures.Utils;
using Plugin.MultiPictures.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CameraPreview), typeof(CameraPreviewRenderer))]
namespace Plugin.MultiPictures.Apple
{
    public class CameraPreviewRenderer : ViewRenderer<CameraPreview, UICameraPreview>
    {
        private CameraPreview _currentElement;

        private UICameraPreview _cameraPreview;

        private readonly NSNumber _flashModeAutoFlash;

        private IOrientationListener _orientationListener;

        public CameraPreviewRenderer()
        {
            _orientationListener = new OrientationListener();
            _orientationListener.OrientationChanged += OnOrientationChanged;

            _flashModeAutoFlash = new NSNumber((long)AVCaptureFlashMode.Auto);
        }

        private void OnOrientationChanged(object sender, DeviceRotation rotation)
        {
            _currentElement?.OnRotationChanged(rotation);
        }

        #region Overrides

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview> e)
        {
            base.OnElementChanged(e);

            if (Control == null && e.NewElement != null)
            {
                _cameraPreview = new UICameraPreview(e.NewElement.Camera);
                SetNativeControl(_cameraPreview);

                if (_cameraPreview.CaptureSession.Outputs.Length == 0)
                {
                    _cameraPreview.CaptureSession.AddOutput(new AVCapturePhotoOutput());
                }
            }

            if (e.NewElement != null)
            {
                _currentElement = e.NewElement;
                _currentElement.StartRecording = (TakePicture);
                _currentElement.StopRecording = (CloseCamera);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                Control.CaptureSession?.Dispose();
                Control.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        protected void TakePicture()
        {
            var session = _cameraPreview.CaptureSession;
            var output = session.Outputs[0] as AVCapturePhotoOutput;
            //output.IsHighResolutionCaptureEnabled = true;

            var photoSettings = AVCapturePhotoSettings.Create();
            photoSettings.IsAutoStillImageStabilizationEnabled = true;
            //photoSettings.IsHighResolutionPhotoEnabled = true;

            var flashModeAuto = output?.SupportedFlashModes.Contains(_flashModeAutoFlash) ?? false;
            if (flashModeAuto == true)
            {
                photoSettings.FlashMode = AVCaptureFlashMode.Auto;
            }

            if (photoSettings.AvailablePreviewPhotoPixelFormatTypes.Length > 0)
            {
                photoSettings.PreviewPhotoFormat = new NSDictionary<NSString, NSObject>(CVPixelBuffer.PixelFormatTypeKey, output.AvailablePhotoPixelFormatTypes[0]);
            }

            var photoCaptureDelegate = new PhotoCaptureDelegate();
            photoCaptureDelegate.PictureTaken += OnPictureTaken;

            output.CapturePhoto(photoSettings, photoCaptureDelegate);
        }

        protected void OnPictureTaken(object sender, NSData data)
        {
            if (data == null)
            {
                return;
            }

            var filename = string.Format("IMG_{0}.jpg", DateTime.Now.ToString("yyyyMMdd_HHmmss_ff"));
            string fullPath = Path.Combine(CrossMultiPictures.Current.MediaFolderPath(_currentElement.MediaOptions), filename);

            NSData imgData;

            using (var image = UIImage.LoadFromData(data))
            {
                var imageResult = RotateImage(image, _orientationListener.DeviceRotation);

                using (var finalImage = UIImage.FromImage(imageResult.CGImage, imageResult.CurrentScale, UIImageOrientation.Up))
                {
                    imgData = finalImage.AsJPEG(_currentElement.MediaOptions.CompressionQuality / 100.0f);
                    imgData.Save(fullPath, false, out NSError err);
                }
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                _currentElement.OnImageAvailable(new MediaFile(fullPath));
            });
        }

        protected UIImage RotateImage(UIImage image, DeviceRotation currentOrientation)
        {
            CGContext bitmap;

            switch (currentOrientation)
            {
                case DeviceRotation.Unknown:
                case DeviceRotation.Portrait:
                    UIGraphics.BeginImageContext(new CGSize(image.CGImage.Height, image.CGImage.Width));

                    bitmap = UIGraphics.GetCurrentContext();
                    bitmap.ScaleCTM(-1, 1);
                    bitmap.RotateCTM((float)Math.PI * 0.5f);
                    bitmap.DrawImage(new CGRect(0, 0, image.CGImage.Width, image.CGImage.Height), image.CGImage);
                    break;
                case DeviceRotation.ReversePortrait:
                    UIGraphics.BeginImageContext(new CGSize(image.CGImage.Height, image.CGImage.Width));

                    bitmap = UIGraphics.GetCurrentContext();
                    bitmap.TranslateCTM(image.CGImage.Height, image.CGImage.Width);
                    bitmap.ScaleCTM(1, -1);
                    bitmap.RotateCTM((float)Math.PI * 0.5f);
                    bitmap.DrawImage(new CGRect(0, 0, image.CGImage.Width, image.CGImage.Height), image.CGImage);
                    break;
                case DeviceRotation.Landscape:
                    UIGraphics.BeginImageContext(new CGSize(image.CGImage.Width, image.CGImage.Height));

                    bitmap = UIGraphics.GetCurrentContext();
                    bitmap.TranslateCTM(image.CGImage.Width, 0);
                    bitmap.ScaleCTM(-1, 1);
                    bitmap.DrawImage(new CGRect(0, 0, image.CGImage.Width, image.CGImage.Height), image.CGImage);
                    break;
                case DeviceRotation.ReverseLandscape:
                    UIGraphics.BeginImageContext(new CGSize(image.CGImage.Width, image.CGImage.Height));

                    bitmap = UIGraphics.GetCurrentContext();
                    bitmap.TranslateCTM(0, image.CGImage.Height);
                    bitmap.ScaleCTM(1, -1);
                    bitmap.DrawImage(new CGRect(0, 0, image.CGImage.Width, image.CGImage.Height), image.CGImage);
                    break;
            }

            try
            {
                return UIGraphics.GetImageFromCurrentImageContext();
            }
            finally
            {
                UIGraphics.EndImageContext();
            }
        }

        protected void CloseCamera()
        {
            _cameraPreview?.PausePreview();
        }
    }
}
