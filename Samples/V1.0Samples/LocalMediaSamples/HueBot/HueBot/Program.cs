// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace HueBot
{
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Sample.Common.Logging;

    /// <summary>
    /// Main entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Global logger instance.
        /// </summary>
        private static readonly IGraphLogger Logger = new GraphLogger(typeof(Program).Assembly.GetName().Name);

        /// <summary>
        /// Main method.
        /// </summary>
        public static void Main()
        {
            // Initialize observer
            var observer = new SampleObserver(Logger);

            // Create and run the bot
            var bot = new HueBot(Logger, observer);
            bot.Run();
        }
    }
}