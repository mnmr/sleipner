﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DR.Sleipner.CacheProxy;

namespace DR.Sleipner.EnyimMemcachedProvider
{
    public static class ProxyRequestExtensions
    {
        public static string CreateHash<T, TResult>(this ProxyRequest<T, TResult> proxyRequest, string cachePool) where T : class
        {
            var sb = new StringBuilder();
            sb.Append(typeof (T).FullName);
            sb.Append(" - ");
            sb.Append(proxyRequest.Method);
            if (!string.IsNullOrWhiteSpace(cachePool))
            {
                sb.Append(" - ");
                sb.Append(cachePool);
            }
            sb.Append(" - ");
            sb.AddParameterRepresentations(proxyRequest.Parameters);
            

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hashAlgorithm = new SHA256Managed();
            var hash = hashAlgorithm.ComputeHash(bytes);
            
            return Convert.ToBase64String(hash);
        }

        public static void AddParameterRepresentations(this StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append(".null");
            }
            else if (value is string)
            {
                builder.Append("\"" + value + "\"");
            }
            else if (value is IEnumerable)
            {
                var ienum = (IEnumerable)value;
                var collection = ienum.Cast<object>().ToArray();
                builder.Append("[");
                for(var i = 0; i < collection.Length; i++)
                {
                    builder.AddParameterRepresentations(collection[i]);
                    if (i < collection.Length-1)
                    {
                        builder.Append(",");
                    }
                    
                }
                builder.Append("]");
            }
            else if (value is DateTime)
            {
                var dt = (DateTime)value;
                builder.Append(dt.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(value);
            }
        }
    }
}
