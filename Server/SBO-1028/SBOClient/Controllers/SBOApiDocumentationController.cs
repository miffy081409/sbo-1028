﻿using sbo.fx;
using sbo.fx.Attributes;
using sbo.fx.Models;
using SBOClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace SBOClient.Controllers
{
    //[Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [RoutePrefix("api/sbo-api-docs")]
    public class SBOApiDocumentationController : ApiController
    {
        public SBOApiDocumentationController() : this(GlobalConfiguration.Configuration)
        {

        }

        public SBOApiDocumentationController(HttpConfiguration config)
        {
            Configuration = config;
            Configuration.Services.Replace(typeof(IDocumentationProvider), new XmlDocumentationProvider(HttpContext.Current.Server.MapPath("~/bin/SBOClient.xml")));
        }

        [HttpGet]
        [Route("explore-sbo-apis")]
        public IEnumerable<APIControllerDocumentation> ExploreSBOApis()
        {
            try
            {
                var controllers = new List<APIControllerDocumentation>();

                ILookup<HttpControllerDescriptor, ApiDescription> groups = GetAPIControllers();

                foreach (var group in groups)
                {
                    controllers.Add(new APIControllerDocumentation() { Name = group.Key.ControllerName, Description = GetAPIDocumentation(group.Key), BaseURL = GetRoutePrefix(group.Key), IsSecured = IsSecureController(group.Key) });
                }

                return controllers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        [Route("get-api-detail")]
        public APIControllerDocumentation GetAPIDetail(string name)
        {
            var api = GetAPIControllers().FirstOrDefault(x => x.Key.ControllerName == name);
            var actionList = new List<APIActionDocumentation>();

            foreach (var action in api.ToList())
            {
                var endpoint = new APIActionDocumentation() { Method = action.HttpMethod.Method, Name = action.ActionDescriptor.ActionName, Description = action.Documentation, Path = action.RelativePath };

                if(action.ParameterDescriptions.Count() > 0)
                {
                    var parameters = new List<APIParameterDocumentation>();
                    foreach (var param in action.ParameterDescriptions)
                    {
                        parameters.Add(new APIParameterDocumentation() { Name = param.Name, Type = param.ParameterDescriptor.ParameterType.Name, IsOptional = param.ParameterDescriptor.IsOptional });
                    }

                    endpoint.Parameters = parameters;
                }

                endpoint.Response = new APIResponseDocumentation() { Type = TranslateToSimpleWordsType(action.ResponseDescription.DeclaredType), Description = action.ResponseDescription.Documentation };

                actionList.Add(endpoint);
            }
            

            return new APIControllerDocumentation() { Name = api.Key.ControllerName, Description = GetAPIDocumentation(api.Key), BaseURL = GetRoutePrefix(api.Key), IsSecured = IsSecureController(api.Key), Actions = actionList };
        }

        [HttpGet]
        [Route("get-sbo-models")]
        public IEnumerable<object> GetSBOModels()
        {
            try
            {
                var list = new List<object>();
                var types = Assembly.Load("sbo.fx").GetTypes().Where(x => x.FullName.ToLower().Contains("sbo.fx.models"));

                foreach (var type in types)
                {
                    var sboType = type.GetCustomAttributes(typeof(SBOTransactionTypeAttribute), false).FirstOrDefault() as SBOTransactionTypeAttribute;
                    if (type.Name != "DocumentationModel")
                    {
                        if (!string.IsNullOrEmpty(sboType?.SBOType))
                        {
                            list.Add(new { Name = type.Name, SBOType = sboType?.SBOType, Description = sboType?.Description, IsDetail = sboType?.IsDetail });
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("get-sbo-model-fields")]
        public IEnumerable<object> GetModelFields(string name)
        {
            var instance = (DocumentationModel)Assembly.Load("sbo.fx").CreateInstance("sbo.fx.Models." + name);
            var list = new List<object>();
            var fields = instance.GetObjectDocumentation();

            foreach (var fld in fields)
            {
                list.Add(new { FieldName = fld.GetType().GetProperty("FieldName")?.GetValue(fld, null), Type = fld.GetType().GetProperty("Type")?.GetValue(fld, null), IsRequired = fld.GetType().GetProperty("IsRequired")?.GetValue(fld, null), FieldLength = fld.GetType().GetProperty("FieldLength")?.GetValue(fld, null) });
            }

            return list;
        }

        private ILookup<HttpControllerDescriptor, ApiDescription> GetAPIControllers()
        {
            //filter api here only show sbo api created by chuckie
            return Configuration.Services.GetApiExplorer().ApiDescriptions.ToLookup(api => api.ActionDescriptor.ControllerDescriptor);
        }

        private string GetAPIDocumentation(HttpControllerDescriptor controller)
        {
            return Configuration.Services.GetDocumentationProvider().GetDocumentation(controller);
        }

        private bool IsSecureController(HttpControllerDescriptor controller)
        {
            bool hasAuthorizeAttribute = controller.ControllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false).Any();
            
            if (hasAuthorizeAttribute)
            {
                return true;
            }
            
            return false;
        }

        private string GetRoutePrefix(HttpControllerDescriptor controller)
        {
            RoutePrefixAttribute routePrefix = controller.ControllerType.GetCustomAttributes(typeof(RoutePrefixAttribute), false).FirstOrDefault() as RoutePrefixAttribute;
            if (routePrefix != null)
            {
                return routePrefix.Prefix;
            }

            return "";
        }

        private string TranslateToSimpleWordsType(Type type)
        {
            var result = "";

            if (type.IsGenericType)
            {
                if (type.Name.ToLower().Contains("enumerable") || type.Name.ToLower().Contains("list"))
                {
                    result = $"{type.GetGenericArguments().FirstOrDefault().Name} List";
                }
            }
            else
            {
                result = type.Name;
            }

            return result;
        }
    }
}
