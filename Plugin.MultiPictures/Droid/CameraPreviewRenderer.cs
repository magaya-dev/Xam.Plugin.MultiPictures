﻿using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Plugin.MultiPictures.Droid;
using Plugin.MultiPictures.Droid.Listeners;
using Plugin.MultiPictures.Utils;
using Plugin.MultiPictures.Views;
using Plugin.PhotoAndScan.CustomMedia.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using JBoolean = Java.Lang.Boolean;
using JMath = Java.Lang.Math;
using Point = Android.Graphics.Point;
using Size = Android.Util.Size;

[assembly: ExportRenderer(typeof(CameraPreview), typeof(CameraPreviewRenderer))]
namespace Plugin.PhotoAndScan.CustomMedia.Droid
{
    public class CameraPreviewRenderer : ViewRenderer<CameraPreview, AutoFitTextureView>, ICamera2
    {
        #region Private fields

        // Tag for the {@link Log}.
        private static readonly string TAG = "Camera2BasicFragment";

        // Max preview width that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_WIDTH = 1920;

        // Max preview height that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_HEIGHT = 1080;

        // TextureView.ISurfaceTextureListener handles several lifecycle events on a TextureView
        private Camera2SurfaceTextureListener _surfaceTextureListener;

        // ID of the current {@link CameraDevice}.
        private string _cameraId;

        // An AutoFitTextureView for camera preview
        private AutoFitTextureView _textureView;

        // The size of the camera preview
        private Size _previewSize;

        // CameraDevice.StateListener is called when a CameraDevice changes its state
        private CameraStateListener _stateCallback;

        // An additional thread for running tasks that shouldn't block the UI.
        private HandlerThread _backgroundThread;

        // An {@link ImageReader} that handles still image capture.
        private ImageReader _imageReader;

        // This a callback object for the {@link ImageReader}. "onImageAvailable" will be called when a
        // still image is ready to be saved.
        private ImageAvailableListener _onImageAvailableListener;

        // Whether the current camera device supports Flash or not.
        private bool _isFlashSupported;

        // Orientation of the camera sensor
        private int _sensorOrientation;

        private CaptureRequest.Builder _stillCaptureBuilder;

        private OrientationEventListener _orientationEventListener;

        private event EventHandler RotationChanged;

        // use to force camera view to show always in portrait mode
        private ScreenOrientation _previousOrientation;

        #endregion

        #region Public fields

        public static readonly int REQUEST_CAMERA_PERMISSION = 1;

        // A {@link CameraCaptureSession } for camera preview.
        public CameraCaptureSession CaptureSession { get; set; }

        // A reference to the opened CameraDevice
        public CameraDevice CameraDevice { get; set; }

        // A {@link Handler} for running tasks in the background.
        public Handler BackgroundHandler { get; set; }

        // This is the output file for our picture.
        public File File { get; set; }

        //{@link CaptureRequest.Builder} for the camera preview
        public CaptureRequest.Builder PreviewRequestBuilder { get; set; }

        // {@link CaptureRequest} generated by {@link #mPreviewRequestBuilder}
        public CaptureRequest PreviewRequest { get; set; }

        // The current state of camera state for taking pictures.
        public Camera2State State { get; set; } = Camera2State.Preview;

        // A {@link Semaphore} to prevent the app from exiting before closing the camera.
        public Semaphore CameraOpenCloseLock { get; set; } = new Semaphore(1);

        // A {@link CameraCaptureSession.CaptureCallback} that handles events related to JPEG capture.
        public CameraCaptureListener CaptureCallback { get; set; }

        public Activity Activity { get; set; }

        public DeviceRotation DeviceRotation { get; set; } = DeviceRotation.Unknown;

        #endregion

        public CameraPreviewRenderer(Context context) : base(context)
        {
            _previousOrientation = ScreenOrientation.Sensor;

            MessagingCenter.Subscribe<PickPhotosPage>(this, "OnAppearing", (page) =>
            {
                OnWindowVisibilityChanged(ViewStates.Gone);
            });

            MessagingCenter.Subscribe<PickPhotosPage>(this, "OnDisappearing", (page) =>
            {
                OnWindowVisibilityChanged(ViewStates.Visible);
            });
        }

