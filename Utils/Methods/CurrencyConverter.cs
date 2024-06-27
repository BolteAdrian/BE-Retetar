using System.Xml.Linq;

public class CurrencyConverter
{
    private Dictionary<string, double> exchangeRatesCache = new Dictionary<string, double>();
    private DateTime lastSuccessfulUpdate;
    private readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(10); // cache validity duration
    private readonly HttpClient httpClient = new HttpClient();

    public CurrencyConverter()
    {
        // Immediate update of exchange rates
        _ = FetchExchangeRates();
        lastSuccessfulUpdate = DateTime.MinValue;
    }

    public async Task<double> ConvertCurrency(double amount, string baseCurrency)
    {
        try
        {
            // Check if the exchange rate is already in cache and if the cache is valid
            if (exchangeRatesCache.ContainsKey(baseCurrency) && (DateTime.Now - lastSuccessfulUpdate) < cacheDuration)
            {
                return amount * exchangeRatesCache[baseCurrency];
            }

            // Update exchange rates if the cache has expired
            await FetchExchangeRates();

            if (exchangeRatesCache.ContainsKey(baseCurrency))
            {
                return amount * exchangeRatesCache[baseCurrency];
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

    private async Task FetchExchangeRates()
    {
        try
        {
            var response = await httpClient.GetAsync("https://www.bnr.ro/nbrfxrates.xml");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var xmlDoc = XDocument.Parse(content);

                XNamespace ns = xmlDoc.Root.GetDefaultNamespace();
                var cubeNode = xmlDoc.Descendants(ns + "Cube").FirstOrDefault();

                exchangeRatesCache.Clear();
                foreach (var rateNode in cubeNode.Elements(ns + "Rate"))
                {
                    var currency = rateNode.Attribute("currency")?.Value;
                    var rate = double.Parse(rateNode.Value);
                    exchangeRatesCache[currency] = rate;
                }

                lastSuccessfulUpdate = DateTime.Now;
                Console.WriteLine("Exchange rates updated successfully.");
            }
            else
            {
                throw new Exception("Failed to fetch exchange rates from BNR.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching exchange rates: {ex.Message}");
        }
    }
}
