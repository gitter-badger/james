﻿using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WinFred.ValidationRules
{
    internal class FileExtensionValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var bindingGroup = value as BindingGroup;
            if (bindingGroup != null)
            {
                var fileExtension = bindingGroup.Items[0] as FileExtension;
                if (fileExtension != null && (fileExtension.Priority < -1 || fileExtension.Priority > 10000))
                {
                    return new ValidationResult(false, "Priority must be inside the range ]-1, 10000[");
                }
                if (fileExtension.Extension.Contains("."))
                {
                    return new ValidationResult(false, "Extension must not contain a dot");
                }
            }
            return ValidationResult.ValidResult;
        }
    }
}