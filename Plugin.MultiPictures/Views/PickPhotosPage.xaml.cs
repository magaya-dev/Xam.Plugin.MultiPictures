
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plugin.MultiPictures.Utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Plugin.MultiPictures.Views
{
    [DesignTimeVisible(true)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PickPhotosPage : ContentPage
    {
        public PickPhotosViewModel PickPhotosVM { get; }

        public IList<PreviewMediaFile> ImageSources { get; }

        protected IList<MediaFile> _photos;

        protected IList<MediaFile> _selectedPhotos;

        protected MediaOptions _mediaOptions;

        protected event EventHandler PhotosLoaded;

        public event EventHandler<IList<MediaFile>> PhotosSelected;

        public PickPhotosPage()
        {
            PickPhotosVM = new PickPhotosViewModel();
            ImageSources = new ObservableCollection<PreviewMediaFile>();

            InitializeComponent();

            BindingContext = PickPhotosVM;
            PhotosLoaded += OnPhotosLoadedAsync;
        }

        public PickPhotosPage(IList<MediaFile> photos) : this()
        {
            _photos = photos;
            PhotosLoaded.Invoke(null, EventArgs.Empty);
        }

        public PickPhotosPage(MediaOptions mediaOptions) : this()
        {
            _photos = new List<MediaFile>();
            _mediaOptions = mediaOptions;

            var loadPhotos = new Action(async () =>
            {
                await LoadFromDirectoryAsync();
            });

            loadPhotos.Invoke();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            MessagingCenter.Send(this, "OnAppearing");
        }

        protected async Task LoadFromDirectoryAsync()
        {
            if (_mediaOptions.UsePublicStorage == true)
            {
                try
                {
                    await CrossMultiPictures.Current.HasPermissions(Plugin.Permissions.Abstractions.Permission.Storage);
                }
                catch (MediaPermissionException ex)
                {
                    await DisplayAlert("Permission Not Granted", ex.Message, "OK");
                }
            }

            var directory = CrossMultiPictures.Current.MediaFolderPath(_mediaOptions);
            var directoryInfo = new DirectoryInfo(directory);
            var filesInfo = directoryInfo.GetFiles("*.jpg", SearchOption.TopDirectoryOnly);

            foreach (var fi in filesInfo)
            {
                _photos.Add(new MediaFile(fi.FullName));
            }

            PhotosLoaded.Invoke(null, EventArgs.Empty);
        }

        protected async void OnPhotosLoadedAsync(object sender, EventArgs args)
        {
            var thumbnailOptions = new MediaOptions
            {
                CompressionQuality = 80,
                PhotoSize = PhotoSize.Small,
                SaveMetaData = false,
                UsePublicStorage = _mediaOptions?.UsePublicStorage ?? false
            };

            foreach (var photo in _photos)
            {
                var bytes = await CrossMultiPictures.Current.ResizedStream(photo, thumbnailOptions);
                ImageSources.Add(new PreviewMediaFile(photo.Path, bytes));
            }
        }

        protected override void OnDisappearing()
        {
            MessagingCenter.Send(this, nameof(OnDisappearing));
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if ((int)PickPhotosVM.ScreenWidth != (int)width && (int)PickPhotosVM.ScreenHeight != (int)height)
            {
                PickPhotosVM.ScreenWidth = width;
                PickPhotosVM.ScreenHeight = height;

                if (PickPhotosVM.ScreenWidth < PickPhotosVM.ScreenHeight) // Portrait
                {
                    PickPhotosVM.ColumnsCount = 2;
                }
                else // Landscape
                {
                    PickPhotosVM.ColumnsCount = 3;
                }
            }
        }

        protected async void OnBtnGoBackClicked(object sender, EventArgs args)
        {
            await Navigation.PopModalAsync();
        }

        protected void OnBtnSelectClicked(object sender, EventArgs args)
        {
            PickPhotosVM.IsSelecting = !PickPhotosVM.IsSelecting;
            var tlbSelect = (Button)sender;

            if (PickPhotosVM.IsSelecting == true)
            {
                tlbSelect.Text = "Cancel";
                clvPhotos.SelectionMode = SelectionMode.Multiple;
            }
            else
            {
                tlbSelect.Text = "Select";
                clvPhotos.SelectionMode = SelectionMode.None;
                clvPhotos.SelectedItems.Clear();
            }
        }

        protected void ClvPhotos_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            PickPhotosVM.SelectionCount = args.CurrentSelection.Count;
        }

        protected async void OnBtnDoneClicked(object sender, EventArgs args)
        {
            _selectedPhotos = clvPhotos.SelectedItems
                                       .Cast<PreviewMediaFile>()
                                       .Select(p => new MediaFile(p.OriginalFilePath)).ToList();

            PhotosSelected?.Invoke(this, _selectedPhotos);

            await Navigation.PopModalAsync();
        }
    }
}
