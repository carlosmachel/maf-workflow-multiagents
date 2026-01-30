using System.ComponentModel;

namespace Console.App.Kycs;

public static class KycTools
{
    [Description("Know Your Customer by by CPF number")]
    public static string ValidateCpf(
        [Description("The CPF formated or unformatted")]
        string cpf)
    {
        return cpf == "123.456.789-00" ? "Rejected" : "Approved";
    }
}