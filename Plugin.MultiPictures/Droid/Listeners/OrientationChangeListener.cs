using System;
using Android.Content;
using Android.Views;

namespace Plugin.MultiPictures.Droid.Listeners
{
    public class OrientationChangeListener : OrientationEventListener
    {
        private readonly Action<int> _onChanged;

        public OrientationChangeListener(Context context, Action<int> handler) : base(context)
        {
            _onChanged = handler;
        }

        public override void OnOrientationChanged(int orientation)
        {
            _onChanged?.Invoke(orientation);
        }
    }
}