using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
        }
    }
}
