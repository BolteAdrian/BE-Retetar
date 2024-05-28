using System.Xml.Linq;

namespace Retetar.Utils.Methods
{
    public class CurrencyConverter
    {
        private Dictionary<string, double> exchangeRatesCache = new Dictionary<string, double>();
        private DateTime lastSuccessfulUpdate;
        private Timer timer;
        private readonly int retryCount = 3;
        private readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(10); // durata de valabilitate a cache-ului

        public CurrencyConverter()
        {
            // Pornim un timer care va actualiza automat ratele de schimb din 10 în 10 minute
            timer = new Timer(UpdateExchangeRates, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            lastSuccessfulUpdate = DateTime.MinValue; // inițial nu avem nicio actualizare reușită
        }

        public async Task<double> ConvertCurrency(double amount, string baseCurrency)
        {
            try
            {
                // Verificăm dacă avem deja rata de schimb în cache și dacă cache-ul este valid
                if (exchangeRatesCache.ContainsKey(baseCurrency) && (DateTime.Now - lastSuccessfulUpdate) < cacheDuration)
                {
                    var baseRate = exchangeRatesCache[baseCurrency];
                    // Calculăm convertirea din moneda de bază în RON
                    var convertedAmount = amount * baseRate;
                    return convertedAmount;
                }

                // Încearcă să obții ratele de schimb
                await FetchExchangeRates();

                if (exchangeRatesCache.ContainsKey(baseCurrency))
                {
                    var baseRate = exchangeRatesCache[baseCurrency];
                    var convertedAmount = amount * baseRate;
                    return convertedAmount;
                }
                else
                {
                    throw new Exception("Base currency not found in rates after fetching.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to perform currency conversion.", ex);
            }
        }

        private async void UpdateExchangeRates(object state)
        {
            await FetchExchangeRates();
        }

        private async Task FetchExchangeRates()
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync("https://www.bnr.ro/nbrfxrates.xml");

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var xmlDoc = XDocument.Parse(content);

                            XNamespace ns = xmlDoc.Root.GetDefaultNamespace();
                            var cubeNode = xmlDoc.Descendants(ns + "Cube").FirstOrDefault();

                            // Actualizăm cache-ul cu noile rate de schimb
                            foreach (var rateNode in cubeNode.Elements(ns + "Rate"))
                            {
                                var currency = rateNode.Attribute("currency")?.Value;
                                var rate = double.Parse(rateNode.Value);
                                exchangeRatesCache[currency] = rate;
                            }

                            // Actualizăm timpul ultimei actualizări reușite
                            lastSuccessfulUpdate = DateTime.Now;
                            Console.WriteLine("Exchange rates updated successfully.");
                            return;
                        }
                        else
                        {
                            throw new Exception("Failed to fetch exchange rates from BNR.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
                    if (i == retryCount - 1)
                    {
                        Console.WriteLine("All attempts to update exchange rates failed.");
                    }
                }
            }
        }
    }
}
