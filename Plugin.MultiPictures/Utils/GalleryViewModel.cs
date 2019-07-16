
using System.ComponentModel;

namespace Plugin.MultiPictures.Utils
{
    public class PickPhotosViewModel : INotifyPropertyChanged
    {
        protected double screenWidth;
        protected double screenHeight;
        protected int columnsCount;
        protected int selectionCount;
        protected bool isSelecting;

        public double ScreenWidth
        {
            get => screenWidth;
            set
            {
                if ((int)screenWidth != (int)value)
                {
                    screenWidth = value;
                    OnPropertyChanged(nameof(ScreenWidth));
                }
            }
        }

        public int ColumnWidth => ((int)ScreenWidth - 2 * (ColumnsCount + 1) * Padding) / ColumnsCount;

        public int Padding => 4;

        public double ScreenHeight
        {
            get => screenHeight;
            set
            {
                if ((int)screenHeight != (int)value)
                {
                    screenHeight = value;
                    OnPropertyChanged(nameof(ScreenHeight));
                }
            }
        }

        public int ColumnsCount
        {
            get => columnsCount;
            set
            {
                if (columnsCount != value)
                {
                    columnsCount = value;
                    OnPropertyChanged(nameof(ColumnsCount));
                    OnPropertyChanged(nameof(ColumnWidth));
                }
            }
        }

        public bool IsSelecting
        {
            get => isSelecting;
            set
            {
                if (isSelecting != value)
                {
                    isSelecting = value;
                    OnPropertyChanged(nameof(IsSelecting));
                }
            }
        }

        public int SelectionCount
        {
            get => selectionCount;
            set
            {
                if (selectionCount != value)
                {
                    selectionCount = value;
                    OnPropertyChanged(nameof(SelectionCount));
                }
            }
        }

        public PickPhotosViewModel()
        {
            ScreenWidth = 200;
            ScreenHeight = 200;
            ColumnsCount = 2;
            IsSelecting = false;
            SelectionCount = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
