﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Couchbase.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Server.Serialization;
using Couchbase.Core;
using NUnit.Framework;
using Couchbase.Core.Buckets;
using Couchbase.Core.Services;
using Couchbase.Core.Transcoders;
using Couchbase.IO;
using Couchbase.IO.Operations;
using Couchbase.UnitTests.IO.Operations.Subdocument;
using Couchbase.Utils;
using Couchbase.Views;
using Moq;

namespace Couchbase.UnitTests.Core.Buckets
{
    [TestFixture]
    public class CouchbaseRequestExecutorTests
    {
        [Test]
        public void WhenForwardMapIsAvailable_AndRevisionIsSame_OperationUsesForwardMapVBucket()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration());

            var server1 = new Mock<IServer>();
            server1.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic>());
            server1.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var server2 = new Mock<IServer>();
            server2.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic>());
            server2.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Parse("255.255.0.0"), 8091));

            var vBucketServerMap = new VBucketServerMap
            {
                ServerList = new[]
                {
                    "localhost:8901",
                    "255.255.0.0:8091"
                },
                VBucketMap = new[] {new[] {0}},
                VBucketMapForward = new[] {new[] {1}}
            };
            var keyMapper = new VBucketKeyMapper(new Dictionary<IPAddress, IServer>
            {
                { IPAddress.Loopback, server1.Object},
                { IPAddress.Parse("255.255.0.0"), server2.Object}
            }, vBucketServerMap, 2, "default");

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsDataCapable).Returns(true);
            configInfo.Setup(x => x.GetKeyMapper()).Returns(keyMapper);
            configInfo.Setup(x => x.ClientConfig).Returns(new ClientConfiguration());
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            var op = new Get<dynamic>("thekey", null, new DefaultTranscoder(), 100);
            op.LastConfigRevisionTried = 2;
            var result = executor.SendWithRetry(op);
            Assert.AreEqual(op.VBucket.LocatePrimary().EndPoint, keyMapper.GetVBucketsForwards().First().Value.LocatePrimary().EndPoint);
        }

        [Test]
        public void WhenForwardMapIsAvailable_AndRevisionIsZero_OperationUsesVBucket()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration());

            var server1 = new Mock<IServer>();
            server1.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic>());
            server1.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var server2 = new Mock<IServer>();
            server2.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic>());
            server2.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Parse("255.255.0.0"), 8091));

            var vBucketServerMap = new VBucketServerMap
            {
                ServerList = new[]
                {
                    "localhost:8901",
                    "255.255.0.0:8091"
                },
                VBucketMap = new[] { new[] { 0 } },
                VBucketMapForward = new[] { new[] { 1 } }
            };
            var keyMapper = new VBucketKeyMapper(new Dictionary<IPAddress, IServer>
            {
                { IPAddress.Loopback, server1.Object},
                { IPAddress.Parse("255.255.0.0"), server2.Object}
            }, vBucketServerMap, 1, "default");

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsDataCapable).Returns(true);
            configInfo.Setup(x => x.GetKeyMapper()).Returns(keyMapper);
            configInfo.Setup(x => x.ClientConfig).Returns(new ClientConfiguration());
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            var op = new Get<dynamic>("thekey", null, new DefaultTranscoder(), 100);
            op.LastConfigRevisionTried = 0;
            var result = executor.SendWithRetry(op);
            Assert.AreEqual(op.VBucket.LocatePrimary().EndPoint, keyMapper.GetVBuckets().First().Value.LocatePrimary().EndPoint);
        }

        [Test]
        public void WhenForwardMapIsAvailable_AndRevisionIsGreater_OperationUsesVBucket()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration());

            var server1 = new Mock<IServer>();
            server1.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic>());
            server1.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var server2 = new Mock<IServer>();
            server2.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic>());
            server2.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Parse("255.255.0.0"), 8091));

            var vBucketServerMap = new VBucketServerMap
            {
                ServerList = new[]
                {
                    "localhost:8901",
                    "255.255.0.0:8091"
                },
                VBucketMap = new[] { new[] { 0 } },
                VBucketMapForward = new[] { new[] { 1 } }
            };
            var keyMapper = new VBucketKeyMapper(new Dictionary<IPAddress, IServer>
            {
                { IPAddress.Loopback, server1.Object},
                { IPAddress.Parse("255.255.0.0"), server2.Object}
            }, vBucketServerMap, 3, "default");

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsDataCapable).Returns(true);
            configInfo.Setup(x => x.GetKeyMapper()).Returns(keyMapper);
            configInfo.Setup(x => x.ClientConfig).Returns(new ClientConfiguration());
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            var op = new Get<dynamic>("thekey", null, new DefaultTranscoder(), 100);
            op.LastConfigRevisionTried = 2;
            var result = executor.SendWithRetry(op);
            Assert.AreEqual(op.VBucket.LocatePrimary().EndPoint, keyMapper.GetVBuckets().First().Value.LocatePrimary().EndPoint);
        }

        [Test]
        public void ReadFromReplica_WhenKeyNotFound_ReturnsKeyNotFound()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration());

            var server1 = new Mock<IServer>();
            server1.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic> { Status = ResponseStatus.KeyNotFound });
            server1.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var server2 = new Mock<IServer>();
            server2.Setup(x => x.Send(It.IsAny<IOperation<dynamic>>())).Returns(new OperationResult<dynamic> {Status = ResponseStatus.KeyNotFound});
            server2.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Parse("255.255.0.0"), 8091));

            var vBucketServerMap = new VBucketServerMap
            {
                ServerList = new[]
                {
                    "localhost:8901",
                    "255.255.0.0:8091"
                },
                VBucketMap = new[] { new[] { 0, 1 } },
                VBucketMapForward = new[] { new[] { 1 } }
            };
            var keyMapper = new VBucketKeyMapper(new Dictionary<IPAddress, IServer>
            {
                { IPAddress.Loopback, server1.Object},
                { IPAddress.Parse("255.255.0.0"), server2.Object}
            }, vBucketServerMap, 3, "default");

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsDataCapable).Returns(true);
            configInfo.Setup(x => x.GetKeyMapper()).Returns(keyMapper);
            configInfo.Setup(x => x.ClientConfig).Returns(new ClientConfiguration());
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            var op = new ReplicaRead<dynamic>("thekey", null, new DefaultTranscoder(), 100);
            var result = executor.ReadFromReplica(op);
            Assert.AreEqual(ResponseStatus.KeyNotFound, result.Status);
        }

        [Test]
        public void When_DataService_Is_Not_Available_SendWithRetry_Throws_ServiceNotSupportedException()
        {
            var mockController = new Mock<IClusterController>();
            mockController.Setup(x => x.Configuration).Returns(new ClientConfiguration());
            var mockConfig = new Mock<IConfigInfo>();
            mockConfig.Setup(x => x.IsDataCapable).Returns(false);
            mockConfig.Setup(x => x.ClientConfig).Returns(new ClientConfiguration());
            var pending = new ConcurrentDictionary<uint, IOperation>();

            var executor = new CouchbaseRequestExecuter(mockController.Object, mockConfig.Object, "default", pending);

            var result = executor.SendWithRetry(new FakeSubDocumentOperation<dynamic>(null, "key", null, new DefaultTranscoder(), 0));

            Assert.IsFalse(result.Success);
            Assert.IsInstanceOf<ServiceNotSupportedException>(result.Exception);
            Assert.AreEqual(string.Format(ExceptionUtil.ServiceNotSupportedMsg, "Data"), result.Exception.Message);
        }

        [Test]
        public void When_DataService_Is_Not_Available_SendWithReteyAsync_Throws_ServiceNotSupportedException()
        {
            var mockController = new Mock<IClusterController>();
            mockController.Setup(x => x.Configuration).Returns(new ClientConfiguration());
            var mockConfig = new Mock<IConfigInfo>();
            mockConfig.Setup(x => x.IsDataCapable).Returns(false);
            mockConfig.Setup(x => x.ClientConfig).Returns(new ClientConfiguration());
            var pending = new ConcurrentDictionary<uint, IOperation>();

            var executor = new CouchbaseRequestExecuter(mockController.Object, mockConfig.Object, "default", pending);

            Assert.ThrowsAsync<ServiceNotSupportedException>(
                async () =>
                    await executor.SendWithRetryAsync(new FakeSubDocumentOperation<dynamic>(null, "key", null,
                        new DefaultTranscoder(), 0)),
                ExceptionUtil.ServiceNotSupportedMsg, "Data");
        }

        public async Task MaxViewRetries_IsOne_RequestIsNotRetried_Async()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration { MaxViewRetries = 0 });

            var server = new Mock<IServer>();
            server.Setup(x => x.SendAsync<dynamic>(It.IsAny<IViewQueryable>())).Returns(
                Task.FromResult<IViewResult<dynamic>>( new ViewResult<dynamic>
                {
                    StatusCode = HttpStatusCode.RequestTimeout,
                    Success = false
                }));
            server.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsViewCapable).Returns(true);
            configInfo.Setup(x => x.GetViewNode()).Returns(server.Object);
            configInfo.Setup(x => x.ClientConfig).Returns(controller.Object.Configuration);
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            //arrange
            var query = new ViewQuery().
                From("beer", "brewery_beers").
                Bucket("beer-sample");

            var result = await executor.SendWithRetryAsync<dynamic>(query);
            Assert.AreEqual(0, query.RetryAttempts);
            Assert.AreEqual(false, result.CannotRetry());
        }

        [Test]
        public void MaxViewRetries_IsOne_RequestIsNotRetried()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration {MaxViewRetries = 0});

            var server = new Mock<IServer>();
            server.Setup(x => x.Send<dynamic>(It.IsAny<IViewQueryable>())).Returns(
                new ViewResult<dynamic>
            {
                StatusCode = HttpStatusCode.RequestTimeout, Success = false
            });
            server.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsViewCapable).Returns(true);
            configInfo.Setup(x => x.GetViewNode()).Returns(server.Object);
            configInfo.Setup(x => x.ClientConfig).Returns(controller.Object.Configuration);
            configInfo.Setup(x => x.BucketConfig.BucketType).Returns("couchbase");
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            //arrange
            var query = new ViewQuery().
                From("beer", "brewery_beers").
                Bucket("beer-sample");

            var result = executor.SendWithRetry<dynamic>(query);
            Assert.AreEqual(0, query.RetryAttempts);
            Assert.AreEqual(false, result.CannotRetry());
        }

        [Test]
        public void MaxViewRetries_IsThree_RequestIsRetriedThreeTimes()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration { MaxViewRetries = 3 });

            var server = new Mock<IServer>();
            server.Setup(x => x.Send<dynamic>(It.IsAny<IViewQueryable>())).Returns(
                new ViewResult<dynamic>
                {
                    StatusCode = HttpStatusCode.RequestTimeout,
                    Success = false
                });
            server.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsViewCapable).Returns(true);
            configInfo.Setup(x => x.GetViewNode()).Returns(server.Object);
            configInfo.Setup(x => x.ClientConfig).Returns(controller.Object.Configuration);
            configInfo.Setup(x => x.BucketConfig.BucketType).Returns("couchbase");
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            //arrange
            var query = new ViewQuery().
                From("beer", "brewery_beers").
                Bucket("beer-sample");

            var result = executor.SendWithRetry<dynamic>(query);
            Assert.AreEqual(3, query.RetryAttempts);
            Assert.AreEqual(false, result.CannotRetry());
        }

        [Test]
        public async Task MaxViewRetries_IsThree_RequestIsRetriedThreeTimes_Async()
        {
            var controller = new Mock<IClusterController>();
            controller.Setup(x => x.Configuration).Returns(new ClientConfiguration { MaxViewRetries = 3 });

            var server = new Mock<IServer>();
            server.Setup(x => x.SendAsync<dynamic>(It.IsAny<IViewQueryable>())).Returns(
                Task.FromResult<IViewResult<dynamic>>(new ViewResult<dynamic>
                {
                    StatusCode = HttpStatusCode.RequestTimeout,
                    Success = false
                }));
            server.Setup(x => x.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 8091));

            var configInfo = new Mock<IConfigInfo>();
            configInfo.Setup(x => x.IsViewCapable).Returns(true);
            configInfo.Setup(x => x.GetViewNode()).Returns(server.Object);
            configInfo.Setup(x => x.ClientConfig).Returns(controller.Object.Configuration);
            var pending = new ConcurrentDictionary<uint, IOperation>();
            var executor = new CouchbaseRequestExecuter(controller.Object, configInfo.Object, "default", pending);

            //arrange
            var query = new ViewQuery().
                From("beer", "brewery_beers").
                Bucket("beer-sample");

            var result = await executor.SendWithRetryAsync<dynamic>(query);
            Assert.AreEqual(3, query.RetryAttempts);
            Assert.AreEqual(false, result.CannotRetry());
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2015 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
