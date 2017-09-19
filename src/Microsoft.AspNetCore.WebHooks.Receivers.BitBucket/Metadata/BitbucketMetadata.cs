using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    public class BitbucketMetadata :
        WebHookMetadata,
        IWebHookEventMetadata,
        IWebHookRequestMetadataService,
        IWebHookSecurityMetadata
    {
        public BitbucketMetadata()
            : base(BitbucketConstants.ReceiverName)
        {
        }

        public string ConstantValue => null;

        public string HeaderName => BitbucketConstants.EventHeaderName;

        public string PingEventName => null;

        public string QueryParameterKey => null;

        public WebHookBodyType BodyType => WebHookBodyType.Json;

        public bool VerifyCodeParameter => true;
    }
}
