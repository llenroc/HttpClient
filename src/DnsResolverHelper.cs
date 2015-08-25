// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net;

    internal class DnsResolverHelper
    {
        private IPHostEntry _host;
        private DateTime _lastUpdateTime;
        private int _index;
        private Uri _uri;

        internal DnsResolverHelper(Uri uri)
        {
            _uri = uri;
            _lastUpdateTime = DateTime.MinValue;
        }

        internal IPEndPoint GetHostEndPoint()
        {
            lock (this)
            {
                if (_host == null || ServicePointManager.DnsRefreshTimeout >= 0 && (DateTime.Now - _lastUpdateTime).TotalMilliseconds > ServicePointManager.DnsRefreshTimeout)
                {
                    var uriHost = _uri.Host;
                    if (_uri.HostNameType == UriHostNameType.IPv6 || _uri.HostNameType == UriHostNameType.IPv4)
                    {
                        if (_uri.HostNameType == UriHostNameType.IPv6)
                        {
                            // Remove square brackets
                            uriHost = uriHost.Substring(1, uriHost.Length - 2);
                        }
                        _host = new IPHostEntry();
                        _host.AddressList = new IPAddress[] { IPAddress.Parse(uriHost) };
                    }
                    else
                    {
                        _host = Dns.GetHostEntry(uriHost);
                    }
                    _lastUpdateTime = DateTime.Now;
                }
                var index = ServicePointManager.EnableDnsRoundRobin ? ((uint)_index++ % _host.AddressList.Length) : _index;
                return new IPEndPoint(_host.AddressList[index], _uri.Scheme == Uri.UriSchemeHttps ? (_uri.IsDefaultPort ? 443 : _uri.Port) : _uri.Port);
            }
        }
    }
}
