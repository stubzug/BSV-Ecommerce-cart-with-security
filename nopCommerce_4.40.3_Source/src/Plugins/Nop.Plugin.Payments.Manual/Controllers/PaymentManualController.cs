using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Plugin.Payments.Manual.Models;
using Nop.Plugin.Payments.Manual.Validators;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Manual.Controllers
{
    [Newtonsoft.Json.JsonObject]
    [Serializable]
    public class PublicAddress
    {
        public string paymail { get; set; }
    }

    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class PaymentManualController : BasePaymentController
    {
        #region Fields
        
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public PaymentManualController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var manualPaymentSettings = await _settingService.LoadSettingAsync<ManualPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                TransactModeId = Convert.ToInt32(manualPaymentSettings.TransactMode),
                AdditionalFee = manualPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = manualPaymentSettings.AdditionalFeePercentage,
                PaymentDestinationAddress = manualPaymentSettings.PaymentDestinationAddress,
                TransactModeValues = await manualPaymentSettings.TransactMode.ToSelectListAsync(),
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.TransactModeId_OverrideForStore = await _settingService.SettingExistsAsync(manualPaymentSettings, x => x.TransactMode, storeScope);
                model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(manualPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(manualPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.PaymentDestinationAddress_OverrideForStore = await _settingService.SettingExistsAsync(manualPaymentSettings, x => x.PaymentDestinationAddress, storeScope);
            }

            return View("~/Plugins/Payments.Manual/Views/Configure.cshtml", model);
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

        public string GetPublicAddress(string paymentDestinationAddress)
        {
            if (ValidEmail(paymentDestinationAddress))
            {
                var address = new PublicAddress();
                address.paymail = paymentDestinationAddress;
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
                        return contents;
                    }
                }
            }
            return paymentDestinationAddress;
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var validator = new ConfigurationValidator(_localizationService);
            var validationResult = validator.Validate(model);
            //if (!validationResult.IsValid)
            //    warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var manualPaymentSettings = await _settingService.LoadSettingAsync<ManualPaymentSettings>(storeScope);

            //save settings
            manualPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            manualPaymentSettings.AdditionalFee = model.AdditionalFee;
            manualPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            manualPaymentSettings.PaymentDestinationAddress = model.PaymentDestinationAddress;
            manualPaymentSettings.PublicAddress = GetPublicAddress(model.PaymentDestinationAddress);

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */

            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.TransactMode, model.TransactModeId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.PaymentDestinationAddress, model.PaymentDestinationAddress_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.PublicAddress, model.PublicAddress_OverrideForStore, storeScope, false);


            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}