namespace WolfLeash.Extensions;

using Base64Guid = string;

public static class GuidExtension
{
    public static Base64Guid ToBase64(this Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray())
            .Replace("/", "-").Replace("+", "_").Replace("=", "");
    }
}