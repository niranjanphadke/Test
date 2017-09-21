// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Well-known names used in Dropbox receivers and handlers.
    /// </summary>
    public static class DropboxConstants
    {
        /// <summary>
        /// Gets the only supported event name for this receiver. This value may be model bound but cannot be used in
        /// action selection.
        /// </summary>
        public static string EventName => "change";

        /// <summary>
        /// Gets the name of the Dropbox WebHook receiver.
        /// </summary>
        public static string ReceiverName => "dropbox";
    }
}
