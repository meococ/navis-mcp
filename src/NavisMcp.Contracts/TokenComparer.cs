using System;
using System.Text;

namespace NavisMcp.Contracts
{
    /// <summary>
    /// Constant-time token comparison for local bridge auth (netstandard2.0-safe).
    /// </summary>
    public static class TokenComparer
    {
        public static bool EqualsConstantTime(string? expected, string? actual)
        {
            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            {
                return false;
            }

            var left = Encoding.UTF8.GetBytes(expected);
            var right = Encoding.UTF8.GetBytes(actual);
            var diff = (uint)left.Length ^ (uint)right.Length;
            var length = Math.Min(left.Length, right.Length);
            for (var i = 0; i < length; i++)
            {
                diff |= (uint)(left[i] ^ right[i]);
            }

            return diff == 0 && left.Length == right.Length;
        }
    }
}
