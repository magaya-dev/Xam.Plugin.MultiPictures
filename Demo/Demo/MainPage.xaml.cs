using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Plugin.MultiPictures;
using Plugin.MultiPictures.Utils;
using Plugin.MultiPictures.Views;
using Xamarin.Forms;

namespace Demo
{
    public partial class MainPage : ContentPage
    {
        private readonly ICollection<ImageSource> _photos;

        public MainPage()
        {
            _photos = new ObservableCollection<ImageSource>();

            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            lvwPhotos.ItemsSource = _photos;
        }

        private async void OnTakePicturesClicked(object sender, EventArgs args)
        {
            try
            {
                var mediaOptions = new MediaOptions
                {
                    CompressionQuality = 90,
                    PhotoSize = PhotoSize.Medium,
                    SaveMetaData = true,
                    UsePublicStorage = true,
                    //Directory = "Magaya"
                };

                var cameraView = await CrossMultiPictures.Current.CameraView(mediaOptions);
                cameraView.PicturesTaken += (s, e) =>
                {
                    _photos.Clear();
                    foreach (var pic in e)
                    {
                        _photos.Add(ImageSource.FromStream(pic.GetStream));
                    }
                };
            }
            catch (NotSupportedException)
            {
                await DisplayAlert("Not Available", "Camera is not available on this device", "OK");
            }
            catch (MediaPermissionException ex)
            {
                await DisplayAlert("Permission Denied", ex.Message, "OK");
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private async void OnPickPicturesClicked(object sender, EventArgs args)
        {
            try
            {
                var pickPage = new PickPhotosPage(new MediaOptions { /*Directory = "Magaya", */UsePublicStorage = true });
                pickPage.PhotosSelected += (s, e) =>
                {
                    _photos.Clear();
                    foreach (var pic in e)
                    {
                        _photos.Add(ImageSource.FromStream(pic.GetStream));
                    }
                };

                await Navigation.PushModalAsync(pickPage);
            }
            catch (MediaPermissionException ex)
            {
                await DisplayAlert("Permission Denied", ex.Message, "OK");
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }
    }
}
