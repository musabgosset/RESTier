﻿using System.Collections.Generic;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a change set.
    /// </summary>
    public class ChangeSet
    {
        private List<ChangeSetEntry> entries;

        /// <summary>
        /// Initializes a new change set.
        /// </summary>
        public ChangeSet()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new change set.
        /// </summary>
        /// <param name="entries">
        /// A set of change set entries.
        /// </param>
        public ChangeSet(IEnumerable<ChangeSetEntry> entries)
        {
            if (entries != null)
            {
                this.entries = new List<ChangeSetEntry>(entries);
            }
        }

        /// <summary>
        /// Gets the entries in this change set.
        /// </summary>
        public IList<ChangeSetEntry> Entries
        {
            get
            {
                if (this.entries == null)
                {
                    this.entries = new List<ChangeSetEntry>();
                }
                return this.entries;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an Entity has been added, modified, or deleted.
        /// </summary>
        // TODO: make the ChangeSet 'dynamic' so it gets added to as things change during the flow
        public bool AnEntityHasChanged { get; set; }
    }
}
