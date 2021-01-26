using Spire.Pdf;
using Spire.Pdf.General.Find;
using Spire.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SpirePDFTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //RemoveDiscounts();
            MultuplyCosts();
        }

        private static void RemoveDiscounts()
        {
            PdfDocument doc = new PdfDocument();
            //
            doc.LoadFromFile("UnEncrypted (1)-21-30.pdf");

            /*PdfBrush brush = new PdfSolidBrush(Color.Black);
            PdfTrueTypeFont font = new PdfTrueTypeFont(new Font("Arial", 20f, FontStyle.Regular));
            doc.Pages[0].Canvas.DrawString("ARE YOU WORKING?", font, brush, new RectangleF(0,0, 500,50));*/

            for (int i = 0; i < doc.Pages.Count; i++)
            {
                var page = doc.Pages[i];
                RemoveText(page, "Customer Number:", 0);
                RemoveText(page, "6793", 0);
                RemoveText(page, "Customer Name:", 0);
                RemoveText(page, "ARMO-SYSTEMS LLC (RU)", 0);
                RemoveText(page, "ARMO-SYSTEMS LLC", 0);
                RemoveText(page, "YOUR % DISCOUNT", 0);
                RemoveText(page, "Unit Price", 600);

                var discount = RemovePercentStrings(page);

                if (discount.Count > 0)
                {
                    RemoveDiscounts(page, discount);
                }
                else if (PageHaveDistributor(page))
                {
                    RemoveDiscounts(page);
                }
            }

            doc.SaveToFile("out.pdf");
        }

        private static bool PageHaveDistributor(PdfPageBase page)
        {
            PdfTextFindCollection collection = page.FindText("DISTRIBUTOR", TextFindParameter.IgnoreCase);

            RectangleF rec;
            foreach (PdfTextFind find in collection.Finds)
            {
                rec = find.Bounds;
                page.Canvas.DrawRectangle(PdfBrushes.White, rec);
                return true;
            }

            return false;
        }

        private static void RemoveText(PdfPageBase page, string patternToRemove, int minPosX)
        {
            PdfTextFindCollection collection = page.FindText(patternToRemove, TextFindParameter.IgnoreCase);

            RectangleF rec;
            foreach (PdfTextFind find in collection.Finds)
            {
                rec = find.Bounds;

                if(rec.X > minPosX)
                    page.Canvas.DrawRectangle(PdfBrushes.White, rec);
            }
        }

        private static List<float> RemovePercentStrings(PdfPageBase page)
        {
            var thisPageDiscount = new List<float>();
            PdfTextFindCollection collection = page.FindText("%", TextFindParameter.IgnoreCase);

            RectangleF rec;
            foreach (PdfTextFind find in collection.Finds)
            {
                rec = find.Bounds;

                rec.X -= 25;
                rec.Width += 25;

                var costStr = find.OuterText.Trim().Replace("%", "").Replace(".",",");
                var temp = 0f;

                if (float.TryParse(costStr, out temp))
                {
                    page.Canvas.DrawRectangle(PdfBrushes.White, rec);
                    thisPageDiscount.Add(temp);
                }
            }

            return thisPageDiscount;
        }

        private static void RemoveDiscounts(PdfPageBase page, List<float> discount)
        {
            PdfTextFindCollection collection = page.FindText(".", TextFindParameter.IgnoreCase);

            RectangleF rec;

            var previousValue = 0f;

            for(int i = 0; i < collection.Finds.Length; i++)
            {
                rec = collection.Finds[i].Bounds;

                rec.X -= 11;
                rec.Width += 40;

                var costStr = collection.Finds[i].OuterText.Trim().Replace(",", "").Replace(".", ",");

                if (float.TryParse(costStr, out var currentValue))
                {
                    foreach (var disc in discount)
                    {
                        if(disc == 0)
                            continue;

                        /*if (disc == 50)
                            Console.WriteLine(currentValue + " " + rec.X);*/

                        if (Math.Abs(Math.Abs(currentValue) - Math.Abs(previousValue) * (100 - disc) / 100) < 0.05f && Math.Abs(currentValue) > 0)
                        {
                            
                            //if (rec.X >= 730 && rec.X <= 760)
                            page.Canvas.DrawRectangle(PdfBrushes.White, rec);
                        }
                        else if (Math.Abs(currentValue - previousValue) < 0.1f)
                        {
                            /*if ((rec.X >= 732 && rec.X <= 736) || (rec.X >= 692 && rec.X <= 700))
                                page.Canvas.DrawRectangle(PdfBrushes.White, rec);*/
                        }
                    }
                    

                    previousValue = currentValue;
                }
            }
        }

        private static void RemoveDiscounts(PdfPageBase page)
        {
            PdfTextFindCollection collection = page.FindText(".", TextFindParameter.IgnoreCase);

            RectangleF rec;

            var previousValue = 0f;

            for (int i = 0; i < collection.Finds.Length; i++)
            {
                rec = collection.Finds[i].Bounds;

                rec.X -= 11;
                rec.Width += 50;

                var costStr = collection.Finds[i].OuterText.Trim().Replace(",", "").Replace(".", ",");

                if (float.TryParse(costStr, out var currentValue))
                {
                    /*if(currentValue == 1000)
                        Console.WriteLine("q");

                    if (Math.Abs(currentValue - previousValue) > 0.1f && Math.Abs(currentValue) > 0 )
                    {
                        Console.WriteLine(currentValue + " " + rec.X);*/
                        if (rec.X >= 761 && rec.X <= 769)
                            page.Canvas.DrawRectangle(PdfBrushes.White, rec);
                    //}
                    

                    previousValue = currentValue;
                }
            }
        }

        private static void MultuplyCosts()
        {
            PdfDocument doc = new PdfDocument();
            //
            for (int docNumIndex = 1; docNumIndex <= 9; docNumIndex++)
            {
                doc.LoadFromFile($"GUNNEBO_Turnstiles 2021 End User EURO price list_ RUSSIA-{docNumIndex}.pdf");

                /*PdfBrush brush = new PdfSolidBrush(Color.Black);
                PdfTrueTypeFont font = new PdfTrueTypeFont(new Font("Arial", 20f, FontStyle.Regular));
                doc.Pages[0].Canvas.DrawString("ARE YOU WORKING?", font, brush, new RectangleF(0,0, 500,50));*/

                for (int i = 0; i < doc.Pages.Count; i++)
                {
                    var page = doc.Pages[i];
                    //
                    PdfTextFindCollection collection = page.FindText("€", TextFindParameter.IgnoreCase);

                    PdfBrush brush = new PdfSolidBrush(Color.Black);
                    PdfTrueTypeFont font = new PdfTrueTypeFont(new Font("Arial", 4f, FontStyle.Regular));

                    RectangleF rec;
                    foreach (PdfTextFind find in collection.Finds)
                    {
                        rec = find.Bounds;

                        rec.X += rec.Width;
                        rec.Width = 20;

                        var costStr = find.OuterText.Trim().Replace("€", "").Replace(",", "");
                        var costVal = 0f;
                        if (float.TryParse(costStr, out costVal))
                        {
                            var newText = $"{costVal * 1.4:F0}";
                            page.Canvas.DrawRectangle(PdfBrushes.White, rec);
                            page.Canvas.DrawString(newText, font, brush, new PointF(rec.X, rec.Y - 0.5f));
                        }

                    }
                }

                doc.SaveToFile($"modifiedCosts_{docNumIndex}.pdf");
            }
        }
    }
}
