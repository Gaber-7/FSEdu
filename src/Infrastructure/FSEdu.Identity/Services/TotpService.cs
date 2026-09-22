using System.Security.Cryptography;
using System.Text;
using FSEdu.Application.Abstractions;

namespace FSEdu.Identity.Services;

// RFC 6238 TOTP implementation (SHA1, 6 digits, 30s step) compatible with
// Google Authenticator / Microsoft Authenticator / Authy / 1Password.
public sealed class TotpService : ITotpService
{
    private const int Step = 30;
    private const int Digits = 6;

    public string GenerateSecretBase32()
    {
        var bytes = RandomNumberGenerator.GetBytes(20); // 160 bits as recommended
        return Base32Encode(bytes);
    }

    public string BuildProvisioningUri(string issuer, string accountLabel, string secretBase32)
    {
        var enc = (string s) => Uri.EscapeDataString(s);
        // otpauth://totp/Issuer:label?secret=...&issuer=Issuer&algorithm=SHA1&digits=6&period=30
        return $"otpauth://totp/{enc(issuer)}:{enc(accountLabel)}" +
               $"?secret={secretBase32}" +
               $"&issuer={enc(issuer)}" +
               $"&algorithm=SHA1&digits={Digits}&period={Step}";
    }

    public bool VerifyCode(string secretBase32, string code)
    {
        if (string.IsNullOrWhiteSpace(secretBase32) || string.IsNullOrWhiteSpace(code)) return false;
        var clean = new string(code.Where(char.IsDigit).ToArray());
        if (clean.Length != Digits) return false;

        var key = Base32Decode(secretBase32);
        var counter = (long)((DateTimeOffset.UtcNow.ToUnixTimeSeconds()) / Step);

        // Allow +/- 1 window for clock skew
        for (long delta = -1; delta <= 1; delta++)
        {
            if (ComputeHotp(key, counter + delta) == clean) return true;
        }
        return false;
    }

    public (List<string> Plain, string HashedJoined) GenerateRecoveryCodes(int count = 10)
    {
        var plain = new List<string>(count);
        var hashes = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            var raw = RandomNumberGenerator.GetBytes(8);
            var hex = Convert.ToHexString(raw); // 16 hex chars
            var pretty = $"{hex.Substring(0, 4)}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}";
            plain.Add(pretty);
            hashes.Add(HashRecoveryCode(pretty));
        }
        return (plain, string.Join("|", hashes));
    }

    public string HashRecoveryCode(string code)
    {
        var normalized = new string(code.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private static string ComputeHotp(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);

        var mod = (int)Math.Pow(10, Digits);
        return (binary % mod).ToString().PadLeft(Digits, '0');
    }

    // ─── Base32 (RFC 4648) ───────────────────────────────
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string Base32Encode(byte[] data)
    {
        if (data.Length == 0) return "";
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = data[0], next = 1, bitsLeft = 8;
        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length) { buffer = (buffer << 8) | (data[next++] & 0xFF); bitsLeft += 8; }
                else { var pad = 5 - bitsLeft; buffer <<= pad; bitsLeft = 5; }
            }
            int index = (buffer >> (bitsLeft - 5)) & 0x1F;
            bitsLeft -= 5;
            sb.Append(Base32Alphabet[index]);
        }
        return sb.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        input = input.Trim().TrimEnd('=').ToUpperInvariant();
        if (input.Length == 0) return Array.Empty<byte>();
        int byteCount = input.Length * 5 / 8;
        var result = new byte[byteCount];
        int buffer = 0, bitsLeft = 0, idx = 0;
        foreach (var c in input)
        {
            var i = Base32Alphabet.IndexOf(c);
            if (i < 0) throw new FormatException($"Invalid base32 char: {c}");
            buffer = (buffer << 5) | i;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                result[idx++] = (byte)((buffer >> (bitsLeft - 8)) & 0xFF);
                bitsLeft -= 8;
            }
        }
        return result;
    }
}
