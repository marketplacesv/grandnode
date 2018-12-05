﻿using Grand.Core.Domain.Catalog;
using Grand.Framework.Kendoui;
using Grand.Framework.Mvc;
using Grand.Framework.Mvc.Filters;
using Grand.Framework.Security.Authorization;
using Grand.Services.Customers;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Messages;
using Grand.Services.Security;
using Grand.Services.Stores;
using Grand.Web.Areas.Admin.Extensions;
using Grand.Web.Areas.Admin.Models.Messages;
using Grand.Web.Areas.Admin.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Grand.Web.Areas.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Attributes)]
    public partial class ContactAttributeController : BaseAdminController
    {
        #region Fields
        private readonly IContactAttributeViewModelService _contactAttributeViewModelService;
        private readonly IContactAttributeService _contactAttributeService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreService _storeService;
        private readonly ICustomerService _customerService;

        #endregion

        #region Constructors

        public ContactAttributeController(IContactAttributeViewModelService contactAttributeViewModelService,
            IContactAttributeService contactAttributeService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IStoreService storeService,
            ICustomerService customerService)
        {
            this._contactAttributeViewModelService = contactAttributeViewModelService;
            this._contactAttributeService = contactAttributeService;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._storeService = storeService;
            this._customerService = customerService;
        }

        #endregion

        #region Contact attributes

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
        public IActionResult List(DataSourceRequest command)
        {
            var contactAttributes = _contactAttributeViewModelService.PrepareContactAttributeListModel();
            var gridModel = new DataSourceResult
            {
                Data = contactAttributes.ToList(),
                Total = contactAttributes.Count()
            };
            return Json(gridModel);
        }

        //create
        public IActionResult Create()
        {
            var model = new ContactAttributeModel();
            //locales
            AddLocales(_languageService, model.Locales);
            //Stores
            model.PrepareStoresMappingModel(null, false, _storeService);
            //ACL
            model.PrepareACLModel(null, false, _customerService);
            //condition
            _contactAttributeViewModelService.PrepareConditionAttributes(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult Create(ContactAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var contactAttribute = _contactAttributeViewModelService.InsertContactAttributeModel(model);
                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = contactAttribute.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            //Stores
            model.PrepareStoresMappingModel(null, true, _storeService);
            //ACL
            model.PrepareACLModel(null, true, _customerService);

            return View(model);
        }

        //edit
        public IActionResult Edit(string id)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(id);
            if (contactAttribute == null)
                //No contact attribute found with the specified id
                return RedirectToAction("List");

            var model = contactAttribute.ToModel();
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = contactAttribute.GetLocalized(x => x.Name, languageId, false, false);
                locale.TextPrompt = contactAttribute.GetLocalized(x => x.TextPrompt, languageId, false, false);
            });
            //ACL
            model.PrepareACLModel(contactAttribute, false, _customerService);
            //Stores
            model.PrepareStoresMappingModel(contactAttribute, false, _storeService);
            //condition
            _contactAttributeViewModelService.PrepareConditionAttributes(model, contactAttribute);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public IActionResult Edit(ContactAttributeModel model, bool continueEditing)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(model.Id);
            if (contactAttribute == null)
                //No contact attribute found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                contactAttribute = _contactAttributeViewModelService.UpdateContactAttributeModel(contactAttribute, model);
                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = contactAttribute.Id });
                }
                return RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            //Stores
            model.PrepareStoresMappingModel(contactAttribute, true, _storeService);
            //ACL
            model.PrepareACLModel(contactAttribute, true, _customerService);
            _contactAttributeViewModelService.PrepareConditionAttributes(model, contactAttribute);
            return View(model);
        }

        //delete
        [HttpPost]
        public IActionResult Delete(string id, [FromServices] ICustomerActivityService customerActivityService)
        {
            if (ModelState.IsValid)
            {
                var contactAttribute = _contactAttributeService.GetContactAttributeById(id);
                _contactAttributeService.DeleteContactAttribute(contactAttribute);

                //activity log
                customerActivityService.InsertActivity("DeleteContactAttribute", contactAttribute.Id, _localizationService.GetResource("ActivityLog.DeleteContactAttribute"), contactAttribute.Name);

                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Deleted"));
                return RedirectToAction("List");
            }
            ErrorNotification(ModelState);
            return RedirectToAction("Edit", new { id = id });
        }

        #endregion

        #region Contact attribute values

        //list
        [HttpPost]
        public IActionResult ValueList(string contactAttributeId, DataSourceRequest command)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var values = contactAttribute.ContactAttributeValues;
            var gridModel = new DataSourceResult
            {
                Data = values.Select(x => new ContactAttributeValueModel
                {
                    Id = x.Id,
                    ContactAttributeId = x.ContactAttributeId,
                    Name = contactAttribute.AttributeControlType != AttributeControlType.ColorSquares ? x.Name : string.Format("{0} - {1}", x.Name, x.ColorSquaresRgb),
                    ColorSquaresRgb = x.ColorSquaresRgb,
                    IsPreSelected = x.IsPreSelected,
                    DisplayOrder = x.DisplayOrder,
                }),
                Total = values.Count()
            };
            return Json(gridModel);
        }

        //create
        public IActionResult ValueCreatePopup(string contactAttributeId)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var model = _contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute);

            //locales
            AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost]
        public IActionResult ValueCreatePopup(ContactAttributeValueModel model)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(model.ContactAttributeId);
            if (contactAttribute == null)
                //No contact attribute found with the specified id
                return RedirectToAction("List");

            if (contactAttribute.AttributeControlType == AttributeControlType.ColorSquares)
            {
                //ensure valid color is chosen/entered
                if (String.IsNullOrEmpty(model.ColorSquaresRgb))
                    ModelState.AddModelError("", "Color is required");                
            }

            if (ModelState.IsValid)
            {
                _contactAttributeViewModelService.InsertContactAttributeValueModel(contactAttribute, model);

                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //edit
        public IActionResult ValueEditPopup(string id, string contactAttributeId)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var cav = contactAttribute.ContactAttributeValues.Where(x => x.Id == id).FirstOrDefault();
            if (cav == null)
                //No contact attribute value found with the specified id
                return RedirectToAction("List");

            var model = _contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute, cav);
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = cav.GetLocalized(x => x.Name, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost]
        public IActionResult ValueEditPopup(ContactAttributeValueModel model)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(model.ContactAttributeId);

            var cav = contactAttribute.ContactAttributeValues.Where(x => x.Id == model.Id).FirstOrDefault();
            if (cav == null)
                //No contact attribute value found with the specified id
                return RedirectToAction("List");

            if (contactAttribute.AttributeControlType == AttributeControlType.ColorSquares)
            {
                //ensure valid color is chosen/entered
                if (String.IsNullOrEmpty(model.ColorSquaresRgb))
                    ModelState.AddModelError("", "Color is required");
            }

            if (ModelState.IsValid)
            {
                _contactAttributeViewModelService.UpdateContactAttributeValueModel(contactAttribute, cav, model);
                ViewBag.RefreshPage = true;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        //delete
        [HttpPost]
        public IActionResult ValueDelete(string id, string contactAttributeId)
        {
            var contactAttribute = _contactAttributeService.GetContactAttributeById(contactAttributeId);
            var cav = contactAttribute.ContactAttributeValues.Where(x => x.Id == id).FirstOrDefault();
            if (cav == null)
                throw new ArgumentException("No contact attribute value found with the specified id");
            if (ModelState.IsValid)
            {
                contactAttribute.ContactAttributeValues.Remove(cav);
                _contactAttributeService.UpdateContactAttribute(contactAttribute);

                return new NullJsonResult();
            }
            return ErrorForKendoGridJson(ModelState);
        }
        #endregion
    }
}
