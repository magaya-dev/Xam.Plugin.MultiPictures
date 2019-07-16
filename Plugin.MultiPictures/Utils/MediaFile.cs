
using System;
using System.IO;

namespace Plugin.MultiPictures.Utils
{
    public class MediaFile : IDisposable
    {
        protected readonly string _path;

        protected bool _isDisposed;

        protected Func<Stream> _streamGetter;

        public string Path
        {
            get
            {
                if (_isDisposed == true)
                {
                    throw new ObjectDisposedException(null);
                }

                return _path;
            }
        }

        public MediaFile(string path)
        {
            _path = path;
            _streamGetter = new Func<Stream>(() => File.OpenRead(_path));
        }

        public Stream GetStream()
        {
            if (_isDisposed == true)
            {
                throw new ObjectDisposedException(null);
            }

            return _streamGetter();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if(_isDisposed == true)
            {
                return;
            }

            _isDisposed = true;
            if(disposing == true)
            {
                _streamGetter = null;
            }
        }

        ~MediaFile()
        {
            Dispose(false);
        }
    }
}
