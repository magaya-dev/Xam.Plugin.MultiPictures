
using System.Linq;
using AVFoundation;
using CoreGraphics;
using Foundation;
using Plugin.MultiPictures.Utils;
using UIKit;

namespace Plugin.MultiPictures.Apple
{
    public class UICameraPreview : UIView
    {
        private AVCaptureVideoPreviewLayer _previewLayer;

        private readonly CameraOptions _cameraOptions;

        public bool IsPreviewing { get; set; }

        public AVCaptureSession CaptureSession { get; set; }

        public UICameraPreview(CameraOptions options)
        {
            _cameraOptions = options;
            IsPreviewing = false;

            Initialize();
        }

        protected void Initialize()
        {
            CaptureSession = new AVCaptureSession();
            _previewLayer = new AVCaptureVideoPreviewLayer(CaptureSession)
            {
                Frame = Bounds,
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };

            AVCaptureDeviceType[] captureDevices = { AVCaptureDeviceType.BuiltInWideAngleCamera };
            var cameraPosition = (_cameraOptions == CameraOptions.Front) ? AVCaptureDevicePosition.Front : AVCaptureDevicePosition.Back;
            var videoDevices = AVCaptureDeviceDiscoverySession.Create(captureDevices, AVMediaType.Video, cameraPosition);
            var device = videoDevices.Devices.FirstOrDefault();

            if(device == null)
            {
                return;
            }

            var input = new AVCaptureDeviceInput(device, out NSError error);
            
            CaptureSession.AddInput(input);
            Layer.AddSublayer(_previewLayer);
            CaptureSession.StartRunning();

            IsPreviewing = true;
        }

        public void ResumePreview()
        {
            CaptureSession?.StartRunning();
        }

        public void PausePreview()
        {
            CaptureSession?.StopRunning();
        }

        #region Overrides

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);
            _previewLayer.Frame = rect;
        }

        #endregion
    }
}
