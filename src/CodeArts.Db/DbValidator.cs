using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CodeArts.Db
{
    /// <summary>
    /// 验证器。
    /// </summary>
    public static class DbValidator
    {
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

            ValidationCache[typeof(T)] = (attr, context, value) => validator.Invoke((T)attr, context);
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

        /// <summary>
        /// 数据验证。
        /// </summary>
        /// <param name="value">数据。</param>
        /// <param name="validationContext">验证上下文。</param>
        /// <param name="validationAttributes">验证属性。</param>
        public static void ValidateValue(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes)
        {
            if (validationContext is null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            if (validationAttributes is null)
            {
                throw new ArgumentNullException(nameof(validationAttributes));
            }

            foreach (ValidationAttribute attr in validationAttributes)
            {
                ValidationResult validation = attr.GetValidationResult(value, validationContext);

                if (validation == ValidationResult.Success) continue;

                if (ValidationCache.TryGetValue(attr.GetType(), out Func<ValidationAttribute, ValidationContext, object, string> invoke))
                {
                    validation.ErrorMessage = invoke.Invoke(attr, validationContext, value);
                }

                throw new ValidationException(validation, attr, value);
            }
        }
    }
}
