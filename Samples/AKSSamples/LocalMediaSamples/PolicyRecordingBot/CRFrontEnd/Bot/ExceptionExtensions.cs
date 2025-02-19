﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
// Helper class to handle exceptions
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CRFrontEnd.Bot
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Microsoft.Graph;

    /// <summary>
    /// Extension methods for Exception.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Inspect the exception type/error and return the correct response.
        /// </summary>
        /// <param name="exception">The caught exception.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public static HttpResponseMessage InspectExceptionAndReturnResponse(this Exception exception)
        {
            HttpResponseMessage responseToReturn;
            if (exception is ServiceException e)
            {
                responseToReturn = (int)e.ResponseStatusCode >= 200
                    ? new HttpResponseMessage()
                    : new HttpResponseMessage(HttpStatusCode.InternalServerError);
                if (e.ResponseHeaders != null)
                {
                    foreach (var responseHeader in e.ResponseHeaders)
                    {
                        responseToReturn.Headers.TryAddWithoutValidation(responseHeader.Key, responseHeader.Value);
                    }
                }

                responseToReturn.Content = new StringContent(e.ToString());
            }
            else
            {
                responseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(exception.ToString()),
                };
            }

            return responseToReturn;
        }
    }
}
