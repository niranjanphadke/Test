﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// An <see cref="Attribute"/> indicating the associated action is a Bitbucket WebHook endpoint.
    /// Specifies the optional <see cref="WebHookAttribute.Id"/>. Also adds a
    /// <see cref="Filters.WebHookReceiverExistsFilter"/> for the action.
    /// </summary>
    public class BitbucketWebHookAttribute : WebHookAttribute, IWebHookEventSelectorMetadata
    {
        private string _eventName;

        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="BitbucketWebHookAttribute"/> indicating the associated action is a Bitbucket
        /// WebHook endpoint.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName(string id, string @event, TData data)
        /// </code>
        /// or include the subset of parameters required. <c>TData</c> must be compatible with expected requests e.g.
        /// <see cref="Newtonsoft.Json.Linq.JObject"/>.
        /// </para>
        /// <para>This constructor should usually be used at most once in a WebHook application.</para>
        /// <para>The default route <see cref="Mvc.Routing.IRouteTemplateProvider.Name"/> is <c>null</c>.</para>
        /// </summary>
        public BitbucketWebHookAttribute()
            : base(BitbucketConstants.ReceiverName)
        {
        }

        /// <summary>
        /// Gets or sets the name of the event the associated controller action accepts.
        /// </summary>
        /// <value>Default value is <c>null</c>, indicating this action accepts all events.</value>
        public string EventName
        {
            get
            {
                return _eventName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.General_ArgumentCannotBeNullOrEmpty, nameof(value));
                }

                _eventName = value;
            }
        }
    }
}