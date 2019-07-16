using Android.Hardware.Camera2;
using Plugin.MultiPictures.Droid;

namespace Plugin.MultiPictures.Droid.Listeners
{
	public class CameraCaptureStillPictureSessionCallback : CameraCaptureSession.CaptureCallback
	{
		//private static readonly string TAG = "CameraCaptureStillPictureSessionCallback";

		private readonly ICamera2 _camera2;

		public CameraCaptureStillPictureSessionCallback(ICamera2 camera2)
		{
            _camera2 = camera2 ?? throw new System.ArgumentNullException("camera2");
		}

		public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
		{
			_camera2.UnlockFocus();
		}
	}
}
