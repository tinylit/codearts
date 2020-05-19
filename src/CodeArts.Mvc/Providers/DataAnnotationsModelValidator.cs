#if NET40 || NET45 || NET451 || NET452 || NET461
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web.Http.Metadata;
using System.Web.Http.Validation;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Mvc.Providers
{
    /// <summary>
    /// 数据注解。
    /// </summary>
    internal class DataAnnotationsModelValidator : System.Web.Http.Validation.ModelValidator
    {
        private readonly System.Web.Http.Validation.Validators.DataAnnotationsModelValidator validator;
        private static readonly Func<System.Web.Http.Validation.Validators.DataAnnotationsModelValidator, ValidationAttribute> attributeGetter;

        static DataAnnotationsModelValidator()
        {
            var modelValidatorType = typeof(System.Web.Http.Validation.Validators.DataAnnotationsModelValidator);

            var paramterExp = Parameter(modelValidatorType, "x");

            var bodyExp = Lambda<Func<System.Web.Http.Validation.Validators.DataAnnotationsModelValidator, ValidationAttribute>>(Property(paramterExp, modelValidatorType.GetProperty("Attribute", BindingFlags.Instance | BindingFlags.NonPublic)), paramterExp);

            attributeGetter = bodyExp.Compile();
        }

        public DataAnnotationsModelValidator(System.Web.Http.Validation.Validators.DataAnnotationsModelValidator validator, IEnumerable<ModelValidatorProvider> validatorProviders) : base(validatorProviders)
        {
            this.validator = validator;
        }

        public override IEnumerable<ModelValidationResult> Validate(ModelMetadata metadata, object container)
        {
            return ModelValidator.Validate(validator, metadata, attributeGetter.Invoke(validator), container);
        }
    }
}
#endif