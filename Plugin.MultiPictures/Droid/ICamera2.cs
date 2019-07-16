using Android.App;
using Android.Hardware.Camera2;
using Android.OS;
using Java.Util.Concurrent;
using Plugin.MultiPictures.Droid.Listeners;
using Plugin.MultiPictures.Utils;

namespace Plugin.MultiPictures.Droid
{
    public interface ICamera2
    {
        Semaphore CameraOpenCloseLock { get; set; }

        CameraDevice CameraDevice { get; set; }

        Activity Activity { get; set; }

        Camera2State State { get; set; }

        Handler BackgroundHandler { get; set; }

        CameraCaptureSession CaptureSession { get; set; }

        CaptureRequest.Builder PreviewRequestBuilder { get; set; }

        CaptureRequest PreviewRequest { get; set; }

        CameraCaptureListener CaptureCallback {get;set;}

        DeviceRotation DeviceRotation { get; set; }

        void CreateCameraPreviewSession();

        void CaptureStillPicture();

        void RunPrecaptureSequence();

        void OpenCamera(int width, int height);

        void ConfigureTransform(int width, int height);

        void UnlockFocus();

        void SetAutoFlash(CaptureRequest.Builder requestBuilder);
    }
}
