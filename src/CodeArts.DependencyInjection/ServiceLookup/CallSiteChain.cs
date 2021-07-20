// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteChain
    {
        private readonly Dictionary<Type,ChainItemInfo> _callSiteChain;

        public CallSiteChain()
        {
            _callSiteChain = new Dictionary<Type, ChainItemInfo>();
        }

        public void CheckCircularDependency(Type serviceType)
        {
            if (_callSiteChain.ContainsKey(serviceType))
            {
                throw new InvalidOperationException(CreateCircularDependencyExceptionMessage(serviceType));
            }
        }

        public void Remove(Type serviceType)
        {
            _callSiteChain.Remove(serviceType);
        }

        public void Add(Type serviceType, Type implementationType = null)
        {
            _callSiteChain[serviceType] = new ChainItemInfo(_callSiteChain.Count, implementationType);
        }

        private string CreateCircularDependencyExceptionMessage(Type type)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendFormat("A circular dependency was detected for the service of type '{0}'.", type.FullName);
            messageBuilder.AppendLine();

            AppendResolutionPath(messageBuilder, type);

            return messageBuilder.ToString();
        }

        private void AppendResolutionPath(StringBuilder builder, Type currentlyResolving = null)
        {
            foreach (var pair in _callSiteChain.OrderBy(p => p.Value.Order))
            {
                var serviceType = pair.Key;
                var implementationType = pair.Value.ImplementationType;
                if (implementationType == null || serviceType == implementationType)
                {
                    builder.Append(serviceType.FullName);
                }
                else
                {
                    builder.AppendFormat("{0}({1})",
                        serviceType.FullName,
                        implementationType.FullName);
                }

                builder.Append(" -> ");
            }

            builder.Append(currentlyResolving?.FullName);
        }

        private struct ChainItemInfo
        {
            public int Order { get; }
            public Type ImplementationType { get; }

            public ChainItemInfo(int order, Type implementationType)
            {
                Order = order;
                ImplementationType = implementationType;
            }
        }
    }
}