        ~CameraPreviewRenderer()
        {
            Dispose(true);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview> args)
        {
            if (DesignMode.IsDesignModeEnabled)
            {
                return;
            }

            if (args.OldElement != null) // Clear old element event
            { }

            if (args.NewElement != null)
            {
                args.NewElement.StartRecording = (() => { TakePicture(); });
                args.NewElement.StopRecording = (() => { CloseCamera(); });

                if (Control == null)
                {
                    Activity = this.Context as Activity;
                    SetNativeControl(new AutoFitTextureView(Context));
                }

                _textureView = Control as AutoFitTextureView;
                _stateCallback = new CameraStateListener(this);
                _surfaceTextureListener = new Camera2SurfaceTextureListener(this);

                CaptureCallback = new CameraCaptureListener(this);

                _onImageAvailableListener = new ImageAvailableListener(this, Element.MediaOptions);

                _onImageAvailableListener.ImageAvailable += (s, e) =>
                {
                    Element.OnImageAvailable(e);
                };

                _orientationEventListener = new OrientationChangeListener(Context, (int rotation) => OnRotationChanged(rotation));
                _orientationEventListener.Enable();

                RotationChanged += (s, e) =>
                {
                    args.NewElement.OnRotationChanged(DeviceRotation);
                };

                StartTheCamera();
            }

            base.OnElementChanged(args);
        }

        // force renderer to show up always in portrait orientation
        protected override void OnWindowVisibilityChanged([GeneratedEnum] ViewStates visibility)
        {
            base.OnWindowVisibilityChanged(visibility);

            var activity = (Activity)Context;

            if (visibility.Equals(ViewStates.Gone))
            {
                // go back to previous orientation
                activity.RequestedOrientation = _previousOrientation;
            }
            else if (visibility.Equals(ViewStates.Visible))
            {
                if (_previousOrientation.Equals(ScreenOrientation.Sensor))
                {
                    _previousOrientation = activity.RequestedOrientation;
                }

                activity.RequestedOrientation = ScreenOrientation.Portrait;
            }
        }

