﻿using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Vendors;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Vendors;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Vendors;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Grand.SharedKernel.Attributes;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class CatalogController : BasePublicController
{
    #region Constructors

    public CatalogController(
        IVendorService vendorService,
        ICategoryService categoryService,
        IBrandService brandService,
        ICollectionService collectionService,
        IWorkContextAccessor workContextAccessor,
        IGroupService groupService,
        ITranslationService translationService,
        IAclService aclService,
        IPermissionService permissionService,
        IMediator mediator,
        VendorSettings vendorSettings)
    {
        _vendorService = vendorService;
        _categoryService = categoryService;
        _brandService = brandService;
        _collectionService = collectionService;
        _workContextAccessor = workContextAccessor;
        _groupService = groupService;
        _translationService = translationService;
        _aclService = aclService;
        _permissionService = permissionService;
        _mediator = mediator;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Fields

    private readonly IVendorService _vendorService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;
    private readonly ICollectionService _collectionService;
    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IAclService _aclService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;

    private readonly VendorSettings _vendorSettings;

    #endregion

    #region Utilities
    private VendorReviewOverviewModel PrepareVendorReviewOverviewModel(Domain.Vendors.Vendor vendor)
    {
        var model = new VendorReviewOverviewModel {
            RatingSum = vendor.ApprovedRatingSum,
            TotalReviews = vendor.ApprovedTotalReviews,
            VendorId = vendor.Id,
            AllowCustomerReviews = vendor.AllowCustomerReviews
        };
        return model;
    }

    #endregion

    #region Categories

    [ProducesResponseType(typeof(CategoryModel), StatusCodes.Status200OK)]
    [HttpGet]
    public virtual async Task<IActionResult> Category(string categoryId, CatalogPagingFilteringModel command)
    {
        var category = await _categoryService.GetCategoryById(categoryId);
        if (category == null)
            return InvokeHttp404();

        var customer = _workContextAccessor.WorkContext.CurrentCustomer;

        //Check whether the current user has a "Manage catalog" permission
        //It allows him to preview a category before publishing
        if (!category.Published && !await _permissionService.Authorize(StandardPermission.ManageCategories, customer))
            return InvokeHttp404();

        //ACL (access control list)
        if (!_aclService.Authorize(category, customer))
            return InvokeHttp404();

        //Store access
        if (!_aclService.Authorize(category, _workContextAccessor.WorkContext.CurrentStore.Id))
            return InvokeHttp404();

        //display "edit" (manage) link
        if (await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer) &&
            await _permissionService.Authorize(StandardPermission.ManageCategories, customer))
            DisplayEditLink(Url.Action("Edit", "Category", new { id = category.Id, area = "Admin" }));

        //model
        var model = await _mediator.Send(new GetCategory {
            Category = category,
            Command = command,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        //layout
        var layoutViewPath =
            await _mediator.Send(new GetCategoryLayoutViewPath { LayoutId = category.CategoryLayoutId });

        return View(layoutViewPath, model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> CategoryAll(CategoryPagingModel command)
    {
        var model = await _mediator.Send(new GetCategoryAll {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Command = command
        });
        return View(model);
    }

    #endregion

    #region Brands

    [HttpGet]
    public virtual async Task<IActionResult> Brand(string brandId, CatalogPagingFilteringModel command)
    {
        var brand = await _brandService.GetBrandById(brandId);
        if (brand == null)
            return InvokeHttp404();

        var customer = _workContextAccessor.WorkContext.CurrentCustomer;

        //Check whether the current user has a "Manage catalog" permission
        //It allows him to preview a collection before publishing
        if (!brand.Published && !await _permissionService.Authorize(StandardPermission.ManageBrands, customer))
            return InvokeHttp404();

        //ACL (access control list)
        if (!_aclService.Authorize(brand, customer))
            return InvokeHttp404();

        //Store access
        if (!_aclService.Authorize(brand, _workContextAccessor.WorkContext.CurrentStore.Id))
            return InvokeHttp404();

        //display "edit" (manage) link
        if (await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer) &&
            await _permissionService.Authorize(StandardPermission.ManageBrands, customer))
            DisplayEditLink(Url.Action("Edit", "Brand", new { id = brand.Id, area = "Admin" }));

        //model
        var model = await _mediator.Send(new GetBrand {
            Command = command,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Brand = brand,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        //template
        var layoutViewPath = await _mediator.Send(new GetBrandLayoutViewPath { LayoutId = brand.BrandLayoutId });

        return View(layoutViewPath, model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> BrandAll(BrandPagingModel command)
    {
        var model = await _mediator.Send(new GetBrandAll {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Command = command
        });
        return View(model);
    }

    #endregion

    #region Collections

    [HttpGet]
    public virtual async Task<IActionResult> Collection(string collectionId, CatalogPagingFilteringModel command)
    {
        var collection = await _collectionService.GetCollectionById(collectionId);
        if (collection == null)
            return InvokeHttp404();

        var customer = _workContextAccessor.WorkContext.CurrentCustomer;

        //Check whether the current user has a "Manage catalog" permission
        //It allows him to preview a collection before publishing
        if (!collection.Published &&
            !await _permissionService.Authorize(StandardPermission.ManageCollections, customer))
            return InvokeHttp404();

        //ACL (access control list)
        if (!_aclService.Authorize(collection, customer))
            return InvokeHttp404();

        //Store access
        if (!_aclService.Authorize(collection, _workContextAccessor.WorkContext.CurrentStore.Id))
            return InvokeHttp404();

        //display "edit" (manage) link
        if (await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer) &&
            await _permissionService.Authorize(StandardPermission.ManageCollections, customer))
            DisplayEditLink(Url.Action("Edit", "Collection", new { id = collection.Id, area = "Admin" }));

        //model
        var model = await _mediator.Send(new GetCollection {
            Command = command,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Collection = collection,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        //template
        var layoutViewPath = await _mediator.Send(new GetCollectionLayoutViewPath
            { LayoutId = collection.CollectionLayoutId });

        return View(layoutViewPath, model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> CollectionAll(CollectionPagingModel command)
    {
        var model = await _mediator.Send(new GetCollectionAll {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Command = command
        });
        return View(model);
    }

    #endregion

    #region Vendors

    [HttpGet]
    public virtual async Task<IActionResult> Vendor(string vendorId, CatalogPagingFilteringModel command)
    {
        var vendor = await _vendorService.GetVendorById(vendorId);
        if (vendor == null || vendor.Deleted || !vendor.Active)
            return InvokeHttp404();

        //Vendor is active?
        if (!vendor.Active)
            return InvokeHttp404();

        var customer = _workContextAccessor.WorkContext.CurrentCustomer;

        //display "edit" (manage) link
        if (await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer) &&
            await _permissionService.Authorize(StandardPermission.ManageCollections, customer))
            DisplayEditLink(Url.Action("Edit", "Vendor", new { id = vendor.Id, area = "Admin" }));

        var model = await _mediator.Send(new GetVendor {
            Command = command,
            Vendor = vendor,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        //review
        model.VendorReviewOverview = PrepareVendorReviewOverviewModel(vendor);

        return View(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> VendorAll(VendorPagingModel command)
    {
        //we don't allow viewing of vendors if "vendors" block is hidden
        if (_vendorSettings.VendorsBlockItemsToDisplay == 0)
            return RedirectToRoute("HomePage");

        var model = await _mediator.Send(
            new GetVendorAll { Language = _workContextAccessor.WorkContext.WorkingLanguage, Command = command });
        return View(model);
    }

    #endregion

    #region Vendor reviews

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    public virtual async Task<IActionResult> VendorReviews(VendorReviewsModel model)
    {
        var vendor = await _vendorService.GetVendorById(model.VendorId);
        if (vendor is not { Active: true } || !vendor.AllowCustomerReviews)
            return Content("");

        if (ModelState.IsValid)
        {
            var vendorReview = await _mediator.Send(new InsertVendorReviewCommand
                { Vendor = vendor, Store = _workContextAccessor.WorkContext.CurrentStore, Model = model });
            //raise event
            if (vendorReview.IsApproved)
                await _mediator.Publish(new VendorReviewApprovedEvent(vendorReview));

            model = await _mediator.Send(new GetVendorReviews { Vendor = vendor });
            model.AddVendorReview.Title = null;
            model.AddVendorReview.ReviewText = null;

            model.AddVendorReview.SuccessfullyAdded = true;
            if (!vendorReview.IsApproved)
            {
                model.AddVendorReview.Result = _translationService.GetResource("VendorReviews.SeeAfterApproving");
            }
            else
            {
                model.AddVendorReview.Result = _translationService.GetResource("VendorReviews.SuccessfullyAdded");
                model.VendorReviewOverview = PrepareVendorReviewOverviewModel(vendor);
            }

            return Json(model);
        }

        var returnModel = await _mediator.Send(new GetVendorReviews { Vendor = vendor });
        returnModel.AddVendorReview.ReviewText = model.AddVendorReview.ReviewText;
        returnModel.AddVendorReview.Title = model.AddVendorReview.Title;
        returnModel.AddVendorReview.SuccessfullyAdded = false;
        returnModel.AddVendorReview.Result =
            string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));

        return Json(returnModel);
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> SetVendorReviewHelpfulness(string VendorReviewId, string vendorId,
        bool washelpful)
    {
        var vendor = await _vendorService.GetVendorById(vendorId);
        var vendorReview = await _vendorService.GetVendorReviewById(VendorReviewId);
        if (vendorReview == null)
            throw new ArgumentException("No vendor review found with the specified id");

        var customer = _workContextAccessor.WorkContext.CurrentCustomer;

        if (await _groupService.IsGuest(customer) && !_vendorSettings.AllowAnonymousUsersToReviewVendor)
            return Json(new {
                Result = _translationService.GetResource("VendorReviews.Helpfulness.OnlyRegistered"),
                TotalYes = vendorReview.HelpfulYesTotal,
                TotalNo = vendorReview.HelpfulNoTotal
            });

        //customers aren't allowed to vote for their own reviews
        if (vendorReview.CustomerId == customer.Id)
            return Json(new {
                Result = _translationService.GetResource("VendorReviews.Helpfulness.YourOwnReview"),
                TotalYes = vendorReview.HelpfulYesTotal,
                TotalNo = vendorReview.HelpfulNoTotal
            });

        vendorReview = await _mediator.Send(new SetVendorReviewHelpfulnessCommand {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Vendor = vendor,
            Review = vendorReview,
            Washelpful = washelpful
        });

        return Json(new {
            Result = _translationService.GetResource("VendorReviews.Helpfulness.SuccessfullyVoted"),
            TotalYes = vendorReview.HelpfulYesTotal,
            TotalNo = vendorReview.HelpfulNoTotal
        });
    }

    #endregion

    #region Product tags

    [HttpGet]
    public virtual async Task<IActionResult> ProductsByTag(string productTagId, CatalogPagingFilteringModel command,
        [FromServices] IProductTagService productTagService)
    {
        var productTag = await productTagService.GetProductTagById(productTagId);
        if (productTag == null)
            return InvokeHttp404();

        var model = await _mediator.Send(new GetProductsByTag {
            Command = command,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            ProductTag = productTag,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> ProductsByTagName(string seName, CatalogPagingFilteringModel command,
        [FromServices] IProductTagService productTagService)
    {
        var productTag = await productTagService.GetProductTagBySeName(seName);
        if (productTag == null)
            return InvokeHttp404();

        var model = await _mediator.Send(new GetProductsByTag {
            Command = command,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            ProductTag = productTag,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View("ProductsByTag", model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> ProductTagsAll()
    {
        var model = await _mediator.Send(new GetProductTagsAll {
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View(model);
    }

    #endregion

    #region Searching

    [HttpGet]
    public virtual async Task<IActionResult> Search(SearchModel model, CatalogPagingFilteringModel command)
    {
        if (model != null && !string.IsNullOrEmpty(model.SearchCategoryId))
        {
            model.cid = model.SearchCategoryId;
            model.adv = true;
        }

        //Prepare model
        var isSearchTermSpecified = HttpContext.Request.Query.ContainsKey("q");
        var searchModel = await _mediator.Send(new GetSearch {
            Command = command,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            IsSearchTermSpecified = isSearchTermSpecified,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Model = model,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View(searchModel);
    }

    [HttpGet]
    public virtual async Task<IActionResult> SearchTermAutoComplete(string term, string categoryId,
        [FromServices] CatalogSettings catalogSettings)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < catalogSettings.ProductSearchTermMinimumLength)
            return Content("");

        var result = await _mediator.Send(new GetSearchAutoComplete {
            CategoryId = categoryId,
            Term = term.Trim(),
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency
        });
        return Json(result);
    }

    #endregion
}