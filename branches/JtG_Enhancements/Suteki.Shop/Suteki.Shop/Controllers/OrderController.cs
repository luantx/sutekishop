﻿using System;
using System.Collections.Specialized;
using System.Web.Mvc;
using Suteki.Common.Extensions;
using Suteki.Common.Repositories;
using Suteki.Common.Services;
using Suteki.Common.Validation;
using Suteki.Shop.Filters;
using Suteki.Shop.ViewData;
using Suteki.Shop.Repositories;
using Suteki.Shop.Services;
using System.Security.Permissions;
using Suteki.Common.Binders;

namespace Suteki.Shop.Controllers
{
    public class OrderController : ControllerBase
    {
        readonly IRepository<Order> orderRepository;
    	readonly IRepository<Country> countryRepository;
        readonly IRepository<CardType> cardTypeRepository;
        readonly IEncryptionService encryptionService;
    	readonly IPostageService postageService;
    	readonly IUserService userService;
		readonly IOrderSearchService searchService;

        public OrderController(
			IRepository<Order> orderRepository, 
			IRepository<Country> countryRepository, 
			IRepository<CardType> cardTypeRepository, 
			IEncryptionService encryptionService, 
			IPostageService postageService, 
			IUserService userService, IOrderSearchService searchService)
        {
            this.orderRepository = orderRepository;
        	this.searchService = searchService;
        	this.userService = userService;
        	this.countryRepository = countryRepository;
            this.cardTypeRepository = cardTypeRepository;
            this.encryptionService = encryptionService;
        	this.postageService = postageService;
        }

        [AcceptVerbs(HttpVerbs.Get), AdministratorsOnly]
        public ActionResult Index()
        {
            return Index(new OrderSearchCriteria());
        }

		[AdministratorsOnly, AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Index([DataBind(Fetch = false)] OrderSearchCriteria orderSearchCriteria)
        {
			var orders = searchService.PerformSearch(orderSearchCriteria);

            return View("Index", ShopView.Data
                .WithOrders(orders)
                .WithOrderSearchCriteria(orderSearchCriteria));
        }

        public ActionResult Item(int id)
        {
            return ItemView(id);
        }

        private ViewResult ItemView(int id)
        {
            var order = orderRepository.GetById(id);

            if (userService.CurrentUser.IsAdministrator)
            {
                var cookie = Request.Cookies["privateKey"];
                if (cookie != null)
                {
                    var privateKey = cookie.Value.Replace("%3D", "=");

                    if (!order.PayByTelephone)
                    {
                        var card = order.Card.Copy();
                        try
                        {
                            encryptionService.PrivateKey = privateKey;
                            encryptionService.DecryptCard(card);
                            return View("Item", CheckoutViewData(order).WithCard(card));
                        }
                        catch (ValidationException exception)
                        {
                            return View("Item", CheckoutViewData(order).WithErrorMessage(exception.Message));
                        }
                    }
                }
            }

			userService.CurrentUser.EnsureCanViewOrder(order);
            return View("Item", CheckoutViewData(order));
        }

        public ActionResult Print(int id)
        {
            var viewResult = ItemView(id);
            viewResult.MasterName = "Print";
            return viewResult;
        }

		[AdministratorsOnly]
        public ActionResult ShowCard(int orderId, string privateKey)
        {
            var order = orderRepository.GetById(orderId);

            var card = order.Card.Copy();

            try
            {
                encryptionService.PrivateKey = privateKey;
                encryptionService.DecryptCard(card);
                return View("Item", CheckoutViewData(order).WithCard(card));
            }
            catch (ValidationException exception)
            {
                return View("Item", CheckoutViewData(order).WithErrorMessage(exception.Message));
            }
        }

        private ShopViewData CheckoutViewData(Order order)
        {
			userService.CurrentUser.EnsureCanViewOrder(order);
            postageService.CalculatePostageFor(order);

            return ShopView.Data
                .WithCountries(countryRepository.GetAll().Active().InOrder())
                .WithCardTypes(cardTypeRepository.GetAll())
                .WithOrder(order);
        }
    }
}