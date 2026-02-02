using FluentValidation;

namespace VulnerableBankApi.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, decimal> PrecisionScale<T>(
        this IRuleBuilder<T, decimal> ruleBuilder, 
        int precision, 
        int scale, 
        bool ignoreTrailingZeros = false)
    {
        return ruleBuilder.Must((rootObject, value, context) =>
        {
            var decimalStr = value.ToString("0.############################");
            var dotIndex = decimalStr.IndexOf('.');
            
            if (dotIndex == -1)
            {
                // No decimal places
                return decimalStr.Length <= precision - scale;
            }

            var integerPart = decimalStr.Substring(0, dotIndex);
            var fractionalPart = decimalStr.Substring(dotIndex + 1);

            if (ignoreTrailingZeros)
            {
                fractionalPart = fractionalPart.TrimEnd('0');
            }

            return integerPart.Length <= precision - scale && 
                   fractionalPart.Length <= scale;
        })
        .WithMessage($"{{PropertyName}} must not exceed {precision} total digits, with {scale} decimal places.");
    }
}
