﻿using System;

namespace Microsoft.Data.Domain
{
    using Submit;

    /// <summary>
    /// Represents context under which a domain operates.
    /// </summary>
    /// <remarks>
    /// A domain context is an instantiation of a domain configuration. It
    /// maintains a set of properties that can be used to share instance
    /// data between hook points.
    /// </remarks>
    public class DomainContext : PropertyBag
    {
        /// <summary>
        /// Initializes a new domain context.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        public DomainContext(DomainConfiguration configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            if (!configuration.IsCommitted)
            {
                // TODO: error message
                throw new ArgumentException();
            }
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the domain configuration.
        /// </summary>
        public DomainConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets a value indicating if this domain
        /// context is current submitting changes.
        /// </summary>
        public bool IsSubmitting { get; internal set; }
    }
}
