
using Android.Hardware.Camera2;
using Plugin.MultiPictures.Utils;

namespace Plugin.MultiPictures.Droid.Listeners
{
    public class CameraCaptureListener : CameraCaptureSession.CaptureCallback
    {
        private readonly ICamera2 _camera2;

        public CameraCaptureListener(ICamera2 camera2)
        {
            _camera2 = camera2 ?? throw new System.ArgumentNullException("camera2");
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            Process(result);
        }

        public override void OnCaptureProgressed(CameraCaptureSession session, CaptureRequest request, CaptureResult partialResult)
        {
            Process(partialResult);
        }

        private void Process(CaptureResult result)
        {
            switch (_camera2.State)
            {
                case Camera2State.WaitingLock:
                    {
                        var autoFocusState = (Java.Lang.Integer)result.Get(CaptureResult.ControlAfState);
                        if (autoFocusState == null)
                        {
                            _camera2.State = Camera2State.PictureTaken;
                            _camera2.CaptureStillPicture();
                            return;
                        }

                        // ControlAeState can be null on some devices
                        var autoExposureState = (Java.Lang.Integer)result.Get(CaptureResult.ControlAeState);
                        if (autoExposureState == null || autoExposureState.IntValue() != ((int)ControlAEState.FlashRequired))
                        {
                            _camera2.State = Camera2State.PictureTaken;
                            _camera2.CaptureStillPicture();
                            return;
                        }

                        if (autoFocusState.IntValue() == ((int)ControlAFState.Inactive) ||
                                autoFocusState.IntValue() == ((int)ControlAFState.FocusedLocked) ||
                                autoFocusState.IntValue() == ((int)ControlAFState.NotFocusedLocked))
                        {
                            if (autoExposureState.IntValue() == ((int)ControlAEState.Converged))
                            {
                                _camera2.State = Camera2State.PictureTaken;
                                _camera2.CaptureStillPicture();
                                return;
                            }
                            else
                            {
                                _camera2.RunPrecaptureSequence();
                            }
                        }
                        break;
                    }
                case Camera2State.WaitingPrecapture:
                    {
                        // ControlAeState can be null on some devices
                        var autoExposureState = (Java.Lang.Integer)result.Get(CaptureResult.ControlAeState);
                        if (autoExposureState == null || autoExposureState.IntValue() == ((int)ControlAEState.Precapture) ||
                                autoExposureState.IntValue() == ((int)ControlAEState.FlashRequired))
                        {
                            _camera2.State = Camera2State.WaitingNonPrecapture;
                        }

                        break;
                    }
                case Camera2State.WaitingNonPrecapture:
                    {
                        // ControlAeState can be null on some devices
                        var autoExposureState = (Java.Lang.Integer)result.Get(CaptureResult.ControlAeState);
                        if (autoExposureState == null || autoExposureState.IntValue() != ((int)ControlAEState.Precapture))
                        {
                            _camera2.State = Camera2State.PictureTaken;
                            _camera2.CaptureStillPicture();
                        }

                        break;
                    }
                case Camera2State.PictureTaken:
                    {
                        System.Diagnostics.Debug.WriteLine("STATE_PICTURE_TAKEN");
                        break;
                    }
            }
        }
    }
}