using System.Collections.Generic;

namespace Nextcloud.DAV
{
    class XmlNamespaces
    {
        public const string NsDav = "DAV:";
        public const string NsCalenderServer = "http://calendarserver.org/ns/";
        public const string NsAppleIcal = "http://apple.com/ns/ical/";
        public const string NsCaldav = "urn:ietf:params:xml:ns:caldav";

        public static string[] GetXmlNamespaces() {
            return new string[] { NsDav, NsCalenderServer, NsAppleIcal, NsCaldav };
        }
    }
}
