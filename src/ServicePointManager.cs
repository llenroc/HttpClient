// Copyright (c) 2015 Yamool. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

namespace Yamool.Net.Http
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Security;
    using System.Threading;

    public static class ServicePointManager
    {
        private static ConcurrentDictionary<string, Lazy<ServicePoint>> _servicePoints = new ConcurrentDictionary<string, Lazy<ServicePoint>>();
        private static int _defaultConnectionLimit = 100;
        private static int _maxServicePointIdleTime = 100000;
        private static int _dnsRefreshTimeout = 120000;
        private static bool _usednsRoundRobin;
        private static int _maxServicePoints;
        internal static RemoteCertificateValidationCallback _server_cert_cb;
        internal static bool _useNagle = true;
        internal static bool _tcp_keepalive;
        internal static int _tcp_keepalive_time;
        internal static int _tcp_keepalive_interval;
        internal static readonly TimerCallback _idleServicePointTimeoutDelegate = new TimerCallback(IdleServicePointTimeoutCallback);

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections allowed by a <see cref="ServicePoint"/> object.
        /// </summary>
        public static int DefaultConnectionLimit
        {
            get
            {
                return _defaultConnectionLimit;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                _defaultConnectionLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates how long a DNS resolution is considered valid.
        /// </summary>
        public static int DnsRefreshTimeout
        {
            get
            {
                return _dnsRefreshTimeout;
            }
            set
            {
                _dnsRefreshTimeout = Math.Max(-1, value);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a DNS resolution rotates among the applicable Internet Protocol (IP) addresses.
        /// </summary>
        public static bool EnableDnsRoundRobin
        {
            get
            {
                return _usednsRoundRobin;
            }
            set
            {
                _usednsRoundRobin = true;
            }
        }

        /// <summary>
        /// Gets or sets the maximum idle time of a ServicePoint object.
        /// If values is -1 that means never be idle.
        /// </summary>
        public static int MaxServicePointIdleTime
        {
            get
            {
                return _maxServicePointIdleTime;
            }
            set
            {
                if (value < -1 || value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                _maxServicePointIdleTime = value;
            }
        }

        public static int MaxServicePoints
        {
            get
            {
                return _maxServicePoints;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                _maxServicePoints = value;
            }
        }

        /// <summary>
        /// Gets or sets the callback to validate a server certificate.
        /// </summary>
        public static RemoteCertificateValidationCallback ServerCertificateValidationCallback
        {
            get
            {
                return _server_cert_cb;
            }
            set
            {
                _server_cert_cb = value;
            }
        }

        /// <summary>
        /// Determines whether the Nagle algorithm is used by the service points managed by this ServicePointManager object.
        /// </summary>
        public static bool UseNagleAlgorithm
        {
            get
            {
                return _useNagle;
            }
            set
            {
                _useNagle = value;
            }
        }

        internal static TimerCallback IdleServicePointTimeoutDelegate
        {
            get
            {
                return _idleServicePointTimeoutDelegate;
            }
        }

        /// <summary>
        /// Enables or disables the keep-alive option on a TCP connection.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="keepAliveTime"></param>
        /// <param name="keepAliveInterval"></param>
        public static void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval)
        {
            if (enabled)
            {
                if (keepAliveTime <= 0)
                {
                    throw new ArgumentOutOfRangeException("keepAliveTime", "Must be greater than 0");
                }
                if (keepAliveInterval <= 0)
                {
                    throw new ArgumentOutOfRangeException("keepAliveInterval", "Must be greater than 0");
                }
            }
            _tcp_keepalive = enabled;
            _tcp_keepalive_time = keepAliveTime;
            _tcp_keepalive_interval = keepAliveInterval;
        }

        public static ServicePoint FindServicePoint(Uri address)
        {
            return FindServicePoint(address, null);
        }

        public static ServicePoint FindServicePoint(string uriString, IWebProxy proxy)
        {
            return FindServicePoint(new Uri(uriString), proxy);
        }

        public static ServicePoint FindServicePoint(Uri address, IWebProxy proxy)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }
            var usesProxy = false;
            if (proxy != null && !proxy.IsBypassed(address))
            {
                usesProxy = true;
                var isSecure = address.Scheme == Uri.UriSchemeHttps;
                address = proxy.GetProxy(address);
                if (address.Scheme != Uri.UriSchemeHttp && !isSecure)
                {
                    throw new NotSupportedException("Proxy scheme not supported.");
                }
            }
            var key = MakeQueryString(address, usesProxy);
            return _servicePoints.GetOrAdd(key, new Lazy<ServicePoint>(() =>
            {
                if (_maxServicePoints > 0 && _servicePoints.Count >= _maxServicePoints)
                {
                    throw new InvalidOperationException("maximum number of service points reached");
                }
                return new ServicePoint(address, _defaultConnectionLimit, key, usesProxy);
            }, false)).Value;
        }

        private static void IdleServicePointTimeoutCallback(object state)
        {
            var servicePoint = (ServicePoint)state;
            Lazy<ServicePoint> idleServicePoint = null;
            if (_servicePoints.TryRemove(servicePoint.LookupString, out idleServicePoint))
            {
                servicePoint.ReleaseAllConnections();
            }
        }

        private static string MakeQueryString(Uri address)
        {
            if (address.IsDefaultPort)
            {
                return address.Scheme + "://" + address.Host;
            }
            return string.Concat(new string[] { address.Scheme, "://", address.DnsSafeHost, ":", address.Port.ToString() });
        }

        private static string MakeQueryString(Uri address, bool isProxy)
        {
            if (isProxy)
            {
                return MakeQueryString(address) + "://proxy";
            }
            return MakeQueryString(address);
        }
    }
}