        protected void OnRotationChanged(int rotation)
        {
            var currentRotation = DeviceRotation.Unknown;

            switch (rotation / 45)
            {
                case 0:
                case 7:
                    currentRotation = DeviceRotation.Portrait;
                    break;
                case 1:
                case 2:
                    currentRotation = DeviceRotation.Landscape;
                    break;
                case 3:
                case 4:
                    currentRotation = DeviceRotation.ReversePortrait;
                    break;
                case 5:
                case 6:
                    currentRotation = DeviceRotation.ReverseLandscape;
                    break;
            }

            if (DeviceRotation != currentRotation)
            {
                DeviceRotation = currentRotation;
                RotationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected void StartTheCamera()
        {
            StartBackgroundThread();

            // When the screen is turned off and turned back on, the SurfaceTexture is already
            // available, and "onSurfaceTextureAvailable" will not be called. In that case, we can open
            // a camera and start preview from here (otherwise, we wait until the surface is ready in
            // the SurfaceTextureListener).
            if (_textureView.IsAvailable)
            {
                OpenCamera(_textureView.Width, _textureView.Height);
            }
            else
            {
                _textureView.SurfaceTextureListener = _surfaceTextureListener;
            }
        }

        protected static Size ChooseOptimalSize(Size[] choices, int textureViewWidth,
            int textureViewHeight, int maxWidth, int maxHeight, Size aspectRatio)
        {
            // Collect the supported resolutions that are at least as big as the preview Surface
            var bigEnough = new List<Size>();
            // Collect the supported resolutions that are smaller than the preview Surface
            var notBigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;

            for (var i = 0; i < choices.Length; i++)
            {
                Size option = choices[i];
                if ((option.Width <= maxWidth) && (option.Height <= maxHeight) &&
                       option.Height == option.Width * h / w)
                {
                    if (option.Width >= textureViewWidth &&
                        option.Height >= textureViewHeight)
                    {
                        bigEnough.Add(option);
                    }
                    else
                    {
                        notBigEnough.Add(option);
                    }
                }
            }

            // Pick the smallest of those big enough. If there is no one big enough, pick the
            // largest of those not big enough.
            if (bigEnough.Count > 0)
            {
                return (Size)Collections.Min(bigEnough, new SizesByAreaComparer());
            }
            else if (notBigEnough.Count > 0)
            {
                return (Size)Collections.Max(notBigEnough, new SizesByAreaComparer());
            }
            else
            {
                Log.Error(TAG, "Couldn't find any suitable preview size");
                return choices[0];
            }
        }

        // Sets up member variables related to camera.
        protected void SetUpCameraOutputs(int width, int height)
        {
            var activity = Activity;
            var manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                for (var i = 0; i < manager.GetCameraIdList().Length; i++)
                {
                    var cameraId = manager.GetCameraIdList()[i];
                    var characteristics = manager.GetCameraCharacteristics(cameraId);

                    // We don't use a front facing camera in this sample.
                    var facing = (Integer)characteristics.Get(CameraCharacteristics.LensFacing);
                    if (facing != null && facing == (Integer.ValueOf((int)LensFacing.Front)))
                    {
                        continue;
                    }

                    var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                    if (map == null)
                    {
                        continue;
                    }

                    // For still image captures, we use the largest available size.
                    Size largest = (Size)Collections.Max(Arrays.AsList(map.GetOutputSizes((int)ImageFormatType.Jpeg)), new SizesByAreaComparer());
                    _imageReader = ImageReader.NewInstance(largest.Width, largest.Height, ImageFormatType.Jpeg, /*maxImages*/2);
                    _imageReader.SetOnImageAvailableListener(_onImageAvailableListener, BackgroundHandler);

                    // Find out if we need to swap dimension to get the preview size relative to sensor
                    // coordinate.
                    var displayRotation = activity.WindowManager.DefaultDisplay.Rotation;
                    //noinspection ConstantConditions
                    _sensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);

                    bool swappedDimensions = false;
                    switch (displayRotation)
                    {
                        case SurfaceOrientation.Rotation0:
                        case SurfaceOrientation.Rotation180:
                            if (_sensorOrientation == 90 || _sensorOrientation == 270)
                            {
                                swappedDimensions = true;
                            }
                            break;
                        case SurfaceOrientation.Rotation90:
                        case SurfaceOrientation.Rotation270:
                            if (_sensorOrientation == 0 || _sensorOrientation == 180)
                            {
                                swappedDimensions = true;
                            }
                            break;
                        default:
                            Log.Error(TAG, "Display rotation is invalid: " + displayRotation);
                            break;
                    }

                    Point displaySize = new Point();
                    activity.WindowManager.DefaultDisplay.GetSize(displaySize);
                    var rotatedPreviewWidth = width;
                    var rotatedPreviewHeight = height;
                    var maxPreviewWidth = displaySize.X;
                    var maxPreviewHeight = displaySize.Y;

                    if (swappedDimensions)
                    {
                        rotatedPreviewWidth = height;
                        rotatedPreviewHeight = width;
                        maxPreviewWidth = displaySize.Y;
                        maxPreviewHeight = displaySize.X;
                    }

                    if (maxPreviewWidth > MAX_PREVIEW_WIDTH)
                    {
                        maxPreviewWidth = MAX_PREVIEW_WIDTH;
                    }

                    if (maxPreviewHeight > MAX_PREVIEW_HEIGHT)
                    {
                        maxPreviewHeight = MAX_PREVIEW_HEIGHT;
                    }

                    // Danger, W.R.! Attempting to use too large a preview size could  exceed the camera
                    // bus' bandwidth limitation, resulting in gorgeous previews but the storage of
                    // garbage capture data.
                    _previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))),
                        rotatedPreviewWidth, rotatedPreviewHeight, maxPreviewWidth, maxPreviewHeight, largest);

                    // We fit the aspect ratio of TextureView to the size of preview we picked.
                    var orientation = Resources.Configuration.Orientation;
                    if (orientation == Android.Content.Res.Orientation.Landscape)
                    {
                        _textureView.SetAspectRatio(_previewSize.Width, _previewSize.Height);
                    }
                    else
                    {
                        _textureView.SetAspectRatio(_previewSize.Height, _previewSize.Width);
                    }

