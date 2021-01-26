using System;

namespace PDFParse
{
    public class Product
    {
        private string groupId;
        private string productId;
        private string description;
        private string price;
        public string groupDescription;

        public string FormattedText => $"{groupId}|{productId}|{groupDescription}|{description}|{price}";

        public Product(string priceText)
        {
            ParsePriceString(priceText);
        }

        public Product(string priceText, string description)
        {
            this.description = description;
            ParsePriceString(priceText);
        }

        private void ParsePriceString(string priceText)
        {
            var words = priceText.Split(' ');

            groupId = words[0];
            productId = words[1];

            for (var i = 0; i < ProductPriceParser.PriceSymbols.Length; i++)
            {
                if(priceText.Contains(ProductPriceParser.PriceSymbols[i]))
                {
                    price = priceText.Substring(priceText.IndexOf(ProductPriceParser.PriceSymbols[i], StringComparison.Ordinal));
                    break;
                }
            }

            if (string.IsNullOrEmpty(description))
            {
                int descriptionStartIndex =
                    priceText.LastIndexOf(words[1], StringComparison.Ordinal) + words[1].Length;
                description = priceText.Substring(descriptionStartIndex,
                    priceText.Length - descriptionStartIndex - price.Length).Trim();
            }
        }
    }
}
