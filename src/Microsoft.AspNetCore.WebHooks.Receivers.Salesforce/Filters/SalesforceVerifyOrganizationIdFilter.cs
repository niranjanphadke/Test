// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    // TODO: Rename WebHookVerifySignatureFilter since this subclass is not checking a signature. VerifyBody?
    //
    // TODO: Create base version of this filter (likely as an IActionFilter) for use in e.g. Azure Alert, Dynamics CRM,
    // TODO: Kudu, MailChimp, and Pusher receivers. Filter would require event name in request body and map that event
    // TODO: name to route data. This would support model binding but not event-based action selection.
    /// <summary>
    /// An <see cref="IAsyncResourceFilter"/> that verifies the Salesforce SOAP request body. Confirms the body
    /// deserializes as <see cref="XElement"/> that can be converted to <see cref="SalesforceNotifications"/>. Then
    /// confirms the organization id and event name is present and that the organization id matches the configured
    /// secret key.
    /// </summary>
    public class SalesforceVerifyOrganizationIdFilter : WebHookVerifySignatureFilter, IAsyncResourceFilter
    {
        // Serialize ModelState errors, especially top-level input formatter issues, similarly to
        // CreateErrorResult(..., message, ...).
        private static readonly string ModelStateRootKey = WebHookErrorKeys.MessageKey;

        private readonly IModelBinder _bodyModelBinder;
        private readonly ISalesforceResultCreator _resultCreator;
        private readonly ModelMetadata _xElementMetadata;

        /// <summary>
        /// Instantiates a new <see cref="SalesforceVerifyOrganizationIdFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="WebHookSecurityFilter.Logger"/>.
        /// </param>
        /// /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="optionsAccessor">
        /// The <see cref="IOptions{MvcOptions}"/> accessor for <see cref="MvcOptions"/>.
        /// </param>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        /// <param name="receiverConfig">
        /// The <see cref="IWebHookReceiverConfig"/> used to initialize
        /// <see cref="WebHookSecurityFilter.Configuration"/> and <see cref="WebHookSecurityFilter.ReceiverConfig"/>.
        /// </param>
        /// <param name="resultCreator"></param>
        public SalesforceVerifyOrganizationIdFilter(
            ILoggerFactory loggerFactory,
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> optionsAccessor,
            IHttpRequestStreamReaderFactory readerFactory,
            IWebHookReceiverConfig receiverConfig,
            ISalesforceResultCreator resultCreator)
            : base(loggerFactory, receiverConfig)
        {
            var options = optionsAccessor.Value;
            _bodyModelBinder = new BodyModelBinder(options.InputFormatters, readerFactory, loggerFactory, options);
            _resultCreator = resultCreator;
            _xElementMetadata = metadataProvider.GetMetadataForType(typeof(XElement));
        }

        /// <inheritdoc />
        public override string ReceiverName => SalesforceConstants.ReceiverName;

        /// <inheritdoc />
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var modelState = context.ModelState;
            var actionContext = new ActionContext(
                context.HttpContext,
                context.RouteData,
                context.ActionDescriptor,
                modelState);

            var data = await ReadAsXmlAsync(actionContext, context.ValueProviderFactories);
            if (data == null)
            {
                // ReadAsXmlAsync returns null when model state is valid only when other filters will log and return
                // errors about the same conditions.
                if (!modelState.IsValid)
                {
                   context.Result = WebHookResultUtilities.CreateErrorResult(modelState);
                }

                return;
            }

            // Got a valid XML body. From this point on, all responses should contain XML.
            var notifications = new SalesforceNotifications(data);

            // Ensure that the organization ID exists and matches the expected value.
            var organizationId = GetShortOrganizationId(notifications.OrganizationId);
            if (string.IsNullOrEmpty(organizationId))
            {
                Logger.LogError(
                    0,
                    "The HTTP request body did not contain a required '{PropertyName}' property.",
                    nameof(notifications.OrganizationId));

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifyOrganization_MissingValue,
                    nameof(notifications.OrganizationId));
                context.Result = await _resultCreator.GetFailedResultAsync(message);

                return;
            }

            var request = context.HttpContext.Request;
            var routeData = context.RouteData;
            var secret = await GetReceiverConfig(request, routeData, SalesforceConstants.ConfigurationName, 15, 18);
            var secretKey = GetShortOrganizationId(secret);
            if (!SecretEqual(organizationId, secretKey))
            {
                Logger.LogError(
                    1,
                    "The '{PropertyName}' value provided in the HTTP request body did not match the expected value.",
                    nameof(notifications.OrganizationId));

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifyOrganization_BadValue,
                    nameof(notifications.OrganizationId));
                context.Result = await _resultCreator.GetFailedResultAsync(message);

                return;
            }

            // Get the event name.
            var eventName = notifications.ActionId;
            if (string.IsNullOrEmpty(eventName))
            {
                Logger.LogError(
                    2,
                    "The HTTP request body did not contain a required '{PropertyName}' property.",
                    nameof(notifications.ActionId));

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifyOrganization_MissingValue,
                    nameof(notifications.ActionId));
                context.Result = await _resultCreator.GetFailedResultAsync(message);

                return;
            }

            // Success. Provide request data and event name for model binding.
            routeData.Values[WebHookConstants.EventKeyName] = eventName;

            // TODO: Add a model binder that maps from these Items. Binding SalesforceNotifications parameters will
            // TODO: currently fail and binding XElement parameters will duplicate work already done here.
            context.HttpContext.Items[typeof(XElement)] = data;
            context.HttpContext.Items[typeof(SalesforceNotifications)] = notifications;
        }

        /// <summary>
        /// Gets the shortened version of <paramref name="fullOrganizationId"/>.
        /// </summary>
        /// <param name="fullOrganizationId">The full organization name.</param>
        /// <returns></returns>
        protected static string GetShortOrganizationId(string fullOrganizationId)
        {
            if (fullOrganizationId?.Length == 18)
            {
                return fullOrganizationId.Substring(0, 15);
            }

            return fullOrganizationId;
        }

        /// <summary>
        /// Reads the XML HTTP request entity body.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="valueProviderFactories">The list of <see cref="IValueProviderFactory"/> instances.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="XElement"/> containing the HTTP request
        /// entity body.
        /// </returns>
        protected virtual async Task<XElement> ReadAsXmlAsync(
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            var request = actionContext.HttpContext.Request;
            if (request.Body == null ||
                !request.ContentLength.HasValue ||
                request.ContentLength.Value == 0L ||
                !HttpMethods.IsPost(request.Method) ||
                !request.IsXml())
            {
                // Other filters will log and return errors about these conditions.
                return null;
            }

            var valueProvider = await CompositeValueProvider.CreateAsync(actionContext, valueProviderFactories);
            var bindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                _xElementMetadata,
                bindingInfo: null,
                modelName: ModelStateRootKey);

            // Read request body.
            await _bodyModelBinder.BindModelAsync(bindingContext);
            if (!bindingContext.ModelState.IsValid)
            {
                return null;
            }

            if (!bindingContext.Result.IsModelSet)
            {
                throw new InvalidOperationException(Resources.VerifyOrganization_ModelBindingFailed);
            }

            // Success
            return (XElement)bindingContext.Result.Model;
        }
    }
}
