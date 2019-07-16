using System;
using Plugin.MultiPictures.Utils;
using Xamarin.Forms;

namespace Plugin.MultiPictures.Views
{
    public class CameraPreview : View
    {
        public event EventHandler<MediaFile> ImageAvailable;

        public event EventHandler<DeviceRotation> RotationChanged;

        public CameraPreview()
        {
            CrossMultiPictures.Current.Camera = this;
            StopRecording += delegate
            {
                CrossMultiPictures.Current.Camera = null;
            };
        }

        public static readonly BindableProperty CameraProperty = BindableProperty.Create(
            nameof(Camera), typeof(CameraOptions), typeof(CameraPreview), CameraOptions.Rear);

        public CameraOptions Camera
        {
            get { return (CameraOptions)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public static readonly BindableProperty MediaOptionsProperty = BindableProperty.Create(
            nameof(MediaOptions), typeof(MediaOptions), typeof(CameraPreview), null, BindingMode.TwoWay, propertyChanged: OnMediaOptionsChanged);

        protected static void OnMediaOptionsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var view = (CameraPreview)bindable;
            view.MediaOptions = (MediaOptions)newValue;
        }

        public MediaOptions MediaOptions
        {
            get { return (MediaOptions)GetValue(MediaOptionsProperty); }
            set { SetValue(MediaOptionsProperty, value); }
        }

        public Action StartRecording;

        public Action StopRecording;

        public Action Dispose;

        public void OnImageAvailable(MediaFile mediaFile)
        {
            ImageAvailable?.Invoke(this, mediaFile);
        }

        public void OnRotationChanged(DeviceRotation rotation)
        {
            RotationChanged?.Invoke(this, rotation);
        }
    }
}