                    // Check if the flash is supported.
                    var available = (JBoolean)characteristics.Get(CameraCharacteristics.FlashInfoAvailable);
                    if (available == null)
                    {
                        _isFlashSupported = false;
                    }
                    else
                    {
                        _isFlashSupported = (bool)available;
                    }

                    _cameraId = cameraId;

                    return;
                }
            }
            catch (CameraAccessException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
            catch (NullPointerException ex)
            {
                // Currently an NPE is thrown when the Camera2API is used but not supported on the
                // device this code runs.
                //ErrorDialog.NewInstance(GetString(Resource.String.camera_error)).Show(ChildFragmentManager, FRAGMENT_DIALOG);
            }
        }

        // Opens the camera specified by {@link Camera2BasicFragment#mCameraId}.
        public void OpenCamera(int width, int height)
        {
            SetUpCameraOutputs(width, height);
            ConfigureTransform(width, height);
            var activity = Activity;
            var manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                if (CameraOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds) == false)
                {
                    throw new RuntimeException("Time out waiting to lock camera opening.");
                }
                if (string.IsNullOrEmpty(_cameraId) == false)
                {
                    manager.OpenCamera(_cameraId, _stateCallback, BackgroundHandler);
                }
            }
            catch (Java.Lang.SecurityException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
            catch (CameraAccessException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
            catch (InterruptedException ex)
            {
                throw new RuntimeException("Interrupted while trying to lock camera opening.", ex);
            }
        }

        // Closes the current {@link CameraDevice}.
        protected void CloseCamera()
        {
            if (CaptureSession == null)
            {
                return;
            }

            try
            {
                CameraOpenCloseLock.Acquire();
                if (null != CaptureSession)
                {
                    try
                    {
                        CaptureSession.StopRepeating();
                        CaptureSession.AbortCaptures();
                    }
                    catch (CameraAccessException ex)
                    {
#if DEBUG
                        ex.PrintStackTrace();
#endif
                    }
                    catch (IllegalStateException ex)
                    {
#if DEBUG
                        ex.PrintStackTrace();
#endif
                    }

                    CaptureSession.Close();
                    CaptureSession = null;
                }

                if (null != CameraDevice)
                {
                    CameraDevice.Close();
                    CameraDevice = null;
                }

                if (null != _imageReader)
                {
                    _imageReader.Close();
                    _imageReader = null;
                }
            }
            catch (InterruptedException ex)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.", ex);
            }
            finally
            {
                CameraOpenCloseLock.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseCamera();
                StopBackgroundThread();

                MessagingCenter.Unsubscribe<PickPhotosPage>(this, "OnAppearing");
                MessagingCenter.Unsubscribe<PickPhotosPage>(this, "OnDisappearing");

                if (Element != null)
                {
                    Element.StartRecording = null;
                    Element.StopRecording = null;
                }
            }

            base.Dispose(disposing);
            GC.SuppressFinalize(this);
        }

        // Starts a background thread and its {@link Handler}.
        protected void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread("CameraBackground");
            _backgroundThread.Start();
            BackgroundHandler = new Handler(_backgroundThread.Looper);
        }

        // Stops the background thread and its {@link Handler}.
        protected void StopBackgroundThread()
        {
            if (_backgroundThread == null)
            {
                return;
            }

            _backgroundThread.QuitSafely();
            try
            {
                _backgroundThread.Join();
                _backgroundThread = null;
                BackgroundHandler = null;
            }
            catch (InterruptedException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
        }

        // Creates a new {@link CameraCaptureSession} for camera preview.
        public void CreateCameraPreviewSession()
        {
            try
            {
                SurfaceTexture texture = _textureView.SurfaceTexture;
                if (texture == null)
                {
                    throw new IllegalStateException("texture is null");
                }

                // We configure the size of default buffer to be the size of camera preview we want.
                texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);

                // This is the output Surface we need to start preview.
                Surface surface = new Surface(texture);

                // We set up a CaptureRequest.Builder with the output Surface.
                PreviewRequestBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                PreviewRequestBuilder.AddTarget(surface);

                // Here, we create a CameraCaptureSession for camera preview.
                List<Surface> surfaces = new List<Surface> { surface, _imageReader.Surface };

                CameraDevice.CreateCaptureSession(surfaces, new CameraCaptureSessionCallback(this), BackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
        }

        public static T Cast<T>(Java.Lang.Object obj) where T : class
        {
            var propertyInfo = obj.GetType().GetProperty("Instance");
            return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as T;
        }

        // Configures the necessary {@link android.graphics.Matrix}
        // transformation to `mTextureView`.
        // This method should be called after the camera preview size is determined in
        // setUpCameraOutputs and also the size of `mTextureView` is fixed.

        public void ConfigureTransform(int viewWidth, int viewHeight)
        {
            Activity activity = Activity;
            if (null == _textureView || null == _previewSize || null == activity)
            {
                return;
            }

            var rotation = (int)activity.WindowManager.DefaultDisplay.Rotation;
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, _previewSize.Height, _previewSize.Width);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();

            if (rotation == (int)SurfaceOrientation.Rotation90 || rotation == (int)SurfaceOrientation.Rotation270)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = JMath.Max((float)viewHeight / _previewSize.Height, (float)viewWidth / _previewSize.Width);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * (rotation - 2), centerX, centerY);
            }
            else if (rotation == (int)SurfaceOrientation.Rotation180)
            {
                matrix.PostRotate(180, centerX, centerY);
            }
            _textureView.SetTransform(matrix);
        }

        // Initiate a still image capture.
        protected void TakePicture()
        {
            //LockFocus();
            CaptureStillPicture();
        }

        // Lock the focus as the first step for a still image capture.
        protected void LockFocus()
        {
            try
            {
                // This is how to tell the camera to lock focus.
                PreviewRequestBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);

                // Tell #mCaptureCallback to wait for the lock.
                State = Camera2State.WaitingLock;
                
                CaptureSession.Capture(PreviewRequestBuilder.Build(), CaptureCallback, BackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
        }

        // Run the precapture sequence for capturing a still image. This method should be called when
        // we get a response in {@link #mCaptureCallback} from {@link #lockFocus()}.
        public void RunPrecaptureSequence()
        {
            try
            {
                // This is how to tell the camera to trigger.
                PreviewRequestBuilder.Set(CaptureRequest.ControlAePrecaptureTrigger, (int)ControlAEPrecaptureTrigger.Start);
                // Tell #mCaptureCallback to wait for the precapture sequence to be set.
                State = Camera2State.WaitingPrecapture;

                CaptureSession.Capture(PreviewRequestBuilder.Build(), CaptureCallback, BackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
        }

        // Capture a still picture. This method should be called when we get a response in
        // {@link #mCaptureCallback} from both {@link #lockFocus()}.
        public void CaptureStillPicture()
        {
            try
            {
                var activity = Activity;
                if (null == activity || null == CameraDevice)
                {
                    return;
                }

                // This is the CaptureRequest.Builder that we use to take a picture.
                //if (_stillCaptureBuilder == null)
                //{
                _stillCaptureBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                //}

                _stillCaptureBuilder.AddTarget(_imageReader.Surface);

                // Use the same AE and AF modes as the preview.
                _stillCaptureBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
                SetAutoFlash(_stillCaptureBuilder);

                CaptureSession.StopRepeating();
                CaptureSession.Capture(_stillCaptureBuilder.Build(), new CameraCaptureStillPictureSessionCallback(this), null);
            }
            catch (CameraAccessException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
        }

        // Unlock the focus. This method should be called when still image capture sequence is
        // finished.
        public void UnlockFocus()
        {
            try
            {
                // Reset the auto-focus trigger
                PreviewRequestBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Cancel);

                SetAutoFlash(PreviewRequestBuilder);
                CaptureSession.Capture(PreviewRequestBuilder.Build(), CaptureCallback, BackgroundHandler);

                // After this, the camera will go back to the normal state of preview.
                State = Camera2State.Preview;
                CaptureSession.SetRepeatingRequest(PreviewRequest, CaptureCallback, BackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
#if DEBUG
                ex.PrintStackTrace();
#endif
            }
        }

        public void SetAutoFlash(CaptureRequest.Builder requestBuilder)
        {
            if (_isFlashSupported == true)
            {
                requestBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.OnAutoFlash);
            }
        }
    }
}