
using System;
using Plugin.Permissions.Abstractions;

namespace Plugin.MultiPictures.Utils
{
    public class MediaPermissionException : Exception
    {
        public Permission[] Permissions { get; }

        public override string Message
        {
            get
            {
                var missing = string.Join(", ", Permissions);
                return string.Format("{0} permission{1} required.", missing, missing.Length > 1 ? "s are" : " is");
            }
        }

        public MediaPermissionException(params Permission[] permissions) : base()
        {
            Permissions = permissions;
        }
    }
}
