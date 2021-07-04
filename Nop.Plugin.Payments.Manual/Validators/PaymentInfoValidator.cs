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
            public string PaymentFromAddress { get; set; }
            public string PaymentToAddress { get; set; }
            public string RequiredPaymentAmount { get; set; }
            public string TxHash { get; set; }
        }

        private static readonly HttpClient _client = new HttpClient();
        

       

        public PaymentInfoValidator(ILocalizationService localizationService)
        {
           

            //useful links:
            //http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
            //http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/

            //RuleFor(x => x.CardNumber).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardNumber.Required"));
            //RuleFor(x => x.CardCode).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardCode.Required"));

            RuleFor(x => x.TxHash).NotEmpty().WithMessage("Transaction ID is required.");

            //RuleFor(x => x.CardholderName).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Payment.CardholderName.Required"));
            //RuleFor(x => x.CardNumber).IsCreditCard().WithMessageAwait(localizationService.GetResourceAsync("Payment.CardNumber.Wrong"));
            //RuleFor(x => x.CardCode).Matches(@"^[0-9]{3,4}$").WithMessageAwait(localizationService.GetResourceAsync("Payment.CardCode.Wrong"));
            //RuleFor(x => x.ExpireMonth).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Payment.ExpireMonth.Required"));
            //RuleFor(x => x.ExpireYear).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Payment.ExpireYear.Required"));
            //RuleFor(x => x.ExpireMonth).Must((x, context) =>
            //{
            //    //not specified yet
            //    if (string.IsNullOrEmpty(x.ExpireYear) || string.IsNullOrEmpty(x.ExpireMonth))
            //        return true;

            //    //the cards remain valid until the last calendar day of that month
            //    //If, for example, an expiration date reads 06/15, this means it can be used until midnight on June 30, 2015
            //    var enteredDate = new DateTime(int.Parse(x.ExpireYear), int.Parse(x.ExpireMonth), 1).AddMonths(1);

            //    if (enteredDate < DateTime.Now)
            //        return false;

            //    return true;
            //}).WithMessageAwait(localizationService.GetResourceAsync("Payment.ExpirationDate.Expired"));

            RuleFor(x => x.TxHash).Must((x, context) =>
            {
                //not specified yet
                //if (string.IsNullOrEmpty(x.ExpireYear) || string.IsNullOrEmpty(x.ExpireMonth))
                //    return true;

                //the cards remain valid until the last calendar day of that month
                //If, for example, an expiration date reads 06/15, this means it can be used until midnight on June 30, 2015
                //var enteredDate = new DateTime(int.Parse(x.ExpireYear), int.Parse(x.ExpireMonth), 1).AddMonths(1);

                //if (enteredDate < DateTime.Now)
                //    return false;

                //return true;

               
                //var responseString = response.Content.ReadAsStringAsync();

                

                //string myJson = "{\"payment_from_address\": \"rcdumlao@centbee.com\",\"payment_to_address\":\"rcdumlao@centbee.com\",\"required_payment_amount\":\".01\",\"tx_hash\":\"802af3ce9b2f8cdd4a5570e56ece6fbe0ec2762227314f61445c566964d8c9a0\"}";
                var payment = new CryptoPayment();
                payment.PaymentFromAddress = "rcdumlao@entbee.com";
                payment.PaymentToAddress = "1BnHDoDQmAgbyTnmJnuvSVhDqgKdxLY2gR";
                payment.RequiredPaymentAmount = x.RequiredPaymentAmount.ToString();
                payment.TxHash = x.TxHash;
       
                using (var client = new HttpClient())
                {
                    //var json = new JavaScriptSerializer().Serialize(model);
                    string myJson = JsonConvert.SerializeObject(payment);
                    //var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = client.PostAsync(
                        "https://fun-validate-crypto-payment.azurewebsites.net/api/ValidateCryptoPayment?code=aDuFw2uypjUA8KR9V3Q02yaDn9eRZivAAx3lcsMuyuFVRpxgr3UxZA==",
                         new StringContent(myJson, Encoding.UTF8, "application/json")).Result;

                    var contents = response.Content.ReadAsStringAsync().Result;

                    if (contents == "True")
                        return true;
                    else
                        return false;
                }

            }).WithMessage("Invalid Transaction ID.");
        }
    }
}