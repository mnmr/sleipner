﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DR.Sleipner.CacheConfiguration;
using DR.Sleipner.CacheProviders;
using DR.Sleipner.CacheProviders.DictionaryCache;
using DR.Sleipner.Model;
using DR.Sleipner.Test.TestModel;
using Moq;
using NUnit.Framework;

namespace DR.Sleipner.Test
{
    [TestFixture]
    public class SleipnerProxyTest
    {
        [Test]
        public void TestPassThrough()
        {
            var instanceMock = new Mock<IAwesomeInterface>();
            var cacheProviderMock = new Mock<ICacheProvider<IAwesomeInterface>>();

            var proxy = new SleipnerProxy<IAwesomeInterface>(instanceMock.Object, cacheProviderMock.Object);
            proxy.Object.VoidMethod();

            instanceMock.Verify(a => a.VoidMethod(), Times.Once());
        }

        [Test]
        public void TestDurationCache()
        {
            var instanceMock = new Mock<IAwesomeInterface>();
            var cacheProvider = new DictionaryCache<IAwesomeInterface>();

            var proxy = new SleipnerProxy<IAwesomeInterface>(instanceMock.Object, cacheProvider);
            proxy.Configure(a =>
                                       {
                                           a.ForAll().CacheFor(50);
                                       });

            var methodReturnValue = new[] {"", ""};
            instanceMock.Setup(a => a.ParameteredMethod("", 0)).Returns(methodReturnValue);

            proxy.Object.ParameteredMethod("", 0);
            proxy.Object.ParameteredMethod("", 0);
            proxy.Object.ParameteredMethod("", 0);

            instanceMock.Verify(a => a.ParameteredMethod("", 0), Times.Once());
        }

        [Test]
        public void TestNoCache()
        {
            var instanceMock = new Mock<IAwesomeInterface>();
            var cacheProvider = new DictionaryCache<IAwesomeInterface>();

            var proxy = new SleipnerProxy<IAwesomeInterface>(instanceMock.Object, cacheProvider);
            proxy.Configure(a =>
            {
                a.ForAll().CacheFor(50);
                a.For(b => b.ParameteredMethod("", 0)).NoCache();
            });

            var methodReturnValue = new[] { "", "" };
            instanceMock.Setup(a => a.ParameteredMethod("", 0)).Returns(methodReturnValue);
            instanceMock.Setup(a => a.ParameterlessMethod()).Returns(methodReturnValue);

            proxy.Object.ParameteredMethod("", 0);
            proxy.Object.ParameteredMethod("", 0);
            proxy.Object.ParameteredMethod("", 0);

            proxy.Object.ParameterlessMethod();
            proxy.Object.ParameterlessMethod();
            proxy.Object.ParameterlessMethod();

            instanceMock.Verify(a => a.ParameteredMethod("", 0), Times.Exactly(3));
            instanceMock.Verify(a => a.ParameterlessMethod(), Times.Once());
        }

        [Test]
        public void TestExceptionSupression()
        {
            var instanceMock = new Mock<IAwesomeInterface>();
            var cacheProviderMock = new Mock<ICacheProvider<IAwesomeInterface>>();
            var sleipner = new SleipnerProxy<IAwesomeInterface>(instanceMock.Object, cacheProviderMock.Object);
            sleipner.Configure(a =>
                                   {
                                       a.For(b => b.ParameterlessMethod()).CacheFor(10);
                                   });
            
            var methodInfo = typeof(IAwesomeInterface).GetMethod("ParameterlessMethod");
            var cachePolicy = sleipner.CachePolicyProvider.GetPolicy(methodInfo);
            var parameters = new object[0];
            IEnumerable<string> result = new[] { "", "" };
            var exception = new Exception();

            cacheProviderMock.Setup(a => a.GetItem<IEnumerable<string>>(methodInfo, cachePolicy, parameters)).Returns(new CachedObject<IEnumerable<string>>(CachedObjectState.Stale, result));
            instanceMock.Setup(a => a.ParameterlessMethod()).Throws(exception);

            sleipner.Object.ParameterlessMethod();

            Thread.Sleep(1000);

            instanceMock.Verify(a => a.ParameterlessMethod(), Times.Once());
            cacheProviderMock.Verify(a => a.GetItem<IEnumerable<string>>(methodInfo, cachePolicy, parameters), Times.Once());
            cacheProviderMock.Verify(a => a.StoreItem(methodInfo, cachePolicy, result, parameters), Times.Once());
            cacheProviderMock.Verify(a => a.StoreException<IEnumerable<string>>(methodInfo, cachePolicy, exception, parameters), Times.Never());
        }

        [Test]
        public void TestExceptionBubble()
        {
            var instanceMock = new Mock<IAwesomeInterface>();
            var cacheProviderMock = new Mock<ICacheProvider<IAwesomeInterface>>();
            var sleipner = new SleipnerProxy<IAwesomeInterface>(instanceMock.Object, cacheProviderMock.Object);
            sleipner.Configure(a =>
            {
                a.For(b => b.ParameterlessMethod()).CacheFor(10).BubbleExceptionsWhenStale(true);
            });

            var methodInfo = typeof(IAwesomeInterface).GetMethod("ParameterlessMethod");
            var cachePolicy = sleipner.CachePolicyProvider.GetPolicy(methodInfo);
            var parameters = new object[0];
            IEnumerable<string> result = new[] { "", "" };
            var exception = new AwesomeException();

            cacheProviderMock.Setup(a => a.GetItem<IEnumerable<string>>(methodInfo, cachePolicy, parameters)).Returns(new CachedObject<IEnumerable<string>>(CachedObjectState.Stale, result));
            instanceMock.Setup(a => a.ParameterlessMethod()).Throws(exception);

            sleipner.Object.ParameterlessMethod();

            Thread.Sleep(1000);

            instanceMock.Verify(a => a.ParameterlessMethod(), Times.Once());
            cacheProviderMock.Verify(a => a.GetItem<IEnumerable<string>>(methodInfo, cachePolicy, parameters), Times.Once());
            cacheProviderMock.Verify(a => a.StoreItem(methodInfo, cachePolicy, result, parameters), Times.Never());
            cacheProviderMock.Verify(a => a.StoreException <IEnumerable<string>>(methodInfo, cachePolicy, exception, parameters), Times.Once());
        }
    }
}