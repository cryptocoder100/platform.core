#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning disable SA1402 // SA1402FileMayOnlyContainASingleType
namespace Exos.Platform.Persistence.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Future Events.
    /// </summary>
    public class FutureEvent
    {
        /// <summary>
        /// Gets or sets the Event.
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// Gets or sets the EntityName.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the DueDateReference.
        /// "DueDateReference": "CreatedDate", this should be in UTC.
        /// </summary>
        public string DueDateReference { get; set; }

        /// <summary>
        /// Gets or sets the DueDateExpression.
        /// "DueDateExpression": "+24".
        /// </summary>
        public string DueDateExpression { get; set; }

        /// <summary>
        /// Gets or sets the DueDateExpressionUnits.
        /// "DueDateExpressionUnits": "hours",days,hours,minutes,seconds.
        /// </summary>
        public string DueDateExpressionUnits { get; set; }

        /// <summary>
        /// Gets or sets the DueDateDataExpressionType.
        /// "DueDateDataExpressionType": "inmemory", this can be inmemory or sql query.
        /// </summary>
        public string DueDateDataExpressionType { get; set; }
    }

    /// <summary>
    /// Cancel Event.
    /// </summary>
    public class CancelEvent
    {
        /// <summary>
        /// Gets or sets event to Cancel.
        /// </summary>
        public string Event { get; set; }
    }

    /// <summary>
    /// Event Configuration.
    /// </summary>
    public class EventConfig
    {
        /// <summary>
        /// Gets or sets the Event.
        /// </summary>
        public string SrcEvent { get; set; }

        /// <summary>
        /// Gets or sets the List of Future Events to Generate.
        /// </summary>
        public List<FutureEvent> GenerateEventsList { get; set; }

        /// <summary>
        /// Gets or sets the List of Future Events to Cancel.
        /// </summary>
        public List<CancelEvent> CancelEventsList { get; set; }
    }

    /// <summary>
    /// Configuration of Future Events.
    /// </summary>
    public class FutureEventsConfig
    {
        /// <summary>
        /// Gets or sets List of Events.
        /// </summary>
        public List<EventConfig> EventsConfig { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning restore SA1402 // SA1402FileMayOnlyContainASingleType