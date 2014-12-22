﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Batch;
using System.Web.OData.Batch;
using Microsoft.OData.Core;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.WebApi.Batch
{
    public class ODataDomainBatchHandler : DefaultODataBatchHandler
    {
        public ODataDomainBatchHandler(HttpServer httpServer, Func<DomainContext> contextFactory = null)
            : base(httpServer)
        {
            this.ContextFactory = contextFactory;
        }

        public Func<DomainContext> ContextFactory { get; set; }

        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.ContextFactory == null)
            {
                throw new InvalidOperationException("ODataDomainBatchHandler was called without a DomainContext Factory.");
            }

            Ensure.NotNull(request, "request");

            ODataMessageReaderSettings oDataReaderSettings = new ODataMessageReaderSettings
            {
                DisableMessageStreamDisposal = true,
                MessageQuotas = MessageQuotas,
                BaseUri = GetBaseUri(request)
            };

            ODataMessageReader reader = await request.Content.GetODataMessageReaderAsync(oDataReaderSettings, cancellationToken);
            request.RegisterForDispose(reader);

            List<ODataBatchRequestItem> requests = new List<ODataBatchRequestItem>();
            ODataBatchReader batchReader = reader.CreateODataBatchReader();
            Guid batchId = Guid.NewGuid();
            while (batchReader.Read())
            {
                if (batchReader.State == ODataBatchReaderState.ChangesetStart)
                {
                    IList<HttpRequestMessage> changeSetRequests = await batchReader.ReadChangeSetRequestAsync(batchId, cancellationToken);
                    foreach (HttpRequestMessage changeSetRequest in changeSetRequests)
                    {
                        changeSetRequest.CopyBatchRequestProperties(request);
                    }
                    requests.Add(this.CreateChangeSetRequestItem(changeSetRequests));
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    HttpRequestMessage operationRequest = await batchReader.ReadOperationRequestAsync(batchId, bufferContentStream: true, cancellationToken: cancellationToken);
                    operationRequest.CopyBatchRequestProperties(request);
                    requests.Add(new OperationRequestItem(operationRequest));
                }
            }

            return requests;
        }

        protected virtual ChangeSetRequestItem CreateChangeSetRequestItem(IList<HttpRequestMessage> changeSetRequests)
        {
            return new ODataDomainChangeSetRequestItem(changeSetRequests, this.ContextFactory());
        }
    }
}
