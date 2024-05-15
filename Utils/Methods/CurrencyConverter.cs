using System.Xml.Linq;

namespace Retetar.Utils.Methods
{
    public class CurrencyConverter
    {
        private Dictionary<string, double> exchangeRatesCache = new Dictionary<string, double>();
        private Timer timer;

        public CurrencyConverter()
        {
            // Pornim un timer care va actualiza automat ratele de schimb din 10 în 10 minute
            timer = new Timer(UpdateExchangeRates, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }

        public async Task<double> ConvertCurrency(double amount, string baseCurrency)
        {
            try
            {
                // Verificăm dacă avem deja rata de schimb în cache
                if (exchangeRatesCache.ContainsKey(baseCurrency))
                {
                    var baseRate = exchangeRatesCache[baseCurrency];
                    // Calculăm convertirea din moneda de bază în RON
                    var convertedAmount = amount * baseRate;
                    return convertedAmount;
                }

                // Dacă nu există în cache, facem o solicitare către serverul BNR
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://www.bnr.ro/nbrfxrates.xml");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var xmlDoc = XDocument.Parse(content);

                        XNamespace ns = xmlDoc.Root.GetDefaultNamespace();
                        var cubeNode = xmlDoc.Descendants(ns + "Cube").FirstOrDefault();

                        // Obținem rata de schimb de bază (în raport cu RON)
                        var baseRateNode = cubeNode.Elements(ns + "Rate")
                                                    .FirstOrDefault(e => e.Attribute("currency")?.Value == baseCurrency);
                        if (baseRateNode == null)
                        {
                            throw new Exception("Base currency not found in rates.");
                        }
                        var baseRate = double.Parse(baseRateNode.Value);

                        // Salvăm rata de schimb în cache
                        exchangeRatesCache[baseCurrency] = baseRate;

                        // Calculăm convertirea din moneda de bază în RON
                        var convertedAmount = amount * baseRate;
                        return convertedAmount;
                    }
                    else
                    {
                        throw new Exception("Failed to fetch exchange rates from BNR.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to perform currency conversion.", ex);
            }
        }

        private async void UpdateExchangeRates(object state)
        {
            try
            {
                // Facem o solicitare către serverul BNR pentru a actualiza ratele de schimb
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
                    }
                    else
                    {
                        throw new Exception("Failed to fetch exchange rates from BNR.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to update exchange rates: " + ex.Message);
            }
        }
    }
}
