using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sensu_client.Configuration
{
    public class ChecksConverter : JsonConverter
    {
        protected Check Create(Type objectType, JObject jsonObject)
        {
            var key = jsonObject.Children().First();
            var check = new Check { Name = key.Path };
            return check;
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var check = value as ICheck;
            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();
            writer.WritePropertyName(check.Name);
            writer.WriteStartObject();
            var propertyInfos = value.GetType().GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                writer.WritePropertyName(propertyInfo.Name);
                var propertyValue = propertyInfo.GetValue(value, null);
                serializer.Serialize(writer, propertyValue);
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var list = (Checks)existingValue ?? new Checks();

                var jsonCheck = JObject.Load(reader); 
            
                var target = Create(objectType, jsonCheck);
                var jsonProperties = jsonCheck[target.Name];

                var propertyNames = typeof(Check).GetProperties().ToDictionary(pi => pi.Name, pi => pi);
                foreach (JProperty jsonProperty in jsonProperties)
                {
                    PropertyInfo targetProperty;
                    var property1 = jsonProperty;
                    var property = propertyNames.FirstOrDefault(k => k.Key.ToLower() == property1.Name.ToLower()).Key;

                    if (propertyNames.TryGetValue(property.ToString(), out targetProperty))
                    {
                        var propertyValue = jsonProperty.Value.ToObject(targetProperty.PropertyType);
                        targetProperty.SetValue(target, propertyValue, null);
                    }

                }
                list.Add(target);

            existingValue = (Object) list;

            return existingValue;

        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ICheck));
        }

    }
}