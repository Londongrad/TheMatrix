namespace Matrix.Simulation.Primitives;

internal static class SimulationKeyValidator
{
    internal const int MaxLength = 64;

    public static string Validate(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A simulation key cannot be empty.", parameterName);

        if (value.Length > MaxLength)
            throw new ArgumentOutOfRangeException(
                parameterName,
                value.Length,
                $"A simulation key cannot exceed {MaxLength} characters.");

        if (!IsAsciiLetter(value[0]) || value[^1] == '-')
            throw InvalidFormat(parameterName);

        bool previousWasSeparator = false;

        foreach (char character in value)
        {
            bool isSeparator = character == '-';

            if ((!IsAsciiLetter(character) && !char.IsAsciiDigit(character) && !isSeparator) ||
                (isSeparator && previousWasSeparator))
                throw InvalidFormat(parameterName);

            previousWasSeparator = isSeparator;
        }

        return value;
    }

    private static bool IsAsciiLetter(char character)
    {
        return character is >= 'a' and <= 'z';
    }

    private static ArgumentException InvalidFormat(string parameterName)
    {
        return new ArgumentException(
            "A simulation key must use lowercase ASCII letters, digits, and single hyphen separators.",
            parameterName);
    }
}
