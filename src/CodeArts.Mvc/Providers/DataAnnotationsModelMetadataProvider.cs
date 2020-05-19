#if NET40 || NET45 || NET451 || NET452 || NET461
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Http.Metadata.Providers;

namespace CodeArts.Mvc.Providers
{

    internal class DataAnnotationsModelMetadataProvider : AssociatedMetadataProvider<CachedDataAnnotationsModelMetadata>
    {
        private readonly System.Web.Http.Metadata.Providers.DataAnnotationsModelMetadataProvider provider;

        public DataAnnotationsModelMetadataProvider(System.Web.Http.Metadata.Providers.DataAnnotationsModelMetadataProvider provider) : base()
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        protected override CachedDataAnnotationsModelMetadata CreateMetadataFromPrototype(CachedDataAnnotationsModelMetadata prototype, Func<object> modelAccessor)
        {
            return new CachedDataAnnotationsModelMetadata(prototype, modelAccessor);
        }

        protected override CachedDataAnnotationsModelMetadata CreateMetadataPrototype(IEnumerable<Attribute> attributes, Type containerType, Type modelType, string propertyName)
        {
            return new CachedDataAnnotationsModelMetadata(provider, containerType, modelType, propertyName, new CachedDataAnnotationsMetadataAttributes(attributes));
        }
    }
}
#endif