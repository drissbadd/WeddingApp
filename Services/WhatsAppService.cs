using Twilio;
using Twilio.Rest.Api.V2010.Account;
using WeddingApp.Models;

namespace WeddingApp.Services;

public class WhatsAppService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly bool _isConfigured;
    private readonly string _from;

    public WhatsAppService(IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _config = config;
        _logger = logger;

        var accountSid = config["Twilio:AccountSid"];
        var authToken = config["Twilio:AuthToken"];
        _from = config["Twilio:WhatsAppFrom"] ?? "whatsapp:+14155238886";

        if (!string.IsNullOrWhiteSpace(accountSid) && !string.IsNullOrWhiteSpace(authToken))
        {
            TwilioClient.Init(accountSid, authToken);
            _isConfigured = true;
        }
    }

    public bool IsConfigured => _isConfigured;

    public async Task<(bool Success, string Message)> SendInvitationAsync(Guest guest, string baseUrl)
    {
        if (!_isConfigured)
            return (false, "WhatsApp n'est pas configuré. Veuillez ajouter vos identifiants Twilio dans appsettings.json.");

        try
        {
            var rsvpLink = $"{baseUrl}/rsvp/{guest.RsvpToken}";

            var body = $"🌸 *Invitation au Mariage* 🌸\n" +
                       $"*Zineb & Driss* 💍\n\n" +
                       $"Cher(e) *{guest.Name}*,\n\n" +
                       $"Nous avons la joie et l'honneur de vous inviter à célébrer notre mariage.\n\n" +
                       $"Votre présence sera pour nous le plus beau des cadeaux. 🎁\n\n" +
                       $"📋 Merci de confirmer votre présence via le lien ci-dessous :\n" +
                       $"{rsvpLink}\n\n" +
                       $"Avec tout notre amour,\n" +
                       $"*Zineb & Driss* 💕";

            await MessageResource.CreateAsync(
                body: body,
                from: new Twilio.Types.PhoneNumber(_from),
                to: new Twilio.Types.PhoneNumber($"whatsapp:{guest.PhoneNumber}")
            );

            return (true, $"Invitation envoyée à {guest.Name} !");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp to {Phone}", guest.PhoneNumber);
            return (false, $"Échec de l'envoi : {ex.Message}");
        }
    }
}
