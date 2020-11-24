using System;
using System.IO;
using Newtonsoft.Json;
using WireMock.Server;

namespace Bank.ApiStub
{
    public static class Extensions
    {
        public static void ReadAllMappings(this WireMockServer stubServer)
        {
            var adminFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "__admin");

            foreach (var directory in Directory.EnumerateDirectories(adminFolder))
            {
                stubServer.ReadStaticMappings(directory);

                foreach (var subDirectory in Directory.EnumerateDirectories(directory))
                {
                    stubServer.ReadStaticMappings(subDirectory);
                }
            }
        }

        public static string SerializeToJson<T>(this T input)
        {
            return JsonConvert.SerializeObject(input);
        }

        public static T DeserializeFromJson<T>(this string result)
        {
            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}