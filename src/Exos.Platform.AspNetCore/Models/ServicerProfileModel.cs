#pragma warning disable SA1402 // FileMayOnlyContainASingleType
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName

namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Base Resource Model.
    /// </summary>
    public class BaseResourceModel
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ResourceType.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the ResourceName.
        /// </summary>
        public string ResourceName { get; set; }
    }

    /// <summary>
    /// Implement a Feature.
    /// </summary>
    public class Feature
    {
        /// <summary>
        /// Gets or sets the FeatureId.
        /// </summary>
        public string FeatureId { get; set; }

        /// <summary>
        /// Gets or sets a List of BaseResourceModel.
        /// </summary>
        public List<BaseResourceModel> Resources { get; set; }
    }

    /// <summary>
    /// This is to pull the service features list and push them in user context.
    /// </summary>
    public class SubscribedFeature
    {
        /// <summary>
        /// Gets or sets Feature Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Feature.
        /// </summary>
        public Feature Feature { get; set; }
    }

    /// <summary>
    /// Servicer Profile.
    /// </summary>
    public class ServicerProfile
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ServicerId.
        /// </summary>
        public long ServicerId { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a list of SubscribedFeature.
        /// </summary>
        public List<SubscribedFeature> SubscribedFeatures { get; set; }
    }
}
#pragma warning restore SA1402 // FileMayOnlyContainASingleType
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName