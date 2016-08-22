using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using Nextcloud.Data;

namespace Nextcloud.DAV
{
    class DAVRequestBody
    {

        /// <summary>
        /// The finialized xml request to grap.
        /// </summary>
        public Stream XmlBody
        {
            get;
            private set;
        }


        /// <summary>
        /// Creates a new request body.
        /// </summary>
        /// <param name="rootElement"></param>
        public DAVRequestBody(Item rootElement)
        {
            XmlBody = new MemoryStream();
            var writer = XmlWriter.Create(XmlBody);
            writer.WriteStartDocument();
            _InsertNode(writer, rootElement);
            writer.WriteEndDocument();
            writer.Dispose();
        }


        private void _InsertNode(XmlWriter writer, Item item)
        {
            if (item.IsNamespaced)
            {
                writer.WriteStartElement(item.LocalName, item.Namespace);
            }
            else
            {
                writer.WriteStartElement(item.LocalName);
            }

            foreach (Item.AttributeNode attr in item.Attributes)
            {
                if (attr.IsNamespaced)
                {
                    writer.WriteAttributeString(attr.LocalName, attr.Namespace, attr.Value);
                }
                else
                {
                    writer.WriteAttributeString(attr.LocalName, attr.Value);
                }
            }

            if (item.HasContent)
            {
                if (item.IsNamespaced)
                {
                    writer.WriteElementString(item.LocalName, item.Namespace, item.LocalValue.ToString());
                }
                else
                {
                    writer.WriteElementString(item.LocalName, item.LocalValue.ToString());
                }
            }

            if (item.HasChildren)
            {
                foreach (Item child in item.Children)
                {
                    _InsertNode(writer, child);
                }
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Creates a listening request with all default properties.
        /// </summary>
        /// <returns></returns>
        static public DAVRequestBody CreateAllPropertiesListing()
        {
            return new DAVRequestBody(
                new Item(Elements.PropertyFind, new List<Item>() {
                    new Item(Elements.Properties, new List<Item> {
                        new Item(Properties.SupportedLock),
                        new Item(Properties.LockDiscovery),
                        new Item(Properties.GetContentLength),
                        new Item(Properties.GetContentType),
                        new Item(Properties.GetETag),
                        new Item(Properties.GetLastModified),
                        new Item(Properties.ResourceType),
                        new Item(Properties.QuotaAvailableBytes),
                        new Item(Properties.QuotaUsedBytes),
                        new Item(Properties.GetContentLanguage),
                        new Item(Properties.CreationDate),
                        new Item(Properties.DisplayName)
                    })
                })
            );
        }

        /// <summary>
        /// Creates a listening request with the CalDAV properties.
        /// </summary>
        /// <returns></returns>
        static public DAVRequestBody CreateCalendarPropertiesListening() {
            return new DAVRequestBody(
                new Item(Elements.PropertyFind, new List<Item>() {
                    new Item(Elements.Properties, new List<Item> {
                        new Item(Properties.DisplayName),
                        new Item(Properties.GetCTag, ns:XmlNamespaces.NsCalenderServer),
                        new Item(Properties.CalendarColor, ns:XmlNamespaces.NsAppleIcal)
                    }),
                    new Item(Elements.Filter, new List<Item> {
                        new Item(Filters.CompFilter, new List<Item>() {
                        }, ns:XmlNamespaces.NsCaldav) {
                            Attributes = new List<Item.AttributeNode> { new Item.AttributeNode("name", "VCALENDAR", ns:"") }
                        }
                    }, ns:XmlNamespaces.NsCaldav)
                })
            );
        }

        /// <summary>
        /// Creates a request for all calendar events.
        /// </summary>
        /// <returns></returns>
        static public DAVRequestBody CreateCondensedCalendarRequest() {
            return new DAVRequestBody(
                new Item(Elements.CalendarQuery, new List<Item>() {
                    new Item(Elements.Properties, new List<Item> {
                        new Item(Properties.GetETag)
                    }),
                    new Item(Elements.Filter, new List<Item> {
                        new Item(Filters.CompFilter, new List<Item>() {
                            new Item(Filters.CompFilter, ns:XmlNamespaces.NsCaldav) {
                                Attributes = new List<Item.AttributeNode>() { new Item.AttributeNode("name", "VEVENT", ns:"") }
                            }
                        }, ns:XmlNamespaces.NsCaldav) {
                            Attributes = new List<Item.AttributeNode> { new Item.AttributeNode("name", "VCALENDAR", ns:"") }
                        }
                    }, ns:XmlNamespaces.NsCaldav)
                }, ns:XmlNamespaces.NsCaldav)
            );
        }

        internal static DAVRequestBody CreateCalendarMultiget(List<CalendarObject> unsyncedCalObjs) {
            List<Item> multigetChildren = new List<Item>() {
                    new Item(Elements.Properties, new List<Item> {
                        new Item(Properties.GetETag),
                        new Item(Properties.CalendarData, ns:XmlNamespaces.NsCaldav)
                    })
                };
            multigetChildren.AddRange(unsyncedCalObjs.Select(e => new Item(Elements.Reference, e.Path)));
            return new DAVRequestBody(new Item(Elements.CalendarMultiget, multigetChildren, ns: XmlNamespaces.NsCaldav));
        }

        private static async void dumpXmlToFile(string filename, byte[] buffer) {
            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteBytesAsync(file, buffer);
        }

        /// <summary>
        /// Item-Sub-Class
        /// </summary>
        public class Item
        {

            public string LocalName = "";
            public object LocalValue = null;
            public string Namespace = "";

            public struct AttributeNode
            {
                public string Namespace;
                public string LocalName;
                public string Value;

                /// <summary>
                /// Finds out if we are using a namespace.
                /// </summary>
                public bool IsNamespaced
                {
                    get
                    {
                        return Namespace.Length > 0;
                    }
                    private set
                    {
                    }
                }

                /// <summary>
                /// Creates a new namespaced Attribute.
                /// </summary>
                /// <param name="localName">The local name.</param>
                /// <param name="ns">The namespace URI.</param>
                /// <param name="value">A value.</param>
                public AttributeNode(string localName, object value, string ns = XmlNamespaces.NsDav)
                {
                    LocalName = localName;
                    Value = value.ToString();
                    Namespace = ns;
                }
            }

            /// <summary>
            /// Holds the item attributes.
            /// </summary>
            public List<AttributeNode> Attributes
            {
                get;
                set;
            }

            public List<Item> Children
            {
                get;
                set;
            }

            /// <summary>
            /// Determines if there are any children.
            /// </summary>
            /// <returns></returns>
            public bool HasChildren
            {
                get
                {
                    return Children.Count > 0;
                }
                private set
                {
                }
            }

            public bool HasContent
            {
                get
                {
                    return LocalValue != null && !string.IsNullOrWhiteSpace(LocalValue.ToString());
                }
                set
                {
                }
            }

            /// <summary>
            /// Finds out if we are using a namespace.
            /// </summary>
            public bool IsNamespaced
            {
                get
                {
                    return Namespace.Length > 0;
                }
                private set
                {
                }
            }

            /// <summary>
            /// Creates a new item.
            /// </summary>
            /// <param name="localName">The local node name</param>
            /// <param name="value">Text or another element.</param>
            /// <param name="ns">The Namespace URI</param>
            public Item(string localName, string value = "", string ns = XmlNamespaces.NsDav)
            {
                Children = new List<Item>();
                Attributes = new List<AttributeNode>();
                LocalName = localName;
                LocalValue = value;
                Namespace = ns;
            }

            /// <summary>
            /// Creates a new item.
            /// </summary>
            /// <param name="localName">The local node name</param>
            /// <param name="children">Child element</param>
            /// <param name="value">Text or another element.</param>
            /// <param name="ns">The Namespace URI</param>
            public Item(string localName, Item children, string value = "", string ns = XmlNamespaces.NsDav)
            {
                Children = new List<Item>() { children };
                Attributes = new List<AttributeNode>();
                LocalName = localName;
                LocalValue = value;
                Namespace = ns;
            }

            /// <summary>
            /// Creates a new item.
            /// </summary>
            /// <param name="localName">The local node name</param>
            /// <param name="children">Child elements</param>
            /// <param name="value">Text or another element.</param>
            /// <param name="ns">The Namespace URI</param>
            public Item(string localName, List<Item> children, string value = "", string ns = XmlNamespaces.NsDav)
            {
                Children = children;
                Attributes = new List<AttributeNode>();
                LocalName = localName;
                LocalValue = value;
                Namespace = ns;
            }
        }
    }
}
