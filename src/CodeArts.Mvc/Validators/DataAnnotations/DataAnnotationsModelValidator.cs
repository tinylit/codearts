#if NETCOREAPP2_0_OR_GREATER
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CodeArts.Mvc.Validators.DataAnnotations
{
    /// <summary>
    /// Validates based on the given <see cref="ValidationAttribute"/>.
    /// </summary>
    internal class DataAnnotationsModelValidator : IModelValidator
    {
        private readonly IModelValidator validator;
        private readonly ValidationAttribute validationAttribute;

        public DataAnnotationsModelValidator(IModelValidator validator, ValidationAttribute validationAttribute)
        {
            this.validator = validator;
            this.validationAttribute = validationAttribute;
        }

        /// <summary>
        /// Validates the context against the <see cref="ValidationAttribute"/>.
        /// </summary>
        /// <param name="validationContext">The context being validated.</param>
        /// <returns>An enumerable of the validation results.</returns>
        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext validationContext)
        {
            return ModelValidator.Validate(validationContext, validator, validationAttribute);
        }
    }
}
#endif