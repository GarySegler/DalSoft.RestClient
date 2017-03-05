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
            if (IsFormUrlEncodedContentType(request))
            {
                var content = request.GetContent();
                request.Content = content == null ? null : new FormUrlEncodedContent(GetContent(content));
            }

            return await base.SendAsync(request, cancellationToken); //next in the pipeline
        }

        /// <summary>Returns a List KeyValuePair to pass into FormUrlEncodedContent supports complex objects People[0]First=Darran&amp;People[0]Last=Darran</summary>
        private static List<KeyValuePair<string, string>> GetContent(object o, List<KeyValuePair<string, string>> nameValueCollection = null, string prefix = null, int recrusions=0)
        {
            const int maxRecrusions = 100;
            recrusions = prefix == null ? 0 : recrusions + 1;
            if (recrusions> maxRecrusions) throw new InvalidOperationException("Object supplied to be UrlEncoded is nested too deeply");

            nameValueCollection = nameValueCollection ?? new List<KeyValuePair<string, string>>();

            foreach (var property in o.GetType().GetProperties())
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

                    for (var i = 0; i < enumerable.Length; i++)
                    {
                        if (IsValueTypeOrPrimitiveOrStringOrDateTimeOrGuid(enumerable[i].GetType().GetTypeInfo()))
                            nameValueCollection.Add(new KeyValuePair<string, string>(propertyName, enumerable[i].ToString()));
                        
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
                                GetContent(propertyItemValue, nameValueCollection, propertyItemName, recrusions);
                            }
                        }
                    }
                }
                else
                {
                    GetContent(property.GetValue(o), nameValueCollection, propertyName, recrusions);
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
