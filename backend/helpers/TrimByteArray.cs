namespace backend.helpers;

public static class TrimByteArray
{
    public static byte[] TrimTo16Bytes(byte[] original)
    {
        if (original.Length == 16)
            return original; // Already 16 bytes
        
        byte[] result = new byte[16];
        Array.Copy(original, result, original.Length > 16 ? 16 : original.Length); // Copy all bytes, remaining are 0s
        // Copy the first 16 bytes
        return result;
    }
}