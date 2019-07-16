using System;

namespace Plugin.MultiPictures
{
    /// <summary>
    /// Cross Plugin.MultiPictures
    /// </summary>
    public static class CrossMultiPictures
    {
        static Lazy<IMultiPictures> implementation = new Lazy<IMultiPictures>(() => CreateInstance(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Gets if the plugin is supported on the current platform.
        /// </summary>
        public static bool IsSupported => implementation.Value == null ? false : true;

        /// <summary>
        /// Current plugin implementation to use
        /// </summary>
        public static IMultiPictures Current
        {
            get
            {
                IMultiPictures ret = implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        static IMultiPictures CreateInstance()
        {
#if NETSTANDARD1_0 || NETSTANDARD2_0
            return null;
#else
            return new MultiPicturesImplementation();
#endif
        }

        internal static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");

    }
}
