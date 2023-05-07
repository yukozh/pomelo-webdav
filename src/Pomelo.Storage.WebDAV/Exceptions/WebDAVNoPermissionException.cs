using System;

namespace Pomelo.Storage.WebDAV.Exceptions
{
    public class WebDAVNoPermissionException : Exception
    {
        public WebDAVNoPermissionException() : base() { }

        public WebDAVNoPermissionException(string message) : base(message) { }
    }
}
