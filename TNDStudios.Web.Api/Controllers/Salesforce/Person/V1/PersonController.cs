﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TNDStudios.Data.Cosmos.DocumentCache;
using TNDStudios.Web.ApiManager.Controllers;
using TNDStudios.Web.ApiManager.Data.Salesforce;

namespace TNDStudios.Web.Api.Controllers.Salesforce.Person.V1
{
    [Route("api/salesforce/person/v1")]
    [ApiController]
    public class PersonController : SalesforceNotificationController<SalesforcePerson>
    {
        /// <summary>
        /// Cosmos DB based document caching handler
        /// </summary>
        private DocumentHandler<SalesforceNotification<SalesforcePerson>> documentHandler;

        /// <summary>
        /// The organisations allowed to access this controller
        /// </summary>
        public override List<string> AllowedOrganisationIds { get; } =
            new List<string>()
            {
                "00D80000000cDmQEAU"
            };

        /// <summary>
        /// Set up logging and the document cache handler
        /// </summary>
        /// <param name="logger"></param>
        public PersonController(ILogger<SalesforceNotificationController<SalesforcePerson>> logger)
            : base(logger)
        {
            // Already got a document handler?
            if (documentHandler == null)
                documentHandler = new DocumentHandler<SalesforceNotification<SalesforcePerson>>(
                    Startup.CosmosDB,
                    "Salesforce_RecieverCache",
                    "SalesforcePerson");
        }

        /// <summary>
        /// Override the notification processor to connect it to the notification caching
        /// </summary>
        /// <param name="notifications">The list of notifications from the well formed request</param>
        /// <returns>Ack to Salesforce</returns>
        public override ActionResult<Boolean> Processor(
            List<SalesforceNotification<SalesforcePerson>> notifications)
        {
            Boolean result = true;

            // We need to make sure all notifications are cached correctly to be a success
            // otherwise we must reject the whole message
            foreach (var notification in notifications)
            {
                // Pump the notificatin to the document cache making sure we define the id
                // that we want to use as the key
                Boolean itemResult = documentHandler.SendToCache(notification.Id, notification);

                // One failed, so all fail
                result = (!itemResult) ? itemResult : result; 
            }

            // Send the result wrapped in a Http Result
            return new ObjectResult(result);
        }
    }
}
