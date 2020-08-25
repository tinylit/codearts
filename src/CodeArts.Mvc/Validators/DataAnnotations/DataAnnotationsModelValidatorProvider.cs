#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CodeArts.Mvc.Validators.DataAnnotations
{
    /// <summary>
    /// An implementation of <see cref="IModelValidatorProvider"/> which provides validators
    /// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
    /// a validator for types which implement <see cref="IValidatableObject"/>.
    /// </summary>
    public sealed class DataAnnotationsModelValidatorProvider : IModelValidatorProvider
    {
        /// <summary>
        /// 创建验证器。
        /// </summary>
        /// <param name="context"></param>
        public void CreateValidators(ModelValidatorProviderContext context)
        {
            context.Results?.ForEach(validatorItem =>
            {
                if (validatorItem.Validator is null || !(validatorItem.ValidatorMetadata is ValidationAttribute attribute))
                {
                    return;
                }

                validatorItem.Validator = new DataAnnotationsModelValidator(validatorItem.Validator, attribute);
            });
        }
    }
}
#endif