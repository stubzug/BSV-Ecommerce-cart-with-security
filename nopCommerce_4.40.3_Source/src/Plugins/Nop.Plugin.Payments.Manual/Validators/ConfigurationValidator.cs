using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using Newtonsoft.Json;
using Nop.Plugin.Payments.Manual.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Manual.Validators
{
    public partial class ConfigurationValidator: BaseNopValidator<ConfigurationModel>
    {
        [JsonObject]
        [Serializable]
        public class PublicAddress
        {
            public string paymail { get; set; }
        }

        public ConfigurationValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.PaymentDestinationAddress).NotEmpty().WithMessage("Payment destination address is required.");

            RuleFor(x => x.PaymentDestinationAddress).Must((x, context) =>
            {
                var address = new PublicAddress();
                address.paymail = x.PaymentDestinationAddress;
                string key = "HxLw3QGt0cagPVxmSII5/y9wS9AqsAiXq4WZGFa520SOzO7YPCmQeA==";
                string apiEndpoint = string.Format("https://fun-paymail-edx-bsv.azurewebsites.net/api/get_paymail_address?code={0}", key);

                using (var client = new HttpClient())
                {
                    if (ValidEmail(address.paymail))
                    {
                        string myJson = JsonConvert.SerializeObject(address);
                        
                        var response = client.PostAsync(
                            apiEndpoint,
                            new StringContent(myJson, Encoding.UTF8, "application/json")).Result;

                        var contents = response.Content.ReadAsStringAsync().Result;

                        if (string.IsNullOrEmpty(contents))
                            return false;
                        else
                            return true;
                    }
                    else
                    {
                        apiEndpoint = string.Format("https://api.whatsonchain.com/v1/bsv/main/address/{0}/info", address.paymail);

                        var response = client.GetAsync(apiEndpoint).Result;
                        var contents = response.Content.ReadAsStringAsync().Result;

                        dynamic data = JsonConvert.DeserializeObject(contents);
                        if (data.isvalid == false)
                            return false;
                        else
                            return true;

                    }

                }
            }).WithMessage("Payment destination addreess is not valid.");
        }

        public bool ValidEmail(string email)
        {
            Regex r = new Regex(@"^[a-z0-9]+[\._]?[a-z0-9]+[@]\w+[.]\w{2,3}$");

            Match match = r.Match(email.ToLower());

            if (match.Success)
            {
                return true;
            }
            else
            {
                // check if it's a custom email
                r = new Regex(@"^[a-z0-9]+[\._]?[a-z0-9]+[@]\w+[.]\w+$");
                match = r.Match(email.ToLower());
                if (match.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
