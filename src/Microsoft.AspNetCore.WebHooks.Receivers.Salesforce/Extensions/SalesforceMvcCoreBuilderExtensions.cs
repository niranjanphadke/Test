// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    // TODO: Add IMvcBuilder variant of this class.
    /// <summary>
    /// Extension methods for setting up Salesforce WebHooks in an <see cref="IMvcCoreBuilder" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SalesforceMvcCoreBuilderExtensions
    {
        /// <summary>
        /// Add Salesforce WebHook configuration and services to the specified <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder" /> to configure.</param>
        public static IMvcCoreBuilder AddSalesforceWebHooks(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWebHookMetadata, SalesforceMetadata>());

            // ??? Are the [DataContract] formatters also needed? XmlSerializer is enough for at least XElement.
            return builder
                .AddXmlSerializerFormatters()
                .AddWebHooks();
        }
    }
}