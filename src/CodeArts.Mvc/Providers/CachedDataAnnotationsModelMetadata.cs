#if NET40 || NET_NORMAL
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Http.Metadata.Providers;
using System.Web.Http.Validation;

namespace CodeArts.Mvc.Providers
{
    internal class CachedDataAnnotationsModelMetadata : CachedModelMetadata<CachedDataAnnotationsMetadataAttributes>
    {
        public CachedDataAnnotationsModelMetadata(CachedModelMetadata<CachedDataAnnotationsMetadataAttributes> prototype, Func<object> modelAccessor) : base(prototype, modelAccessor)
        {
        }

        public CachedDataAnnotationsModelMetadata(System.Web.Http.Metadata.Providers.DataAnnotationsModelMetadataProvider provider, Type containerType, Type modelType, string propertyName, CachedDataAnnotationsMetadataAttributes prototypeCache) : base(provider, containerType, modelType, propertyName, prototypeCache)
        {
        }

        public override IEnumerable<System.Web.Http.Validation.ModelValidator> GetValidators(IEnumerable<ModelValidatorProvider> validatorProviders)
        {
            foreach (var validator in base.GetValidators(validatorProviders))
            {
                if (validator is System.Web.Http.Validation.Validators.DataAnnotationsModelValidator modalValidator)
                {
                    yield return new DataAnnotationsModelValidator(modalValidator, validatorProviders);
                }
                else
                {
                    yield return validator;
                }
            }
        }
    }
}
#endif