
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Plugin.MultiPictures.Utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Plugin.MultiPictures.Views
{
    [DesignTimeVisible(true)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CameraView : ContentView
    {
        protected IList<MediaFile> _pictures;

        public event EventHandler<IList<MediaFile>> PicturesTaken;
        
        public CameraView(MediaOptions mediaOptions = null)
        {
            InitializeComponent();

            _cameraPreview.MediaOptions = mediaOptions ?? new MediaOptions
            {
                PhotoSize = PhotoSize.Medium,
                CompressionQuality = 90,
                SaveMetaData = true,
                UsePublicStorage = false,
                Directory = "Sample"
            };

            MessagingCenter.Subscribe<Application>(this, "OnSleep", (app) =>
            {
                OnBtnCloseClicked(this, EventArgs.Empty);
            });
        }

        protected void OnRotationChanged(object sender, DeviceRotation args)
        {
            var btnFlash = (Button)FindByName("btnDone");

            switch (args)
            {
                case DeviceRotation.Unknown:
                case DeviceRotation.Portrait:
                    btnFlash.Rotation = 0;
                    imgThumbnail.Rotation = 0;
                    break;
                case DeviceRotation.ReversePortrait:
                    btnFlash.Rotation = 180;
                    imgThumbnail.Rotation = 180;
                    break;
                case DeviceRotation.Landscape:
                    btnFlash.Rotation = 270;
                    imgThumbnail.Rotation = 270;
                    break;
                case DeviceRotation.ReverseLandscape:
                    btnFlash.Rotation = 90;
                    imgThumbnail.Rotation = 90;
                    break;
            }
        }

        protected void OnBtnTakePictureClicked(object sender, EventArgs args)
        {
            SwitchActivityIndicator(true);
            _cameraPreview.StartRecording();
        }

        protected void OnCameraPreviewImageAvailable(object sender, MediaFile args)
        {
            if (_pictures == null)
            {
                _pictures = new List<MediaFile>();
            }

            _pictures.Add(args);

            Device.BeginInvokeOnMainThread(() =>
            {
                imgThumbnail.Source = ImageSource.FromStream(args.GetStream);

                gridThumbnail.IsVisible = true;
                btnDone.IsVisible = true;

                SwitchActivityIndicator(false);
            });
        }

        protected void SwitchActivityIndicator(bool isRunning)
        {
            actIndicator.IsVisible = isRunning;
            actIndicator.IsRunning = isRunning;
        }

        public void StopCamera()
        {
            _cameraPreview.StopRecording?.Invoke();
        }

        protected async void OnBtnCloseClicked(object sender, EventArgs args)
        {
            StopCamera();
            await Navigation.PopModalAsync(sender != null);
        }

        protected void BtnDone_Clicked(object sender, EventArgs args)
        {
            OnDone(_pictures, true);
        }

        protected void OnDone(IList<MediaFile> files, bool popAnimation)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                PicturesTaken?.Invoke(this, files);
#if __ANDROID__
                OnBtnCloseClicked(popAnimation == true ? this : null, EventArgs.Empty);
#endif
            });
#if __IOS__
            OnBtnCloseClicked(popAnimation == true ? this : null, EventArgs.Empty);
#endif
        }

        protected async void Thumbnail_Tapped(object sender, EventArgs args)
        {
            var pickPage = new PickPhotosPage(_pictures);

            pickPage.PhotosSelected += (s, e) =>
            {
                OnDone(e, false);
            };

            await Navigation.PushModalAsync(pickPage);
        }
    }
}
