using Android.Hardware.Camera2;

namespace Plugin.MultiPictures.Droid.Listeners
{
    public class CameraStateListener : CameraDevice.StateCallback
    {
        private readonly ICamera2 _camera2;

        public CameraStateListener(ICamera2 camera2)
        {
            _camera2 = camera2 ?? throw new System.ArgumentNullException("camera2");
        }

        public override void OnOpened(CameraDevice cameraDevice)
        {
            // This method is called when the camera is opened.  We start camera preview here.
            _camera2.CameraOpenCloseLock.Release();
            _camera2.CameraDevice = cameraDevice;
            _camera2.CreateCameraPreviewSession();
        }

        public override void OnDisconnected(CameraDevice cameraDevice)
        {
            _camera2.CameraOpenCloseLock.Release();
            cameraDevice.Close();
            _camera2.CameraDevice = null;
        }

        public override void OnError(CameraDevice cameraDevice, CameraError error)
        {
            _camera2.CameraOpenCloseLock.Release();
            cameraDevice.Close();
            _camera2.CameraDevice = null;

            if (_camera2 == null)
            {
                return;
            }

            var activity = _camera2.Activity;

            if (activity != null)
            {
                activity.Finish();
            }
        }
    }
}