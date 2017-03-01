using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DalSoft.RestClient.Handlers
{
    public class FormUrlEncodedHander : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if(!IsFormUrlEncodedContentType(request))
                return null;

            var content = request.GetContent();

            if (content == null)
                return null;

            request.Content = new FormUrlEncodedContent(GetContent(content));

            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>Returns a List KeyValuePair to pass into FormUrlEncodedContent supports complex objects People[0]First=Darran&amp;People[0]Last=Darran</summary>
        private static List<KeyValuePair<string, string>> GetContent(object o, List<KeyValuePair<string, string>> nameValueCollection = null, string prefix = null)
        {
            nameValueCollection = nameValueCollection ?? new List<KeyValuePair<string, string>>();

            foreach (PropertyInfo property in o.GetType().GetProperties())
            {
                var propertyName = prefix == null ? property.Name : $"{prefix}.{property.Name}";
                var propertyValue = property.GetValue(o);

                if (propertyValue == null) continue;

                if (IsValueTypeOrPrimitiveOrStringOrDateTimeOrGuid(property.PropertyType.GetTypeInfo()))
                {
                    nameValueCollection.Add(new KeyValuePair<string, string>(propertyName, propertyValue.ToString()));
                }
                else if (propertyValue is IEnumerable)
                {
                    var enumerable = ((IEnumerable)propertyValue).Cast<object>().ToArray();

                    for (var i = 0; i < enumerable.Count(); i++)
                    {
                        foreach (var propertyItem in enumerable[i].GetType().GetProperties())
                        {
                            var propertyItemName = $"{propertyName}[{i}].{propertyItem.Name}";
                            var propertyItemValue = propertyItem.GetValue(enumerable[i]);

                            if (propertyItemValue == null) continue;

                            if (IsValueTypeOrPrimitiveOrStringOrDateTimeOrGuid(propertyItem.PropertyType.GetTypeInfo()))
                            {
                                nameValueCollection.Add(new KeyValuePair<string, string>(propertyItemName, propertyItemValue.ToString()));
                            }
                            else
                            {
                                GetContent(propertyItemValue, nameValueCollection, propertyItemName);
                            }
                        }
                    }
                }
                else
                {
                    GetContent(property.GetValue(o), nameValueCollection, propertyName);
                }
            }

            return nameValueCollection;
        }

        private static bool IsValueTypeOrPrimitiveOrStringOrDateTimeOrGuid(TypeInfo type)
        {
            return type.IsValueType || type.IsPrimitive || type.IsEnum || type.AsType() == typeof(string) || type.AsType() == typeof(DateTime) || type.AsType() == typeof(Guid);
        }

        private static bool IsFormUrlEncodedContentType(HttpRequestMessage request)
        {
            return request.GetContentType() == "application/x-www-form-urlencoded";
        }
    }
}
