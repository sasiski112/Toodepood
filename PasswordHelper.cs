namespace Tooded_EFCore;

public static class PasswordHelper
{
    // Teeb paroolist räsi. Sama parool annab iga kord erineva räsi (sool).
    public static string Hash(string parool) =>
        BCrypt.Net.BCrypt.HashPassword(parool);

    // Kontrollib, kas sisestatud parool vastab salvestatud räsile
    public static bool Verify(string parool, string rasi) =>
        BCrypt.Net.BCrypt.Verify(parool, rasi);
}
