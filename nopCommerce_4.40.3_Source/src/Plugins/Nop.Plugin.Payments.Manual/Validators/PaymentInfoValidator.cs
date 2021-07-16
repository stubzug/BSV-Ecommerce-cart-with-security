using System;
using FluentValidation;
using Nop.Plugin.Payments.Manual.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Manual.Validators
{
    public partial class PaymentInfoValidator : BaseNopValidator<PaymentInfoModel>
    {
        [JsonObject]
        [Serializable]
        public class CryptoPayment
        {
            public string PaymentToAddress { get; set; }
            public string RequiredPaymentAmount { get; set; }
        }

        private static readonly HttpClient _client = new HttpClient();

        public PaymentInfoValidator(ILocalizationService localizationService)
        {
           

            //useful links:
            //http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
            //http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/

            RuleFor(x => x.PaymentDestinationAddress).Must((x, context) =>
            {
                var payment = new CryptoPayment();
                payment.PaymentToAddress = x.PaymentDestinationAddress;
                payment.RequiredPaymentAmount = x.RequiredPaymentAmount.ToString();
       
                using (var client = new HttpClient())
                {
                    string myJson = JsonConvert.SerializeObject(payment);

                    var response = client.PostAsync(
                        "https://fun-validate-crypto-payment.azurewebsites.net/api/ValidateCryptoPayment?code=aDuFw2uypjUA8KR9V3Q02yaDn9eRZivAAx3lcsMuyuFVRpxgr3UxZA==",
                         new StringContent(myJson, Encoding.UTF8, "application/json")).Result;

                    var contents = response.Content.ReadAsStringAsync().Result;

                    if (contents == "True")
                        return true;
                    else
                        return false;
                }

            }).WithMessage("Invalid payment!");
        }
    }
}