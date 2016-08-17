using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nextcloud.DAV
{
    class DAVRequestHeader
    {
        /// <summary>
        /// DAV Methods.
        /// </summary>
        public struct Method
        {
            /// <summary>
            /// Used to fetch avaiable server options.
            /// </summary>
            public const string Options = "OPTIONS";

            /// <summary>
            /// Used to fetch resource properties.
            /// </summary>
            public const string PropertyFind = "PROPFIND";

            /// <summary>
            /// Sets or remove properties.
            /// </summary>
            public const string PropertyPatch = "PROPPATCH";

            /// <summary>
            /// Creates a new collection resource (a directory).
            /// </summary>
            public const string MakeCollection = "MKCOL";

            /// <summary>
            /// Deletes a resource.
            /// </summary>
            public const string Delete = "DELETE";

            /// <summary>
            /// Creates a resource.
            /// </summary>
            public const string Put = "PUT";

            /// <summary>
            /// Copies a resource to another uri destination.
            /// </summary>
            public const string Copy = "COPY";

            /// <summary>
            /// Moves a resource from a destination to another.
            /// </summary>
            public const string Move = "MOVE";

            /// <summary>
            /// Locks a resource.
            /// </summary>
            public const string Lock = "LOCK";

            /// <summary>
            /// Unlocks a locked resource.
            /// </summary>
            public const string UnLock = "UNLOCK";

            public const string Report = "REPORT";
        }

        /// <summary>
        /// Additional headers to be used.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get;
            set;
        }

        string _reqResource = "";
        /// <summary>
        /// Resource to be used.
        /// </summary>
        public string RequestedResource
        {
            get
            {
                return _reqResource;
            }
            set
            {
                _reqResource = value.TrimStart('/');
            }
        }

        /// <summary>
        /// Method to be used.
        /// </summary>
        public string RequestedMethod
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new request header object.
        /// </summary>
        /// <param name="method">The RequestHeader.Method to be used</param>
        /// <param name="resource">A resource URI to work with</param>
        /// <param name="headers">Additional headers this request should have</param>
        public DAVRequestHeader(string method, string resource, Dictionary<string, string> headers = null)
        {
            if (headers != null) Headers = headers;
            else
            {
                Headers = new Dictionary<string, string>();
            }
            RequestedResource = resource;
            RequestedMethod = method;

            Headers.Add(Header.ContentType, "application/xml; charset=\"utf-8\"");
        }

        /// <summary>
        /// Creates a listening request.
        /// </summary>
        /// <param name="path">A relative path to the resource.</param>
        /// <returns></returns>
        static public DAVRequestHeader CreateListing(string path = "/")
        {
            return new DAVRequestHeader(Method.PropertyFind, path, new Dictionary<string, string>()
            {
                {Header.Depth, HeaderAttribute.MethodDepth.ApplyResourceAndChildren}
            });
        }

        /// <summary>
        /// Creates a make collection (new folder) request.
        /// </summary>
        /// <param name="path">The full path of the collection.</param>
        /// <returns></returns>
        static public DAVRequestHeader MakeCollection(string path)
        {
            return new DAVRequestHeader(Method.MakeCollection, path);
        }

        static public DAVRequestHeader CreateReport(string path) {
            return new DAVRequestHeader(Method.Report, path, new Dictionary<string, string>()
            {
                {Header.Depth, HeaderAttribute.MethodDepth.ApplyResourceAndChildren},
                {"Prefer", "return_minimal"}
            });
        }

        /// <summary>
        /// Creates a delete request.
        /// </summary>
        /// <param name="path">The full path of the item to delete.</param>
        /// <returns></returns>
        static public DAVRequestHeader Delete(string path)
        {
            return new DAVRequestHeader(Method.Delete, path);
        }
    }
}
