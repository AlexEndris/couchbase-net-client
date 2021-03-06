﻿using System.Linq;
using Couchbase.Core;
using Couchbase.Core.IO.SubDocument;
using Couchbase.Core.Serialization;
using Couchbase.IO.Operations;
using Moq;
using NUnit.Framework;

namespace Couchbase.UnitTests.Core
{
    [TestFixture]
    public class LookupBuilderTests
    {
        [Test]
        public void GetCommands_Enumerates_ExactlyThreeLookups()
        {
            var mockedInvoker = new Mock<ISubdocInvoker>();
            var builder = new LookupInBuilder<dynamic>(mockedInvoker.Object, () => new DefaultSerializer(), "mykey");

            var count = ((LookupInBuilder<dynamic>) builder.Get("boo.foo").Exists("foo.boo").Get("boo.foo")).Count();
            Assert.AreEqual(3, count);
        }

        [TestCase(SubdocLookupFlags.XattrPath, 4)]
        [TestCase(SubdocLookupFlags.AccessDeleted, 12)]
        [TestCase(SubdocLookupFlags.XattrPath | SubdocLookupFlags.AccessDeleted, 12)]
        public void Get_For_Xattr_Sets_Correct_Flag(SubdocLookupFlags flags, byte expected)
        {
            var mockResult = new Mock<IDocumentFragment<dynamic>>();

            var mockedInvoker = new Mock<ISubdocInvoker>();
            mockedInvoker.Setup(x => x.Invoke(It.IsAny<LookupInBuilder<dynamic>>()))
                .Returns(mockResult.Object);

            var lookupBuilder = new LookupInBuilder<dynamic>(mockedInvoker.Object, () => new DefaultSerializer(), "mykey");

            var result = lookupBuilder.Get("path", flags)
                .Execute();

            Assert.AreSame(mockResult.Object, result);
            mockedInvoker.Verify(
                invoker => invoker.Invoke(It.Is<LookupInBuilder<dynamic>>(
                    builder =>
                        builder.FirstSpec().OpCode == OperationCode.SubGet &&
                        builder.FirstSpec().Path == "path" &&
                        builder.FirstSpec().Flags == expected)
                ), Times.Once
            );
        }

        [TestCase(SubdocLookupFlags.XattrPath, 4)]
        [TestCase(SubdocLookupFlags.AccessDeleted, 12)]
        [TestCase(SubdocLookupFlags.XattrPath | SubdocLookupFlags.AccessDeleted, 12)]
        public void Exists_For_Xattr_Sets_Correct_Flag(SubdocLookupFlags flags, byte expected)
        {
            var mockResult = new Mock<IDocumentFragment<dynamic>>();

            var mockedInvoker = new Mock<ISubdocInvoker>();
            mockedInvoker.Setup(x => x.Invoke(It.IsAny<LookupInBuilder<dynamic>>()))
                .Returns(mockResult.Object);

            var lookupBuilder = new LookupInBuilder<dynamic>(mockedInvoker.Object, () => new DefaultSerializer(), "mykey");

            var result = lookupBuilder.Exists("path", flags)
                .Execute();

            Assert.AreSame(mockResult.Object, result);
            mockedInvoker.Verify(
                invoker => invoker.Invoke(It.Is<LookupInBuilder<dynamic>>(
                    builder =>
                        builder.FirstSpec().OpCode == OperationCode.SubExist &&
                        builder.FirstSpec().Path == "path" &&
                        builder.FirstSpec().Flags == expected)
                ), Times.Once
            );
        }
    }
}
