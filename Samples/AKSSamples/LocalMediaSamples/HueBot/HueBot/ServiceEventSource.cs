// <copyright file="ServiceEventSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace HueBot
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Simplified event source for AKS.
    /// </summary>
    [EventSource(Name = "Sample.HueBot")]
    internal sealed class ServiceEventSource : EventSource
    {
        public static readonly ServiceEventSource Current = new ServiceEventSource();

        private ServiceEventSource()
        {
        }

        /// <summary>
        /// Log a general message.
        /// </summary>
        [Event(1, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(1, message);
            }
        }

        /// <summary>
        /// Log an error during service initialization.
        /// </summary>
        [Event(4, Level = EventLevel.Error, Message = "Service initialization failed")]
        public void ServiceInitializationFailed(string exception)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(4, exception);
            }
        }

        /// <summary>
        /// Keywords for event categorization (optional).
        /// </summary>
        public static class Keywords
        {
            public const EventKeywords Requests = (EventKeywords)0x1L;
            public const EventKeywords ServiceInitialization = (EventKeywords)0x2L;
        }
    }
}