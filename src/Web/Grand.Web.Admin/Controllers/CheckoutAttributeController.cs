﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Directory;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.CheckoutAttributes)]
public class CheckoutAttributeController : BaseAdminController
{
    #region Constructors

    public CheckoutAttributeController(ICheckoutAttributeService checkoutAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        ICurrencyService currencyService,
        CurrencySettings currencySettings,
        IMeasureService measureService,
        MeasureSettings measureSettings,
        ICheckoutAttributeViewModelService checkoutAttributeViewModelService)
    {
        _checkoutAttributeService = checkoutAttributeService;
        _languageService = languageService;
        _translationService = translationService;
        _currencyService = currencyService;
        _currencySettings = currencySettings;
        _measureService = measureService;
        _measureSettings = measureSettings;
        _checkoutAttributeViewModelService = checkoutAttributeViewModelService;
    }

    #endregion

    #region Fields

    private readonly ICheckoutAttributeService _checkoutAttributeService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly ICurrencyService _currencyService;
    private readonly CurrencySettings _currencySettings;
    private readonly IMeasureService _measureService;
    private readonly MeasureSettings _measureSettings;
    private readonly ICheckoutAttributeViewModelService _checkoutAttributeViewModelService;

    #endregion

    #region Checkout attributes

    //list
    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var checkoutAttributes = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeListModel();
        var gridModel = new DataSourceResult {
            Data = checkoutAttributes.ToList(),
            Total = checkoutAttributes.Count()
        };
        return Json(gridModel);
    }

    //create
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeModel();
        //locales
        await AddLocales(_languageService, model.Locales);

        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(CheckoutAttributeModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var checkoutAttribute = await _checkoutAttributeViewModelService.InsertCheckoutAttributeModel(model);
            Success(_translationService.GetResource("Admin.Orders.CheckoutAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = checkoutAttribute.Id })
                : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        //tax categories
        await _checkoutAttributeViewModelService.PrepareTaxCategories(model, null, true);

        return View(model);
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(id);
        if (checkoutAttribute == null)
            //No checkout attribute found with the specified id
            return RedirectToAction("List");

        var model = checkoutAttribute.ToModel();
        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = checkoutAttribute.GetTranslation(x => x.Name, languageId, false);
            locale.TextPrompt = checkoutAttribute.GetTranslation(x => x.TextPrompt, languageId, false);
        });

        //tax categories
        await _checkoutAttributeViewModelService.PrepareTaxCategories(model, checkoutAttribute, false);

        //condition
        await _checkoutAttributeViewModelService.PrepareConditionAttributes(model, checkoutAttribute);

        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(CheckoutAttributeModel model, bool continueEditing)
    {
        var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(model.Id);
        if (checkoutAttribute == null)
            //No checkout attribute found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            checkoutAttribute =
                await _checkoutAttributeViewModelService.UpdateCheckoutAttributeModel(checkoutAttribute, model);
            Success(_translationService.GetResource("Admin.Orders.CheckoutAttributes.Updated"));
            if (continueEditing)
            {
                //selected tab
                await SaveSelectedTabIndex();

                return RedirectToAction("Edit", new { id = checkoutAttribute.Id });
            }

            return RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        //tax categories
        await _checkoutAttributeViewModelService.PrepareTaxCategories(model, checkoutAttribute, true);
        return View(model);
    }

    //delete
    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(id);
        await _checkoutAttributeService.DeleteCheckoutAttribute(checkoutAttribute);

        Success(_translationService.GetResource("Admin.Orders.CheckoutAttributes.Deleted"));
        return RedirectToAction("List");
    }

    #endregion

    #region Checkout attribute values

    //list
    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ValueList(string checkoutAttributeId, DataSourceRequest command)
    {
        var checkoutAttribute =
            await _checkoutAttributeViewModelService.PrepareCheckoutAttributeValuesModel(checkoutAttributeId);
        var gridModel = new DataSourceResult {
            Data = checkoutAttribute.ToList(),
            Total = checkoutAttribute.Count()
        };
        return Json(gridModel);
    }

    //create
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(string checkoutAttributeId)
    {
        var model = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeValueModel(checkoutAttributeId);
        //locales
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(CheckoutAttributeValueModel model)
    {
        var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(model.CheckoutAttributeId);
        if (checkoutAttribute == null)
            //No checkout attribute found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _checkoutAttributeViewModelService.InsertCheckoutAttributeValueModel(checkoutAttribute, model);
            return Content("");
        }

        //If we got this far, something failed, redisplay form
        model.PrimaryStoreCurrencyCode =
            (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
        model.BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name;
        return View(model);
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(string id, string checkoutAttributeId)
    {
        var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        var cav = checkoutAttribute.CheckoutAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            //No checkout attribute value found with the specified id
            return RedirectToAction("List");

        var model = await _checkoutAttributeViewModelService.PrepareCheckoutAttributeValueModel(checkoutAttribute, cav);

        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(CheckoutAttributeValueModel model)
    {
        var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(model.CheckoutAttributeId);

        var cav = checkoutAttribute.CheckoutAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            //No checkout attribute value found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _checkoutAttributeViewModelService.UpdateCheckoutAttributeValueModel(checkoutAttribute, cav, model);
            return Content("");
        }

        //If we got this far, something failed, redisplay form
        model.PrimaryStoreCurrencyCode =
            (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
        model.BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name;

        return View(model);
    }

    //delete
    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueDelete(string id, string checkoutAttributeId)
    {
        var checkoutAttribute = await _checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        var cav = checkoutAttribute.CheckoutAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            throw new ArgumentException("No checkout attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            checkoutAttribute.CheckoutAttributeValues.Remove(cav);
            await _checkoutAttributeService.UpdateCheckoutAttribute(checkoutAttribute);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}