using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace EMI_REMAINDER.Services;

public class SmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration config, ILogger<SmsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string toPhone, string message)
    {
        var fromNumber = _config["Twilio:FromNumber"]!;
        var e164Phone = FormatToE164(toPhone);

        if (_config["Twilio:AccountSid"] == "dev_skip")
        {
            _logger.LogInformation("[DEV SMS] To: {Phone} | Message: {Message}", e164Phone, message);
            return true;
        }

        try
        {
            var smsMessage = await MessageResource.CreateAsync(
                to: new PhoneNumber(e164Phone),
                from: new PhoneNumber(fromNumber),
                body: message);

            _logger.LogInformation("SMS sent to {Phone}. SID: {Sid}", e164Phone, smsMessage.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}", e164Phone);
            return false;
        }
    }

    public async Task<bool> SendWhatsAppAsync(string toPhone, string message)
    {
        var fromNumber = _config["Twilio:FromNumber"]!;
        var e164Phone = FormatToE164(toPhone);

        if (_config["Twilio:AccountSid"] == "dev_skip")
        {
            _logger.LogInformation("[DEV WHATSAPP] To: {Phone} | Message: {Message}", e164Phone, message);
            return true;
        }

        try
        {
            var whatsappTo = $"whatsapp:{e164Phone}";
            var whatsappFrom = fromNumber.StartsWith("whatsapp:") ? fromNumber : $"whatsapp:{fromNumber}";

            var smsMessage = await MessageResource.CreateAsync(
                to: new PhoneNumber(whatsappTo),
                from: new PhoneNumber(whatsappFrom),
                body: message);

            _logger.LogInformation("WhatsApp sent to {Phone}. SID: {Sid}", e164Phone, smsMessage.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp to {Phone}", e164Phone);
            return false;
        }
    }

    private static string FormatToE164(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+")) return phone;
        if (phone.Length == 10) return $"+91{phone}";
        if (phone.StartsWith("91") && phone.Length == 12) return $"+{phone}";
        return $"+91{phone}";
    }
}
