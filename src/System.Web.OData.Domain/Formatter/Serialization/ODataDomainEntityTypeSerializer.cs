﻿using System.Web.OData.Domain.Results;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;

namespace System.Web.OData.Domain.Formatter.Serialization
{
    public class ODataDomainEntityTypeSerializer : ODataEntityTypeSerializer
    {
        public ODataDomainEntityTypeSerializer(ODataSerializerProvider provider)
            : base(provider)
        {
        }

        public override void WriteObject(object graph, Type type, ODataMessageWriter messageWriter, ODataSerializerContext writeContext)
        {
            EntityResult entityResult = graph as EntityResult;
            if (entityResult != null)
            {
                graph = entityResult.Result;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }

        public override string CreateETag(EntityInstanceContext entityInstanceContext)
        {
            string etag = null;
            object etagGetterObject;
            if (entityInstanceContext.Request.Properties.TryGetValue("ETagGetter", out etagGetterObject))
            {
                Func<object, string> etagGetter = etagGetterObject as Func<object, string>;
                if (etagGetter != null)
                {
                    etag = etagGetter(entityInstanceContext.EntityInstance);
                }
            }

            if (etag == null)
            {
                etag = base.CreateETag(entityInstanceContext);
            }

            return etag;
        }
    }
}
