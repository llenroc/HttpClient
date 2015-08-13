// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Net;
    using System.Collections.Concurrent;

    internal class ServicePointManager
    {
        private static ConcurrentDictionary<string, ServicePoint> _servicePointTable = new ConcurrentDictionary<string, ServicePoint>();

        public static ServicePoint FindServicePoint(Uri address, IWebProxy proxy)
        {
            var isProxyServicePoint = false;
            if (proxy != null && !address.IsLoopback)
            {
                if (!proxy.IsBypassed(address))
                {
                    address = proxy.GetProxy(address);
                    isProxyServicePoint = true;
                }
            }
            var queryKey = MakeQueryString(address);
            var servicePoint = _servicePointTable.GetOrAdd(queryKey, (key) =>
            {
                return new ServicePoint(address, 10, key, isProxyServicePoint);
            });
            return servicePoint;
        }

        private static string MakeQueryString(Uri address)
        {
            if (address.IsDefaultPort)
            {
                return address.Scheme + "://" + address.Host;
            }
            return string.Concat(new string[] { address.Scheme, "://", address.DnsSafeHost, ":", address.Port.ToString() });
        }
    }
}
