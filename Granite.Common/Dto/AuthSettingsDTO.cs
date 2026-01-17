namespace Granite.Common.Dto;

public class AuthSettingsDTO
{
    public string AuthenticationType { get; set; }

    public AuthSettingsDTO(string authenticationType)
    {
        AuthenticationType = authenticationType;
    }
}
