// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Pomelo.Storage.WebDAV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pomelo.Storage.WebDAV.E2ETests
{
    public class ResponseStatusTests
    {
        [Fact]
        public void InitResponseStatusWithText200Test()
        {
            // Arrange
            var text = "HTTP/1.1 200 OK";

            // Act
            var responseStatus = new ResponseStatus(text);

            // Assert
            Assert.Equal("HTTP/1.1", responseStatus.Protocol);
            Assert.Equal(200, responseStatus.StatusCode);
            Assert.Equal("OK", responseStatus.ReasonPhase);
        }

        [Fact]
        public void InitResponseStatusWithText400Test()
        {
            // Arrange
            var text = "HTTP/1.1 400 Bad Request";

            // Act
            var responseStatus = new ResponseStatus(text);

            // Assert
            Assert.Equal("HTTP/1.1", responseStatus.Protocol);
            Assert.Equal(400, responseStatus.StatusCode);
            Assert.Equal("Bad Request", responseStatus.ReasonPhase);
        }
    }
}
