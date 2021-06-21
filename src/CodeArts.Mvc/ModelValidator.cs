using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
#if NETCOREAPP2_0_OR_GREATER
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
using System.Web.Http.Metadata;
using System.Web.Http.Validation;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 模型验证器。
    /// </summary>
    public static class ModelValidator
    {
        private static readonly object _emptyValidationContextInstance = new object();
        private static readonly Dictionary<Type, Func<ValidationAttribute, ValidationContext, object, string>> ValidationCache = new Dictionary<Type, Func<ValidationAttribute, ValidationContext, object, string>>();

        /// <summary>
        /// 消息验证（校验异常时，返回自定义的错误消息）。
        /// </summary>
        /// <typeparam name="T">验证属性。</typeparam>
        /// <param name="validator">验证器。</param>
        public static void CustomValidate<T>(Func<T, ValidationContext, string> validator) where T : ValidationAttribute
        {
            if (validator is null)
            {
                throw new ArgumentNullException(nameof(validator));
            }

            CustomValidate<T>((attr, context, value) => validator.Invoke(attr, context));
        }

        /// <summary>
        /// 消息验证（校验异常时，返回自定义的错误消息）。
        /// </summary>
        /// <typeparam name="T">验证属性。</typeparam>
        /// <param name="validator">验证器。</param>
        public static void CustomValidate<T>(Func<T, ValidationContext, object, string> validator) where T : ValidationAttribute
        {
            if (validator is null)
            {
                throw new ArgumentNullException(nameof(validator));
            }

            ValidationCache[typeof(T)] = (attr, context, value) => validator.Invoke((T)attr, context, value);
        }
#if NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// 数据验证。
        /// </summary>
        /// <param name="validationContext">验证上下文。</param>
        /// <param name="validator">验证器。</param>
        /// <param name="validationAttribute">验证属性。</param>
        public static IEnumerable<ModelValidationResult> Validate(ModelValidationContext validationContext, IModelValidator validator, ValidationAttribute validationAttribute)
        {
            if (validationContext is null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            if (validator is null)
            {
                throw new ArgumentNullException(nameof(validator));
            }

            if (validationAttribute is null)
            {
                throw new ArgumentNullException(nameof(validationAttribute));
            }

            if (validationContext.Model is string text && (text is null || text.Length == 0) && (validationAttribute is DataTypeAttribute || validationAttribute is RegularExpressionAttribute))
            {
                yield break;
            }
            else if (ValidationCache.TryGetValue(validationAttribute.GetType(), out Func<ValidationAttribute, ValidationContext, object, string> invoke))
            {
                var metadata = validationContext.ModelMetadata;
#if NETCOREAPP3_1
                var memberName =metadata.Name;
#else
                var memberName = metadata.MetadataKind == Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.ModelMetadataKind.Property
                    ? metadata.PropertyName
                    : metadata.DisplayName;
#endif

                var container = validationContext.Container;

                var context = new ValidationContext(
                     container ?? validationContext.Model ?? _emptyValidationContextInstance,
                     validationContext.ActionContext?.HttpContext?.RequestServices,
                     null)
                {
                    DisplayName = metadata.GetDisplayName(),
                    MemberName = memberName
                };

                ValidationResult validation = validationAttribute.GetValidationResult(validationContext.Model, context);

                if (validation == ValidationResult.Success)
                {
                    yield break;
                }

                yield return new ModelValidationResult(memberName, invoke.Invoke(validationAttribute, context, validationContext.Model));
            }
            else
            {
                foreach (var validationResult in validator.Validate(validationContext))
                {
                    yield return validationResult;
                }
            }
        }
#else
        /// <summary>
        /// 数据验证。
        /// </summary>
        /// <param name="validator">验证器。</param>
        /// <param name="metadata">模型数据。</param>
        /// <param name="validationAttribute">验证属性。</param>
        /// <param name="container">数据。</param>
        public static IEnumerable<ModelValidationResult> Validate(System.Web.Http.Validation.ModelValidator validator, ModelMetadata metadata, ValidationAttribute validationAttribute, object container)
        {
            if (validator is null)
            {
                throw new ArgumentNullException(nameof(validator));
            }

            if (metadata is null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (validationAttribute is null)
            {
                throw new ArgumentNullException(nameof(validationAttribute));
            }

            if (metadata.Model is string text && (text is null || text.Length == 0) && (validationAttribute is DataTypeAttribute || validationAttribute is RegularExpressionAttribute))
            {
                yield break;
            }
            else if (ValidationCache.TryGetValue(validationAttribute.GetType(), out Func<ValidationAttribute, ValidationContext, object, string> invoke))
            {
                var memberName = metadata.GetDisplayName();
                var context = new ValidationContext(
                     container ?? metadata.Model ?? _emptyValidationContextInstance,
                     null,
                     null)
                {
                    DisplayName = metadata.GetDisplayName(),
                    MemberName = memberName
                };

                ValidationResult validation = validationAttribute.GetValidationResult(metadata.Model, context);

                if (validation == ValidationResult.Success)
                {
                    yield break;
                }

                yield return new ModelValidationResult
                {
                    MemberName = memberName,
                    Message = invoke.Invoke(validationAttribute, context, metadata.Model)
                };
            }
            else
            {
                foreach (var validationResult in validator.Validate(metadata, container))
                {
                    yield return validationResult;
                }
            }
        }
#endif
    }
}