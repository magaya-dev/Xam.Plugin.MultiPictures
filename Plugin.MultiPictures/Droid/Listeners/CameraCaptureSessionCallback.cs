using Android.Hardware.Camera2;

namespace Plugin.MultiPictures.Droid.Listeners
{
    public class CameraCaptureSessionCallback : CameraCaptureSession.StateCallback
    {
        private readonly ICamera2 _camera2;

        public CameraCaptureSessionCallback(ICamera2 camera2)
        {
            _camera2 = camera2 ?? throw new System.ArgumentNullException("camera2");
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            //owner.ShowToast("Failed");
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            // The camera is already closed
            if (null == _camera2.CameraDevice)
            {
                return;
            }

            // When the session is ready, we start displaying the preview.
            _camera2.CaptureSession = session;
            try
            {
                // Auto focus should be continuous for camera preview.
                _camera2.PreviewRequestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);

                // Flash is automatically enabled when necessary.
                _camera2.SetAutoFlash(_camera2.PreviewRequestBuilder);

                // Finally, we start displaying the camera preview.
                _camera2.PreviewRequest = _camera2.PreviewRequestBuilder.Build();
                _camera2.CaptureSession.SetRepeatingRequest(_camera2.PreviewRequest, _camera2.CaptureCallback, _camera2.BackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }
    }
}