using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Davinci.net
{
    public static class Extensions
    {
        public static async Task<IEnumerable<T>> ExecuteAsync<T>(this IEnumerable<Task<T>> tasks)
        {
            var results = new List<T>();
            foreach (var task in tasks)
            {
                results.Add(await task);

            }
            return results; 
        }
    }
}