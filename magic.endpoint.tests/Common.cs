/*
 * Aista Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using magic.node;
using magic.signals.services;
using magic.signals.contracts;
using magic.endpoint.services;
using magic.endpoint.contracts;
using magic.node.extensions.hyperlambda;
using magic.endpoint.services.utilities;

namespace magic.endpoint.tests
{
    public static class Common
    {
        static public Node Evaluate(string hl)
        {
            var services = Initialize();
            var lambda = HyperlambdaParser.Parse(hl);
            var signaler = services.GetService(typeof(ISignaler)) as ISignaler;
            signaler.Signal("eval", lambda);
            return lambda;
        }

        #region [ -- Private helper methods -- ]

        public static IServiceProvider Initialize()
        {
            var services = new ServiceCollection();
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.SetupGet(x => x[It.IsAny<string>()]).Returns("60");
            services.AddTransient((svc) => mockConfiguration.Object);
            services.AddTransient<ISignaler, Signaler>();
            services.AddTransient<IHttpArgumentsHandler, HttpArgumentsHandler>();
            var types = new SignalsProvider(InstantiateAllTypes<ISlot>(services));
            services.AddTransient<ISignalsProvider>((svc) => types);
            services.AddTransient<IHttpExecutorAsync, HttpExecutorAsync>();
            var provider = services.BuildServiceProvider();
            Utilities.RootFolder = AppDomain.CurrentDomain.BaseDirectory;
            return provider;
        }

        static IEnumerable<Type> InstantiateAllTypes<T>(ServiceCollection services) where T : class
        {
            var type = typeof(T);
            var result = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic && !x.FullName.StartsWith("Microsoft", StringComparison.InvariantCulture))
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var idx in result)
            {
                services.AddTransient(idx);
            }
            return result;
        }

        #endregion
    }
}
