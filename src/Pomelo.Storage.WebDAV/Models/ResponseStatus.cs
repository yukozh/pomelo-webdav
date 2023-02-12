// Copyright (c) Yuko(Yisheng) Zheng. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;

namespace Pomelo.Storage.WebDAV.Models
{
    public class ResponseStatus
    {
        public ResponseStatus() { }

        public ResponseStatus(string statusText)
        {
            this.Protocol = statusText.Substring(0, statusText.IndexOf(" "));
            statusText = statusText.Substring(statusText.IndexOf(" ") + 1);
            this.StatusCode = Convert.ToInt32(statusText.Substring(0, statusText.IndexOf(" ")));
            statusText = statusText.Substring(statusText.IndexOf(" ") + 1);
            this.ReasonPhase = statusText;
        }

        public int StatusCode { get; set; }

        public string Protocol { get; set; }

        public string ReasonPhase { get; set; }
    }
}
