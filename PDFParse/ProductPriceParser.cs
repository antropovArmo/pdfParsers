using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PDFParse
{
    internal class ProductPriceParser
    {
        //, @"[A-Z]{3}\d{2}",@"[A-Z]{2}\d{3}"
        internal static readonly string[] regExIdentifiers =
        {
            @"\d{8}",@"\d{10}", @"[A-Z]\d{9}", @"[A-Z]{2}-[A-Z]{2}-[A-Z]{2}-\d{4}", @"\d{3}[A-Z]{3}\d{2}", @"[A-Z]{8}\d{3}[A-Z]{2}", @"[A-Z]{5}\d{2}",
            @"[A-Z]{8}\d{2}", @"\d{4}[A-Z]{4}", @"[A-Z]{6}\d[A-Z]", @"[A-Z]{2}-[A-Z]{2}-[A-Z]{3}", @"[A-Z]{14}", @"[A-Z]{10}"
        };

        internal static readonly string[] PriceSymbols = {"€", "POA", "FOC"};

        // 3
        internal static readonly string[]
            GroupedDescriptionPrefixesTriplet = { "Left cabinet", "Right cabinet", "Centre cabinet", "Service gate"};

        // 2
        internal static readonly string[]
            GroupedDescriptionPrefixesDouplet = {"Combi LW", "Combi RW"};

        // 4
        internal static readonly string[]
            GroupedDescriptionPrefixesQuadlet =
            {
                "Service gate Combi LW", "Service gate Combi RW", "Service gate dummy cabinet", "Service gate", "1448mm long (NC) cabinet left hand",
                "1448mm long (NC) cabinet right hand", "1932mm long (NO) cabinet left hand", "1932mm long (NO) cabinet right hand"
            };
        //, "1448mm long", "1932mm long"
        internal static readonly string[]
            GroupedDescriptionPrefixesFifthlet =
                {"Left cabinet", "Right cabinet", "Centre cabinet", "Combi LW", "Combi RW"};

        internal static readonly string[]
            GroupedDescriptionPrefixesNlet =
            {
                "Plain", "With pictogram", "With one pictogram", "With two pictograms", "with LED way mode indicator",
                "with CRI03 Proximity reader integration"
            };

        internal static readonly string[] MiddleWords = {"Electromechanical"};

        internal static readonly string[] DontAddLinesContatining =
        {
            "Select suitable", "reader integration", "long cabinets and standard walkway or wide walkways",
            "finish with stainless ", "Stainless steel", "End User Price", "CRI03", "(full)", "there is no passage", "Combi"
        };

        internal static readonly string[] DontAddWordsInGroup =
            {"RW", "LW", "Right Wide Combi", "Left Wide Combi", "Left Wide", "Centre Wide", "Right Wide", "Centre"};

        private static bool flag;

        internal string ParsePageText(string pageText)
        {
            var lines = pageText.Split('\n');

            return ParseLines(lines);
        }

        internal string UpdateCostsOnPage(string pageText, float modifier)
        {
            var lines = pageText.Split('\n');

            return UpdateCostsOnLines(lines, modifier, true);
        }

        private string ParseLines(string[] lines)
        {
            var text = new StringBuilder();

            for (var i = 0; i < lines.Length; i++)
            {
                if (!flag && (lines[i].Contains("201 - GAO")
                              || i + 1 < lines.Length && lines[i + 1].Contains("201 - GAO")))
                flag = true;

                if (flag)
                    text.Append(ParseLine(lines, ref i));
            }

            return text.ToString();
        }

        public string UpdateCostsOnLines(string[] lines, float modifier, bool newLines)
        {
            var text = new StringBuilder();

            for (var i = 0; i < lines.Length; i++)
                /*if (!flag && (lines[i].Contains("5004000050")
                                  || (i + 1 < lines.Length && lines[i + 1].Contains("5004000050"))))
                    {
                        flag = true;
                    }
    
                    if (flag)*/
                text.Append(UpdateCostsOnLine(lines[i], modifier, newLines));

            return text.ToString();
        }

        private string UpdateCostsOnLine(string line, float modifier, bool newLines)
        {
            var text = new StringBuilder();

            try
            {
                if (line.Contains("€"))
                {
                    var priceSymbolPosition = line.IndexOf("€", StringComparison.Ordinal);
                    var endPricePosition = priceSymbolPosition;
                    var temp = line.Substring(priceSymbolPosition + 1);

                    var cost = "";

                    foreach (var c in temp.ToCharArray())
                        if (char.IsDigit(c) || c == ' ' || c == ',')
                        {
                            cost += c;
                            endPricePosition++;
                        }
                        else
                        {
                            break;
                        }

                    if (cost.Contains(",") && cost.IndexOf(",") < cost.Length - 2)
                        cost = cost.Replace(",", "");

                    var costValue = 0f;

                    if (float.TryParse(cost.Replace(",", ""), out costValue))
                    {
                        costValue *= modifier;
                        var newLine =
                            $"{line.Substring(0, priceSymbolPosition + 1)} {costValue:F0} {line.Substring(endPricePosition + 1)}";
                        //costValue:F1
                        if (newLines)
                            text.AppendLine(newLine);
                        else
                            text.Append(newLine);
                    }
                    else
                    {
                        if (newLines)
                            text.AppendLine(line);
                        else
                            text.Append(line);
                    }
                }
                else
                {
                    if (newLines)
                        text.AppendLine(line);
                    else
                        text.Append(line);
                }
            }
            catch (Exception ex)
            {
                text.AppendLine("ERROR! " + ex.Message + ": " + line);
            }

            return text.ToString();
        }

        private string ParseLine(string[] lines, ref int i)
        {
            var text = new StringBuilder();

            try
            {
                for (var j = 0; j < PriceSymbols.Length; j++)
                    if (lines[i].Contains(PriceSymbols[j]))
                    {
                        if (lines[i].Length > lines[i].IndexOf(PriceSymbols[j]) + PriceSymbols[j].Length &&
                            lines[i][lines[i].IndexOf(PriceSymbols[j]) + PriceSymbols[j].Length] == ')')
                            continue;

                        var products = CreateProduct(lines, ref i, PriceSymbols[j]);

                        if (products.Count == 0)
                            text.AppendLine("NOT PARSED: " + lines[i]);

                        foreach (var product in products)
                            text.AppendLine(product.FormattedText);

                        break;
                    }
            }
            catch (Exception ex)
            {
                text.AppendLine("ERROR! " + ex.Message + ": " + lines[i]);
            }

            return text.ToString();
        }

        private List<Product> CreateProduct(string[] lines, ref int i, string priceSymbol)
        {
            var products = new List<Product>();

            if (GroupedDescriptionPrefixesTriplet.Any(lines[i].Contains)
                || GroupedDescriptionPrefixesTriplet.Any(lines[i + 1].Contains)
                || GroupedDescriptionPrefixesDouplet.Any(lines[i].Contains)
                || GroupedDescriptionPrefixesDouplet.Any(lines[i + 1].Contains)
                || GroupedDescriptionPrefixesNlet.Any(lines[i].Contains)
                || GroupedDescriptionPrefixesNlet.Any(lines[i + 1].Contains)
                || GroupedDescriptionPrefixesQuadlet.Any(lines[i].Contains)
                || GroupedDescriptionPrefixesQuadlet.Any(lines[i + 1].Contains))
            {
                products = CreateProductsGroup(lines, ref i, priceSymbol);
            }
            else
            {
                var product = CreateProductSingle(lines, ref i, priceSymbol);

                if (product != null)
                    products.Add(product);
            }

            return products;
        }

        private List<Product> CreateProductsGroup(string[] lines, ref int i, string priceSymbol)
        {
            var products = new List<Product>();

            if (GroupedDescriptionPrefixesNlet.Any(lines[i].Contains)
                || GroupedDescriptionPrefixesNlet.Any(lines[i + 1].Contains))
                products = CreateProductsGroupNlet(lines, ref i, priceSymbol);
            else if (IsItFifthlet(lines, i, priceSymbol))
                products = CreateProductsGroupFifthlet(lines, ref i, priceSymbol);
            else if (GroupedDescriptionPrefixesQuadlet.Any(lines[i].Contains)
                     || GroupedDescriptionPrefixesQuadlet.Any(lines[i + 1].Contains))
                products = CreateProductsGroupQuadlet(lines, ref i, priceSymbol);
            else if (GroupedDescriptionPrefixesTriplet.Any(lines[i].Contains)
                     || GroupedDescriptionPrefixesTriplet.Any(lines[i + 1].Contains))
                products = CreateProductsGroupTriplet(lines, ref i, priceSymbol);
            else if (GroupedDescriptionPrefixesDouplet.Any(lines[i].Contains)
                     || GroupedDescriptionPrefixesDouplet.Any(lines[i + 1].Contains))
                // douplet
                products = CreateProductsGroupDouplet(lines, ref i, priceSymbol);

            return products;
        }

        private bool IsItFifthlet(string[] lines, int i, string priceSymbol)
        {
            var counterIdentifiers = 0;
            // check until second price line - if there is only prices, triplet words and identifiers => fifthlet
            for (var j = i; j < lines.Length; j++)
            {
                if (LineContainsIdentifier(lines[j]))
                    counterIdentifiers++;

                var words = lines[j].Split(' ');

                var first = IsWordIdentifier(words[0]) ? words[0] : "";
                var second = words.Length > 1 ? IsWordIdentifier(words[1]) ? words[1] : "" : "";
                var price = lines[j].Contains(priceSymbol)
                    ? lines[j].Substring(lines[j].LastIndexOf(priceSymbol, StringComparison.Ordinal))
                    : "";
                var lineWords = "";

                for (var k = 0; k < GroupedDescriptionPrefixesTriplet.Length; k++)
                    if (lines[j].Contains(GroupedDescriptionPrefixesTriplet[k]))
                    {
                        lineWords = GroupedDescriptionPrefixesTriplet[k];
                        break;
                    }

                var tempLine = lines[j].Replace(" - Fail safe", "");

                foreach (var word in DontAddWordsInGroup) tempLine = tempLine.Replace(word, "");

                tempLine = tempLine.Trim();

                var totalLength = tempLine.Length - price.Length - lineWords.Length - first.Length -
                                  second.Length;

                /*lines[j].Replace(price, "").Substring(second != "" ? 
                    (lines[j].LastIndexOf(second) + second.Length) : (lines[j].IndexOf(first) + first.Length)
                    );*/

                if (totalLength > 3 && tempLine.Split(' ').Length > 1) return false;

                // check if identifiers meet twice => return true
                if (counterIdentifiers >= 2)
                    break;
            }

            return true;
        }

        private bool NLetEnded(string[] lines, int i)
        {
            for (var j = i; j < lines.Length; j++)
                if (PriceSymbols.Any(lines.Contains) || LineContainsIdentifier(lines[j]))
                {
                    if (lines[j].Contains(GroupedDescriptionPrefixesNlet[0]))
                        return true;
                    if (GroupedDescriptionPrefixesNlet.Any(lines[j].Contains)) return false;
                }

            return true;
        }

        private List<Product> CreateProductsGroupNlet(string[] lines, ref int i, string priceSymbol)
        {
            var currentGroup = "";

            var products = new List<Product>();
            var firstRow = true;

            while (!NLetEnded(lines, i) || firstRow)
            {
                for (var j = 0; j < GroupedDescriptionPrefixesNlet.Length; j++)
                    if (lines[i].Contains(GroupedDescriptionPrefixesNlet[j]) ||
                        lines[i + 1].Contains(GroupedDescriptionPrefixesNlet[j]) &&
                        CountIdentifiersOnLine(lines[i]) != 2 && LineContainsIdentifier(lines[i + 1]))
                    {
                        var words = lines[i].Split(' ');

                        Product product = null;
                        // triplet                        
                        if (LineContainsIdentifier(lines[i]))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < words.Length &&
                                   !IsWordIdentifier(words[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = words[identifierWordsShift];

                            var secondIdentifier = "";
                            var secondWordOnThisLine = false;

                            if (IsWordIdentifier(words[1 + identifierWordsShift]))
                            {
                                secondIdentifier = words[1 + identifierWordsShift];
                                secondWordOnThisLine = true;
                            }
                            else if (IsWordIdentifier(lines[i + 1].Split(' ')[0]))
                            {
                                secondIdentifier = lines[i + 1].Split(' ')[0];
                            }

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = 0;
                            var tempDescription = "";

                            if (lines[i].Contains(GroupedDescriptionPrefixesNlet[j]))
                            {
                                descriptionStartIndex = lines[i].IndexOf(GroupedDescriptionPrefixesNlet[j],
                                    StringComparison.Ordinal);
                                tempDescription = lines[i].Substring(descriptionStartIndex);
                                tempDescription =
                                    tempDescription.Substring(0, tempDescription.Length - price.Length);

                                var addToGroup = lines[i].Substring(
                                                     lines[i].LastIndexOf(secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier)
                                                     + (secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier).Length
                                                     , lines[i].Length - (secondWordOnThisLine ? lines[i].LastIndexOf(secondIdentifier) + secondIdentifier.Length : lines[i].IndexOf(firstIdentifier) + firstIdentifier.Length) - price.Length - tempDescription.Length - 1) +
                                                 " ";

                                if (!NLetEnded(lines, i + 1))
                                {
                                    var shift = 1;

                                    if (!secondWordOnThisLine) shift++;

                                    while (i + shift < lines.Length)
                                    {
                                        if ((LineContainsIdentifier(lines[i + shift]) && !lines[i+shift].Contains("CRI")) ||
                                            PriceSymbols.Any(lines[i + shift].Contains))
                                            break;

                                        if (!DontAddLinesContatining.Any(lines[i + shift].Contains)
                                            && !GroupedDescriptionPrefixesNlet.Any(lines[i + shift].Contains)
                                            && i + shift + 1 < lines.Length &&
                                            !(lines[i + shift + 1]
                                                  .Contains(GroupedDescriptionPrefixesNlet[0]) &&
                                              PriceSymbols.Any(lines[i + shift + 1].Contains)))
                                            addToGroup += lines[i + shift];
                                        shift++;
                                    }

                                    i = i + shift - 1;
                                }

                                currentGroup += addToGroup;
                            }
                            else if (lines[i + 1].Contains(GroupedDescriptionPrefixesNlet[j]))
                            {
                                descriptionStartIndex = lines[i + 1].IndexOf(GroupedDescriptionPrefixesNlet[j],
                                    StringComparison.Ordinal);
                                tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                                var addToGroup = lines[i + 1].Substring(
                                                     lines[i + 1].LastIndexOf(!secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier)
                                                     + (!secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier).Length
                                                     , lines[i + 1].Length - (!secondWordOnThisLine ? lines[i + 1].LastIndexOf(secondIdentifier) + secondIdentifier.Length : 0) - tempDescription.Length - 1) +
                                                 " ";

                                var shift = 1;

                                if (!secondWordOnThisLine) shift++;

                                while (i + shift < lines.Length)
                                {
                                    if ((LineContainsIdentifier(lines[i + shift]) && !lines[i + shift].Contains("CRI")) || PriceSymbols.Any(
                                                                                     lines[i + shift].Contains)
                                                                                 || DontAddLinesContatining.Any(
                                                                                     lines[i + shift].Contains))
                                        break;

                                    if (!GroupedDescriptionPrefixesNlet.Any(lines[i + shift].Contains)
                                        && i + shift + 1 < lines.Length && !lines[i + shift + 1]
                                            .Contains(GroupedDescriptionPrefixesNlet[0]))
                                        addToGroup += lines[i + shift];
                                    shift++;
                                }

                                currentGroup += addToGroup;
                                i = i + shift - 2;
                            }

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");

                            if (!secondWordOnThisLine && !NLetEnded(lines, i) ||
                                firstRow && CountIdentifiersOnLine(lines[i]) == 1)
                                i++;
                        }
                        else if (PriceSymbols.Any(lines[i].Contains))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));
                            var addToGroup = lines[i].Substring(0, lines[i].Length - price.Length);

                            var shift = 2;

                            while (i + shift < lines.Length)
                            {
                                if ((LineContainsIdentifier(lines[i + shift]) && !lines[i + shift].Contains("CRI")) || PriceSymbols.Any(
                                                                                                                        lines[i + shift].Contains)
                                                                                                                    || DontAddLinesContatining.Any(
                                                                                                                        lines[i + shift].Contains))
                                    break;

                                if (!GroupedDescriptionPrefixesNlet.Any(lines[i + shift].Contains)
                                    && i + shift + 1 < lines.Length && !lines[i + shift + 1]
                                        .Contains(GroupedDescriptionPrefixesNlet[0]))
                                    addToGroup += lines[i + shift];
                                shift++;
                            }

                            currentGroup += addToGroup;

                            var wordsNextLine = lines[i + 1].Split(' ');

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < wordsNextLine.Length &&
                                   !IsWordIdentifier(wordsNextLine[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = wordsNextLine[identifierWordsShift];

                            var secondIdentifier = "";

                            if (IsWordIdentifier(wordsNextLine[1 + identifierWordsShift]))
                                secondIdentifier = wordsNextLine[1 + identifierWordsShift];
                            else if (IsWordIdentifier(lines[i + 2].Split(' ')[0]))
                                secondIdentifier = lines[i + 2].Split(' ')[0];

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = lines[i + 1].IndexOf(GroupedDescriptionPrefixesNlet[j],
                                StringComparison.Ordinal);
                            var tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                            if (lines[i].Length == price.Length)
                                currentGroup += lines[i + 1].Substring(
                                    lines[i + 1].LastIndexOf(wordsNextLine[1 + identifierWordsShift]) +
                                    wordsNextLine[1 + identifierWordsShift].Length
                                    , lines[i + 1].Length - tempDescription.Length - tempPrice.Length + price.Length);

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");
                            
                            if (!NLetEnded(lines, i) || firstRow)
                                i++;
                        }

                        if (product != null)
                            products.Add(product);

                        break;
                    }

                if (!NLetEnded(lines, i + 1) || firstRow)
                    i++;
                else if (!firstRow)
                    break;

                firstRow = false;
            }

            foreach (var pr in products)
                pr.groupDescription = currentGroup.Trim();

            return products;
        }

        private List<Product> CreateProductsGroupFifthlet(string[] lines, ref int i, string priceSymbol)
        {
            var currentGroup = "";

            var products = new List<Product>();

            for (var k = 0; k < 5;)
            {
                for (var j = 0; j < GroupedDescriptionPrefixesFifthlet.Length; j++)
                    if (lines[i].Contains(GroupedDescriptionPrefixesFifthlet[j]) ||
                        lines[i + 1].Contains(GroupedDescriptionPrefixesFifthlet[j]))
                    {
                        if (lines[i].Contains("61450655") || lines[i + 1].Contains("61450655"))
                            Console.WriteLine("1");

                        var words = lines[i].Split(' ');

                        Product product = null;

                        if (LineContainsIdentifier(lines[i]))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));

                            var identifiersOnLine = CountIdentifiersOnLine(lines[i]);
                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < words.Length &&
                                   !IsWordIdentifier(words[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = words[identifierWordsShift];

                            var secondIdentifier = "";
                            var secondWordOnThisLine = false;

                            if (identifiersOnLine == 2)
                            {
                                var identifier2WordsShift = 1;

                                while (identifierWordsShift + identifier2WordsShift < words.Length &&
                                       !IsWordIdentifier(words[identifier2WordsShift + identifierWordsShift]))
                                    identifier2WordsShift++;

                                secondIdentifier = words[identifier2WordsShift + identifierWordsShift];
                                secondWordOnThisLine = true;
                            }
                            else if (identifiersOnLine == 1)
                            {
                                var wordsNextLine = lines[i + 1].Split(' ');

                                var identifier2WordsShift = 0;

                                while (identifier2WordsShift < wordsNextLine.Length &&
                                       !IsWordIdentifier(wordsNextLine[identifier2WordsShift]))
                                    identifier2WordsShift++;

                                secondIdentifier = wordsNextLine[identifier2WordsShift];
                            }

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = 0;
                            var tempDescription = "";
                            var descriptionOnFirstLine = false;

                            if (lines[i].Contains(GroupedDescriptionPrefixesFifthlet[k]))
                            {
                                descriptionStartIndex = lines[i].IndexOf(GroupedDescriptionPrefixesFifthlet[k],
                                    StringComparison.Ordinal);
                                tempDescription = lines[i].Substring(descriptionStartIndex);
                                tempDescription =
                                    tempDescription.Substring(0, tempDescription.Length - price.Length);
                                descriptionOnFirstLine = true;
                            }
                            else if (lines[i + 1].Contains(GroupedDescriptionPrefixesFifthlet[k]))
                            {
                                descriptionStartIndex = lines[i + 1]
                                    .IndexOf(GroupedDescriptionPrefixesFifthlet[k], StringComparison.Ordinal);
                                tempDescription = lines[i + 1].Substring(descriptionStartIndex);
                            }

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");

                            if (GroupedDescriptionPrefixesDouplet.Any(GroupedDescriptionPrefixesFifthlet[k]
                                .Contains))
                            {
                                var shift = -1;

                                var addToGroup = "";

                                while (i + shift >= 0)
                                {
                                    if (LineContainsIdentifier(lines[i + shift]) ||
                                        PriceSymbols.Any(lines[i + shift].Contains))
                                        break;

                                    if (!DontAddLinesContatining.Any(lines[i + shift].Contains)
                                        && !GroupedDescriptionPrefixesFifthlet.Any(lines[i + shift].Contains))
                                        addToGroup += lines[i + shift];
                                    shift--;
                                }

                                currentGroup += addToGroup;
                            }
                            else
                            {
                                var addToGroup = lines[i].Substring(
                                    lines[i].LastIndexOf(secondWordOnThisLine
                                        ? secondIdentifier
                                        : firstIdentifier)
                                    + (secondWordOnThisLine
                                        ? secondIdentifier
                                        : firstIdentifier).Length
                                    , lines[i].Length -
                                      (secondWordOnThisLine ? secondIdentifier.Length : 0) -
                                      firstIdentifier.Length - price.Length -
                                      (descriptionOnFirstLine ? tempDescription.Length : 0) -
                                      1) + " ";

                                if (addToGroup.Trim() == "")
                                {
                                    var shift = 1;

                                    if (!secondWordOnThisLine) shift++;

                                    if (i + shift < lines.Length && !LineContainsIdentifier(lines[i + shift]) &&
                                        !PriceSymbols.Any(lines[i + shift].Contains))
                                        addToGroup = lines[i + shift];
                                }

                                currentGroup += addToGroup;
                            }

                            if (!secondWordOnThisLine && k < 4)
                                i++;
                        }
                        else if (PriceSymbols.Any(lines[i].Contains))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));
                            currentGroup += lines[i].Substring(0, lines[i].Length - price.Length);

                            var wordsNextLine = lines[i + 1].Split(' ');

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < wordsNextLine.Length &&
                                   !IsWordIdentifier(wordsNextLine[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = wordsNextLine[identifierWordsShift];

                            var secondIdentifier = "";

                            if (IsWordIdentifier(wordsNextLine[1 + identifierWordsShift]))
                                secondIdentifier = wordsNextLine[1 + identifierWordsShift];
                            else if (IsWordIdentifier(lines[i + 2].Split(' ')[0]))
                                secondIdentifier = lines[i + 2].Split(' ')[0];

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = lines[i + 1]
                                .IndexOf(GroupedDescriptionPrefixesFifthlet[k], StringComparison.Ordinal);
                            var tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                            if (lines[i].Length == price.Length)
                            {
                                if (GroupedDescriptionPrefixesDouplet.Any(GroupedDescriptionPrefixesFifthlet[k]
                                    .Contains))
                                {
                                    var shift = -1;

                                    var addToGroup = "";

                                    while (i + shift >= 0)
                                    {
                                        if (LineContainsIdentifier(lines[i + shift]) ||
                                            PriceSymbols.Any(lines[i + shift].Contains))
                                            break;

                                        if (!DontAddLinesContatining.Any(lines[i + shift].Contains)
                                            && !GroupedDescriptionPrefixesFifthlet.Any(
                                                lines[i + shift].Contains))
                                            addToGroup += lines[i + shift];
                                        shift--;
                                    }

                                    currentGroup += addToGroup;
                                }
                                else
                                {
                                    var addToGroup = lines[i + 1].Substring(
                                        lines[i + 1].LastIndexOf(wordsNextLine[1 + identifierWordsShift]) +
                                        wordsNextLine[1 + identifierWordsShift].Length
                                        , lines[i + 1].Length - tempDescription.Length - tempPrice.Length + price.Length);

                                    if (addToGroup.Trim() == "")
                                    {
                                        var groupLineShift = 2;
                                        while (i + groupLineShift < lines.Length &&
                                               DontAddLinesContatining.Any(lines[i + groupLineShift].Contains))
                                            groupLineShift++;

                                        if (!LineContainsIdentifier(lines[i + groupLineShift]))
                                            addToGroup = lines[i + groupLineShift];
                                    }

                                    currentGroup += addToGroup;
                                }
                            }

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");

                            if (k < 4)
                                i++;
                        }
                        else
                        {
                            k--;
                        }

                        if (product != null)
                            products.Add(product);

                        k++;
                        break;
                    }

                if (k < 5)
                    i++;
            }

            foreach (var word in DontAddWordsInGroup) currentGroup = currentGroup.Replace(word, "");

            foreach (var pr in products)
                pr.groupDescription = currentGroup.Trim();

            return products;
        }

        private List<Product> CreateProductsGroupQuadlet(string[] lines, ref int i, string priceSymbol)
        {
            var currentGroup = "";

            var products = new List<Product>();

            for (var k = 0; k < 4;)
            {
                for (var j = 0; j < GroupedDescriptionPrefixesQuadlet.Length; j++)
                    if (lines[i].Contains(GroupedDescriptionPrefixesQuadlet[j]) ||
                        lines[i + 1].Contains(GroupedDescriptionPrefixesQuadlet[j]))
                    {
                        if (lines[i].Contains("SSPNWNCRC01") || lines[i + 1].Contains("SSPNWNCRC01"))
                            Console.WriteLine("1");

                        var words = lines[i].Split(' ');

                        Product product = null;
                        // triplet                        
                        if (LineContainsIdentifier(lines[i]))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < words.Length &&
                                   !IsWordIdentifier(words[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = words[identifierWordsShift];

                            var secondIdentifier = "";
                            var secondWordOnThisLine = false;

                            if (IsWordIdentifier(words[1 + identifierWordsShift]))
                            {
                                secondIdentifier = words[1 + identifierWordsShift];
                                secondWordOnThisLine = true;
                            }
                            else if (IsWordIdentifier(lines[i + 1].Split(' ')[0]))
                            {
                                secondIdentifier = lines[i + 1].Split(' ')[0];
                            }

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = 0;
                            var tempDescription = "";

                            if (lines[i].Contains(GroupedDescriptionPrefixesQuadlet[j]))
                            {
                                descriptionStartIndex = lines[i].IndexOf(GroupedDescriptionPrefixesQuadlet[j],
                                    StringComparison.Ordinal);
                                tempDescription = lines[i].Substring(descriptionStartIndex);
                                tempDescription =
                                    tempDescription.Substring(0, tempDescription.Length - price.Length);

                                var addToGroup = lines[i].Substring(
                                                     lines[i].LastIndexOf(secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier)
                                                     + (secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier).Length
                                                     , lines[i].Length - (secondWordOnThisLine ? lines[i].LastIndexOf(secondIdentifier) + secondIdentifier.Length : lines[i].IndexOf(firstIdentifier) + firstIdentifier.Length) - price.Length - tempDescription.Length - 1) +
                                                 " ";

                                var shift = 1;

                                //if (string.IsNullOrEmpty(addToGroup.Trim()) && lines.Length > i + 1)
                                //{
                                //    if (!LineContainsIdentifier(lines[i + 1]) && !PriceSymbols.Any(lines[i + shift].Contains))
                                //    {
                                //        addToGroup = lines[i + 1];
                                //        shift++;
                                //    }
                                //}

                                if (!secondWordOnThisLine) shift++;

                                while (i + shift < lines.Length)
                                {
                                    if (LineContainsIdentifier(lines[i + shift]) ||
                                        PriceSymbols.Any(lines[i + shift].Contains))
                                        break;

                                    if (!DontAddLinesContatining.Any(lines[i + shift].Contains)
                                        && !GroupedDescriptionPrefixesQuadlet.Any(lines[i + shift].Contains)
                                        && i + shift + 1 < lines.Length &&
                                        !(lines[i + shift + 1].Contains(GroupedDescriptionPrefixesQuadlet[0])
                                          && PriceSymbols.Any(lines[i + shift + 1].Contains)))
                                        addToGroup += lines[i + shift];
                                    shift++;
                                }

                                currentGroup += addToGroup;
                            }
                            else if (lines[i + 1].Contains(GroupedDescriptionPrefixesQuadlet[j]))
                            {
                                descriptionStartIndex = lines[i + 1]
                                    .IndexOf(GroupedDescriptionPrefixesQuadlet[j], StringComparison.Ordinal);
                                tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                                var addToGroup = lines[i+1].Substring(
                                                     lines[i+1].LastIndexOf(!secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier)
                                                     + (!secondWordOnThisLine
                                                         ? secondIdentifier
                                                         : firstIdentifier).Length
                                                     , lines[i+1].Length - (secondWordOnThisLine ? lines[i+1].LastIndexOf(secondIdentifier) + secondIdentifier.Length : lines[i+1].IndexOf(firstIdentifier) + firstIdentifier.Length) - tempDescription.Length - 1) +
                                                 " ";

                                var shift = 2;

                                while (i + shift < lines.Length)
                                {
                                    if (LineContainsIdentifier(lines[i + shift]) ||
                                        PriceSymbols.Any(lines[i + shift].Contains))
                                        break;

                                    if (!DontAddLinesContatining.Any(lines[i + shift].Contains)
                                        && !GroupedDescriptionPrefixesQuadlet.Any(lines[i + shift].Contains)
                                        && i + shift + 1 < lines.Length &&
                                        !(lines[i + shift + 1].Contains(GroupedDescriptionPrefixesQuadlet[0])
                                          && PriceSymbols.Any(lines[i + shift + 1].Contains)))
                                        addToGroup += lines[i + shift];
                                    shift++;
                                }

                                currentGroup += addToGroup;
                            }

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");


                            if (!secondWordOnThisLine && k < 3)
                                i++;
                        }
                        else if (PriceSymbols.Any(lines[i].Contains))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));
                            var addToGroup = lines[i].Substring(0, lines[i].Length - price.Length);

                            var shift = 2;

                            while (i + shift < lines.Length)
                            {
                                if (LineContainsIdentifier(lines[i + shift]) ||
                                    PriceSymbols.Any(lines[i + shift].Contains))
                                    break;

                                if (!DontAddLinesContatining.Any(lines[i + shift].Contains)
                                    && !GroupedDescriptionPrefixesQuadlet.Any(lines[i + shift].Contains)
                                    && i + shift + 1 < lines.Length &&
                                    !(lines[i + shift + 1].Contains(GroupedDescriptionPrefixesQuadlet[0])
                                      && PriceSymbols.Any(lines[i + shift + 1].Contains)))
                                    addToGroup += lines[i + shift];
                                shift++;
                            }

                            currentGroup += addToGroup;

                            var wordsNextLine = lines[i + 1].Split(' ');

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < wordsNextLine.Length &&
                                   !IsWordIdentifier(wordsNextLine[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = wordsNextLine[identifierWordsShift];

                            var secondIdentifier = "";

                            if (IsWordIdentifier(wordsNextLine[1 + identifierWordsShift]))
                                secondIdentifier = wordsNextLine[1 + identifierWordsShift];
                            else if (IsWordIdentifier(lines[i + 2].Split(' ')[0]))
                                secondIdentifier = lines[i + 2].Split(' ')[0];

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = lines[i + 1]
                                .IndexOf(GroupedDescriptionPrefixesQuadlet[j], StringComparison.Ordinal);
                            var tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                            if (lines[i].Length == price.Length)
                                currentGroup += lines[i + 1].Substring(
                                    lines[i + 1].LastIndexOf(secondIdentifier) + secondIdentifier.Length
                                    , lines[i + 1].Length - lines[i + 1].IndexOf(firstIdentifier) - tempDescription.Length - tempPrice.Length + price.Length);

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");

                            if (k < 3)
                                i++;
                        }
                        else
                        {
                            k--;
                        }

                        if (product != null)
                            products.Add(product);

                        k++;
                        break;
                    }

                if (k < 4)
                    i++;

                if (i + 1 >= lines.Length)
                    break;
            }

            foreach (var pr in products)
                pr.groupDescription = currentGroup.Trim();

            return products;
        }

        private List<Product> CreateProductsGroupTriplet(string[] lines, ref int i, string priceSymbol)
        {
            var currentGroup = "";

            var products = new List<Product>();

            for (var k = 0; k < 3;)
            {
                for (var j = 0; j < GroupedDescriptionPrefixesTriplet.Length; j++)
                    if (lines[i].Contains(GroupedDescriptionPrefixesTriplet[j]) ||
                        lines[i + 1].Contains(GroupedDescriptionPrefixesTriplet[j]))
                    {
                        var words = lines[i].Split(' ');

                        Product product = null;
                        // triplet                        
                        if (LineContainsIdentifier(lines[i]))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < words.Length &&
                                   !IsWordIdentifier(words[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = words[identifierWordsShift];

                            var secondIdentifier = "";
                            var secondWordOnThisLine = false;

                            if (IsWordIdentifier(words[1 + identifierWordsShift]))
                            {
                                secondIdentifier = words[1 + identifierWordsShift];
                                secondWordOnThisLine = true;
                            }
                            else if (IsWordIdentifier(lines[i + 1].Split(' ')[0]))
                            {
                                secondIdentifier = lines[i + 1].Split(' ')[0];
                            }

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            /*int descriptionStartIndex = lines[i].IndexOf(GroupedDescriptionPrefixesTriplet[k], StringComparison.Ordinal);
                            string tempDescription = lines[i].Substring(descriptionStartIndex);
                            tempDescription = tempDescription.Substring(0, tempDescription.Length - price.Length);*/

                            var descriptionStartIndex = 0;
                            var tempDescription = "";

                            if (lines[i].Contains(GroupedDescriptionPrefixesTriplet[j]))
                            {
                                descriptionStartIndex = lines[i].IndexOf(GroupedDescriptionPrefixesTriplet[j],
                                    StringComparison.Ordinal);
                                tempDescription = lines[i].Substring(descriptionStartIndex);
                                tempDescription =
                                    tempDescription.Substring(0, tempDescription.Length - price.Length);

                                var tempGroupData = lines[i].Substring(
                                                    lines[i].LastIndexOf(secondWordOnThisLine
                                                        ? secondIdentifier
                                                        : firstIdentifier)
                                                    + (secondWordOnThisLine
                                                        ? secondIdentifier
                                                        : firstIdentifier).Length
                                                    , lines[i].Length - (secondWordOnThisLine ? lines[i].LastIndexOf(secondIdentifier) + secondIdentifier.Length : lines[i].IndexOf(firstIdentifier) + firstIdentifier.Length) - price.Length - tempDescription.Length - 1) +
                                                " ";

                                if (string.IsNullOrEmpty(tempGroupData.Trim()) && lines.Length < i + 1)
                                    tempGroupData = lines[i + 1];

                                currentGroup += tempGroupData;
                            }
                            else if (lines[i + 1].Contains(GroupedDescriptionPrefixesTriplet[j]))
                            {
                                descriptionStartIndex = lines[i + 1]
                                    .IndexOf(GroupedDescriptionPrefixesTriplet[j], StringComparison.Ordinal);
                                tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                                currentGroup += lines[i+1].Substring(
                                                    lines[i+1].LastIndexOf(!secondWordOnThisLine
                                                        ? secondIdentifier
                                                        : firstIdentifier)
                                                    + (!secondWordOnThisLine
                                                        ? secondIdentifier
                                                        : firstIdentifier).Length
                                                    , lines[i+1].Length - (!secondWordOnThisLine ? lines[i+1].LastIndexOf(secondIdentifier) + secondIdentifier.Length : lines[i+1].IndexOf(firstIdentifier) + firstIdentifier.Length) - tempDescription.Length - 1) +
                                                " ";
                            }

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");


                            if (!secondWordOnThisLine && k < 2)
                                i++;
                        }
                        else if (PriceSymbols.Any(lines[i].Contains))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));
                            currentGroup += lines[i].Substring(0, lines[i].Length - price.Length);

                            var wordsNextLine = lines[i + 1].Split(' ');

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < wordsNextLine.Length &&
                                   !IsWordIdentifier(wordsNextLine[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = wordsNextLine[identifierWordsShift];

                            var secondIdentifier = "";

                            if (IsWordIdentifier(wordsNextLine[1 + identifierWordsShift]))
                                secondIdentifier = wordsNextLine[1 + identifierWordsShift];
                            else if (IsWordIdentifier(lines[i + 2].Split(' ')[0]))
                                secondIdentifier = lines[i + 2].Split(' ')[0];

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = lines[i + 1]
                                .IndexOf(GroupedDescriptionPrefixesTriplet[j], StringComparison.Ordinal);
                            var tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                            if (lines[i].Length == price.Length)
                                currentGroup += lines[i + 1].Substring(
                                    lines[i + 1].LastIndexOf(wordsNextLine[1 + identifierWordsShift]) +
                                    wordsNextLine[1 + identifierWordsShift].Length
                                    , lines[i + 1].Length - tempDescription.Length - tempPrice.Length + price.Length);

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");

                            if (k < 2)
                                i++;
                        }
                        else
                        {
                            k--;
                        }

                        if (product != null)
                            products.Add(product);

                        k++;
                        break;
                    }

                if (k < 3)
                    i++;
            }

            foreach (var pr in products)
                pr.groupDescription = currentGroup.Trim();

            return products;
        }

        private List<Product> CreateProductsGroupDouplet(string[] lines, ref int i, string priceSymbol)
        {
            var currentGroup = "";

            var products = new List<Product>();

            for (var k = 0; k < 2;)
            {
                for (var j = 0; j < GroupedDescriptionPrefixesDouplet.Length; j++)
                    if (lines[i].Contains(GroupedDescriptionPrefixesDouplet[j]) ||
                        lines[i + 1].Contains(GroupedDescriptionPrefixesDouplet[j]))
                    {
                        var words = lines[i].Split(' ');

                        Product product = null;

                        if (IsWordIdentifier(words[0]))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));

                            var secondIdentifier = "";

                            if (IsWordIdentifier(words[1]))
                                secondIdentifier = words[1];
                            else if (IsWordIdentifier(lines[i + 1].Split(' ')[0]))
                                secondIdentifier = lines[i + 1].Split(' ')[0];

                            var tempPrice = $"{words[0]} {secondIdentifier} {price}";
                            var descriptionStartIndex = lines[i].IndexOf(GroupedDescriptionPrefixesDouplet[k],
                                StringComparison.Ordinal);
                            var tempDescription = lines[i].Substring(descriptionStartIndex);
                            tempDescription =
                                tempDescription.Substring(0, tempDescription.Length - price.Length);
                            product = new Product(tempPrice, $"{tempDescription.Trim()}");

                            currentGroup += k == 0 ? lines[i + 1] : lines[i - 1];
                        }
                        else if (PriceSymbols.Any(lines[i].Contains))
                        {
                            var price = lines[i]
                                .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));
                            currentGroup += lines[i - 1];

                            var wordsNextLine = lines[i + 1].Split(' ');

                            var firstIdentifier = "";
                            var identifierWordsShift = 0;

                            while (identifierWordsShift < wordsNextLine.Length &&
                                   !IsWordIdentifier(wordsNextLine[identifierWordsShift]))
                                identifierWordsShift++;

                            firstIdentifier = wordsNextLine[identifierWordsShift];

                            var secondIdentifier = "";

                            if (IsWordIdentifier(wordsNextLine[1 + identifierWordsShift]))
                                secondIdentifier = wordsNextLine[1 + identifierWordsShift];
                            else if (IsWordIdentifier(lines[i + 2].Split(' ')[0]))
                                secondIdentifier = lines[i + 2].Split(' ')[0];

                            var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";

                            var descriptionStartIndex = lines[i + 1]
                                .IndexOf(GroupedDescriptionPrefixesDouplet[k], StringComparison.Ordinal);
                            var tempDescription = lines[i + 1].Substring(descriptionStartIndex);

                            product = new Product(tempPrice, $"{tempDescription.Trim()}");
                            i++;
                        }
                        else
                        {
                            k--;
                        }

                        if (product != null)
                            products.Add(product);

                        k++;
                        break;
                    }

                i++;
            }

            foreach (var pr in products)
                pr.groupDescription = currentGroup.Trim();

            return products;
        }

        private bool LineContainsIdentifier(string line)
        {
            var words = line.Split(' ');

            foreach (var word in words)
                if (IsWordIdentifier(word))
                    return true;

            return false;
        }

        private int CountIdentifiersOnLine(string line)
        {
            var count = 0;

            var words = line.Split(' ');

            foreach (var word in words)
                if (IsWordIdentifier(word))
                    count++;
            ;

            return count;
        }

        private bool IsWordIdentifier(string word)
        {
            foreach (var regexStr in regExIdentifiers)
            {
                var regex = new Regex(regexStr, RegexOptions.None);
                var matches = regex.Matches(word);

                if (matches.Count > 0)
                    return true;
            }

            return false;
        }

        private string GetFirstWord(string line)
        {
            foreach (var regexStr in regExIdentifiers)
            {
                var regex = new Regex(regexStr, RegexOptions.IgnoreCase);
                var matches = regex.Matches(line);

                if (matches.Count > 0)
                    return matches[0].Value;
            }

            return "";
        }

        private Product CreateProductSingle(string[] lines, ref int i, string priceSymbol)
        {
            var words = lines[i].Split(' ');

            Product product = null;

            if (words.Length > 1 && IsWordIdentifier(words[1]))
            {
                if (IsWordIdentifier(words[0]))
                {
                    if (words.Length > 4 && !MiddleWords.Contains(words[2]))
                    {
                        product = new Product(lines[i]);
                    }
                    else
                    {
                        var price = lines[i]
                            .Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));
                        var tempPrice = $"{words[0]} {words[1]} {price}";
                        var descriptionStartIndex =
                            lines[i].IndexOf(words[1], StringComparison.Ordinal) + words[1].Length;
                        var tempDescription = lines[i].Substring(descriptionStartIndex,
                            lines[i].Length - tempPrice.Length);
                        product = new Product(tempPrice, $"{lines[i - 1]} {tempDescription} {lines[i + 1]}");
                    }
                }
                else if (IsWordIdentifier(words[2]))
                {
                    var price = lines[i].Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));
                    var tempPrice = $"{words[1]} {words[2]} {price}";
                    var descriptionStartIndex =
                        lines[i].LastIndexOf(words[2], StringComparison.Ordinal) + words[2].Length;
                    var tempDescription = lines[i].Substring(descriptionStartIndex,
                        lines[i].Length - lines[i].IndexOf(words[1], StringComparison.Ordinal) -
                        tempPrice.Length);
                    product = new Product(tempPrice, $"{tempDescription}");
                }
            }
            else if (i + 1 < lines.Length)
            {
                var wordsNextLine = lines[i + 1].Split(' ');
                var price = lines[i].Substring(lines[i].LastIndexOf(priceSymbol, StringComparison.Ordinal));

                var firstIdentifier = "";
                var secondIdentifier = "";
                var identifierWordsShift = 0;

                var identifiersOnLine = CountIdentifiersOnLine(lines[i]);

                if (identifiersOnLine == 0)
                {
                    while (identifierWordsShift < wordsNextLine.Length &&
                           !IsWordIdentifier(wordsNextLine[identifierWordsShift]))
                        identifierWordsShift++;

                    firstIdentifier = wordsNextLine[identifierWordsShift];
                    secondIdentifier = wordsNextLine[identifierWordsShift + 1];
                }
                else if (identifiersOnLine == 1)
                {
                    while (identifierWordsShift < words.Length &&
                           !IsWordIdentifier(words[identifierWordsShift]))
                        identifierWordsShift++;

                    firstIdentifier = words[identifierWordsShift];

                    for (var k = 0; k < wordsNextLine.Length; k++)
                        if (IsWordIdentifier(wordsNextLine[k]))
                        {
                            secondIdentifier = wordsNextLine[k];
                            break;
                        }
                }
                else if (identifiersOnLine >= 2)
                {
                    while (identifierWordsShift < words.Length &&
                           !IsWordIdentifier(words[identifierWordsShift]))
                        identifierWordsShift++;

                    firstIdentifier = words[identifierWordsShift];
                    secondIdentifier = words[identifierWordsShift + 1];
                }

                var tempPrice = $"{firstIdentifier} {secondIdentifier} {price}";
                var tempDescription = "";

                var descriptionShift = 0;

                if (identifiersOnLine == 0)
                {
                    if (lines[i].Length - price.Length <= 0) descriptionShift = 1;
                }
                else if (identifiersOnLine == 1)
                {
                    if (lines[i].Length -
                        (price.Length + lines[i].IndexOf(firstIdentifier) + firstIdentifier.Length) <=
                        3) descriptionShift = 1;
                }
                else if (identifiersOnLine >= 2)
                {
                    if (lines[i].Length - (price.Length + lines[i].IndexOf(firstIdentifier) +
                                           firstIdentifier.Length + lines[i].LastIndexOf(secondIdentifier) +
                                           secondIdentifier.Length) <= 3)
                    {
                        if (DontAddLinesContatining.Any(lines[i + 1].Contains))
                            descriptionShift = 1;
                        else
                            descriptionShift = -1;
                    }
                }

                if (descriptionShift == 0)
                    tempDescription = lines[i].Substring(identifiersOnLine == 0 ? 0 :
                        identifiersOnLine == 1 ? lines[i].IndexOf(firstIdentifier) + firstIdentifier.Length :
                        lines[i].LastIndexOf(secondIdentifier) + secondIdentifier.Length,
                        lines[i].Length - price.Length -
                        (identifiersOnLine == 0
                            ? 0
                            : identifiersOnLine == 1
                                ? lines[i].IndexOf(firstIdentifier) + firstIdentifier.Length
                                : lines[i].LastIndexOf(secondIdentifier) + secondIdentifier.Length));
                else if (descriptionShift == 1)
                    tempDescription = lines[i + 1].Substring(identifiersOnLine == 2
                            ? 0
                            : identifiersOnLine == 1
                                ? lines[i + 1].IndexOf(firstIdentifier) + firstIdentifier.Length
                                : lines[i + 1].LastIndexOf(secondIdentifier) + secondIdentifier.Length,
                        lines[i + 1].Length -
                        (identifiersOnLine >= 2
                            ? 0
                            : identifiersOnLine == 1
                                ? lines[i + 1].IndexOf(firstIdentifier) + firstIdentifier.Length
                                : lines[i + 1].LastIndexOf(secondIdentifier) + secondIdentifier.Length));
                else
                    tempDescription = lines[i + descriptionShift];

                product = new Product(tempPrice, tempDescription);
            }
            /*for (int j = 0; j < words.Length - 1; j++)
            {
                if (lines[i].Contains("5001000006") && j == 2)
                    Console.WriteLine("1");
                var wordValue = Int64.MinValue;
                if (words[j].Length > 0 && words[j + 1].Length > 0 && IdentifiersLengths.Any(s => s == words[j].Length) &&
                    (Int64.TryParse(words[j], out wordValue) ||
                    Int64.TryParse(words[j].Substring(1), out wordValue) ||
                    (words[j].Length > 9 && Int64.TryParse(words[j].Substring(9), out wordValue))) &&
                    !Int64.TryParse(words[j + 1].Substring(1), out wordValue))
                {
                    if (j > 0 && (IdentifiersLengths.Any(s => s == words[j - 1].Length)))
                    {
                        product = new Product(lines[i]
                            .Substring(lines[i].IndexOf(words[j - 1], StringComparison.Ordinal)));
                        break;
                    }
                    else if (lines[i + 1].StartsWith(words[j]))
                    {
                        string tempPrice = $"{words[j]} {words[j]} {lines[i].Substring(lines[i].IndexOf(priceSymbol, StringComparison.Ordinal))}";
                        string tempDescription = lines[i + 1].Substring(words[j].Length + 1);

                        product = new Product(tempPrice, tempDescription);
                    }
                }
            }*/
            //}

            return product;
        }

        private Product CreateProductFromSingleLine(string line)
        {
            Product product = null;


            return product;
        }
    }
}