﻿using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a change set entry validator.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </remarks>
    public interface IChangeSetEntryValidator
    {
        /// <summary>
        /// Asynchronously validates a change set entry.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="entry">
        /// The change set entry to validate.
        /// </param>
        /// <param name="validationResults">
        /// A set of validation results.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task ValidateEntityAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            ValidationResults validationResults,
            CancellationToken cancellationToken);
    }
}
