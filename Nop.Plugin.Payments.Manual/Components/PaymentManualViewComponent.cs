using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Payments.Manual.Models;
using Nop.Web.Framework.Components;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Core;
using System.Threading.Tasks;
using Nop.Services.Configuration;
using System.Text.RegularExpressions;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

namespace Nop.Plugin.Payments.Manual.Components
{
    /*[Newtonsoft.Json.JsonObject]
    [Serializable]
    public class PublicAddress
    {
        public string paymail { get; set; }
    }*/

    [ViewComponent(Name = "PaymentManual")]
    public class PaymentManualViewComponent : NopViewComponent
    {
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;

        public PaymentManualViewComponent(IOrderTotalCalculationService orderTotalCalculationService,
                                    IShoppingCartService shoppingCartService,
                                    IWorkContext workContext,
                                    IStoreContext storeContext,
                                    ISettingService settingService)
        {
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
            _orderTotalCalculationService = orderTotalCalculationService;
            _storeContext = storeContext;
            _settingService = settingService;
        }

        public async Task<decimal?> GetOrderTotal()
        {
            var shoppingCart = (await _shoppingCartService
                    .GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), Core.Domain.Orders.ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id))
                    .ToList();
            var orderTotal = Math.Round((await _orderTotalCalculationService.GetShoppingCartTotalAsync(shoppingCart, usePaymentMethodAdditionalFee: false)).shoppingCartTotal ?? decimal.Zero, 2);

            //_orderTotalCalculationService.GetShoppingCartTotalAsync(shoppingCart);

            return orderTotal;

        }

        /*public bool ValidEmail(string email)
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
        }*/

        public async Task<string> GetPublicAddress()
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var manualPaymentSettings = await _settingService.LoadSettingAsync<ManualPaymentSettings>(storeScope);

            /*if (ValidEmail(manualPaymentSettings.PaymentDestinationAddress))
            {
                var address = new PublicAddress();
                address.paymail = manualPaymentSettings.PaymentDestinationAddress;
                string key = "HxLw3QGt0cagPVxmSII5/y9wS9AqsAiXq4WZGFa520SOzO7YPCmQeA==";
                string apiEndpoint = string.Format("https://fun-paymail-edx-bsv.azurewebsites.net/api/get_paymail_address?code={0}", key);

                using (var client = new HttpClient())
                {
                    
                    string myJson = JsonConvert.SerializeObject(address);

                    var response = client.PostAsync(
                        apiEndpoint,
                        new StringContent(myJson, Encoding.UTF8, "application/json")).Result;

                    var contents = response.Content.ReadAsStringAsync().Result;

                    if (!string.IsNullOrEmpty(contents))
                    {
                        manualPaymentSettings.PaymentDestinationAddress = contents;
                    }
                }
            }*/
            return manualPaymentSettings.PublicAddress;
        }

        public IViewComponentResult Invoke()
        {
            decimal? orderTotal = GetOrderTotal().Result;

            var model = new PaymentInfoModel()
            {
                CreditCardTypes = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Visa", Value = "visa" },
                    new SelectListItem { Text = "Master card", Value = "MasterCard" },
                    new SelectListItem { Text = "Discover", Value = "Discover" },
                    new SelectListItem { Text = "Amex", Value = "Amex" },
                },

                RequiredPaymentAmount = orderTotal,
                PaymentDestinationAddress = GetPublicAddress().Result
            };

            //years
            for (var i = 0; i < 15; i++)
            {
                var year = (DateTime.Now.Year + i).ToString();
                model.ExpireYears.Add(new SelectListItem { Text = year, Value = year, });
            }

            //months
            for (var i = 1; i <= 12; i++)
            {
                model.ExpireMonths.Add(new SelectListItem { Text = i.ToString("D2"), Value = i.ToString(), });
            }

            //set postback values (we cannot access "Form" with "GET" requests)
            /*if (Request.Method != WebRequestMethods.Http.Get)
            {
                var form = Request.Form;
                model.CardholderName = form["CardholderName"];
                model.CardNumber = form["CardNumber"];
                model.CardCode = form["CardCode"];
                var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedCcType != null)
                    selectedCcType.Selected = true;
                var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedMonth != null)
                    selectedMonth.Selected = true;
                var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedYear != null)
                    selectedYear.Selected = true;
            }*/

            return View("~/Plugins/Payments.Manual/Views/PaymentInfo.cshtml", model);
        }
    }
}
