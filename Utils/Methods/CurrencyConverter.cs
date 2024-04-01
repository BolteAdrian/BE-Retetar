using System.Xml.Linq;

namespace Retetar.Utils.Methods
{
    public class CurrencyConverter
    {
        public async Task<double> ConvertCurrency(double amount, string baseCurrency)
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

                        // Obținem rata de schimb de bază (în raport cu RON)
                        var baseRateNode = cubeNode.Elements(ns + "Rate")
                                                    .FirstOrDefault(e => e.Attribute("currency")?.Value == baseCurrency);
                        if (baseRateNode == null)
                        {
                            throw new Exception("Base currency not found in rates.");
                        }
                        var baseRate = double.Parse(baseRateNode.Value);

                        // Calculăm convertirea din moneda de bază în RON
                        var convertedAmount = amount *baseRate;
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
    }
}
