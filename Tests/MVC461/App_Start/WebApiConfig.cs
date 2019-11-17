﻿using SkyBuilding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Mvc461
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            // Web API 路由
            config.MapHttpAttributeRoutes();

            var typeStore = RuntimeTypeCache.Instance.GetCache(config.GetType());

            var propertyStore = typeStore.PropertyStores.First(x => x.Name == "Services");

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
