using System;
using System.Windows.Forms;
using pdftron;
using pdftron.Common;
using pdftron.Filters;
using pdftron.SDF;
using pdftron.PDF;
using XSet = System.Collections.Generic.List<int>;
using System.Text;
using System.Collections.Generic;

namespace PDFTronTest
{
    public partial class Form1 : Form
    {
        private static pdftron.PDFNetLoader pdfNetLoader = pdftron.PDFNetLoader.Instance();

        public Form1()
        {
            InitializeComponent();
            PDFNet.Initialize();

            string input_path = "Turnstiles 2020 End User EURO price list _ RUSSIA (2).pdf";
            string output_path = "test_pdftron.pdf";

            /*using (PDFDoc doc = new PDFDoc(input_path))
            using (ContentReplacer replacer = new ContentReplacer())
            {
                doc.InitSecurityHandler();

                // first, replace the image on the first page
                Page page = doc.GetPage(1);
                //Image img = Image.Create(doc, "peppers.png");
                //replacer.AddImage(page.GetMediaBox(), img.GetSDFObj());
                // next, replace the text place holders on the second page
                replacer.AddString("€", "$");
                replacer.AddString("EURO", "DOLLAR");
                replacer.AddString("Euro", "Dollar");
                replacer.AddString("euro", "dollar");
                replacer.AddString("Range", "Grange");
                replacer.AddString("Price", "Price1");
                // finally, apply
                replacer.Process(page);

                var contents = page.GetContents();

                

                doc.Save(output_path, 0);
                MessageBox.Show($"Done. Result saved in {output_path}");
            }*/

            /*page = doc.GetPage(1);
                Rect target_region = page.GetMediaBox();
                string replacement_text = "DOLLAR";
                replacer.AddText(target_region, replacement_text);
                replacer.Process(page);*/

            
            using (PDFDoc doc = new PDFDoc(input_path))
            {
                var fnt = pdftron.PDF.Font.Create(doc, "Arial", "0123456789 ,");                
                
                doc.InitSecurityHandler();

                ElementWriter writer = new ElementWriter();
                ElementReader reader = new ElementReader();
                XSet visited = new XSet();

                PageIterator itr = doc.GetPageIterator();

                while (itr.HasNext())
                {
                    try
                    {
                        Page page = itr.Current();
                        visited.Add(page.GetSDFObj().GetObjNum());

                        reader.Begin(page);
                        writer.Begin(page, ElementWriter.WriteMode.e_replacement, false, true, page.GetResourceDict());

                        var pgText = ProcessElements(reader, writer, visited, fnt);
                        
                        writer.End();
                        reader.End();

                        itr.Next();
                    }
                    catch (PDFNetException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                doc.Save(output_path, SDFDoc.SaveOptions.e_remove_unused);
                MessageBox.Show($"Done. Result saved in {output_path}...");
            }

            /*PDFDoc doc = new PDFDoc(input_path);
            ElementReader reader = new ElementReader();
            ElementWriter writer = new ElementWriter();

            //  Read page content on every page in the document
            for (PageIterator itr = doc.GetPageIterator(); itr.HasNext(); itr.Next())
            {
                var page = itr.Current();
                // Read the page
                reader.Begin(page);
                writer.Begin(page, ElementWriter.WriteMode.e_replacement, false, true, page.GetResourceDict());
                var pageText = ProcessElements(reader, writer);
                reader.End();
            }*/
        }

        static string ProcessElements(ElementReader reader, ElementWriter writer)
        {
            var pageTextBuilder = new StringBuilder();
            Element element;
            while ((element = reader.Next()) != null)   // Read page contents
            {
                switch (element.GetType())
                {
                    case Element.Type.e_text:               // Process text strings...
                        {
                            String str = element.GetTextString();

                            if(str.Contains("€"))
                            {
                                if (str.Length > 1)
                                {
                                    var cost = 0f;
                                    if (float.TryParse(str.Replace("€", ""), out cost))
                                    {
                                        var newStr = $"€ {cost * 1.4:F1}";
                                        var buffer = System.Text.Encoding.UTF8.GetBytes(newStr);
                                        element.SetTextData(buffer, newStr.Length);
                                        writer.WriteElement(element);
                                    }
                                }
                            }
                            //pageTextBuilder.Append(str);
                            break;
                        }

                    case Element.Type.e_form:               // Process form XObjects
                        {
                            Console.WriteLine("Process Element.Type.e_form");
                            reader.FormBegin();
                            ProcessElements(reader, writer);
                            reader.End();
                            break;
                        }
                }
            }
            return pageTextBuilder.ToString();
        }

        static string ProcessElements(ElementReader reader, ElementWriter writer, XSet visited, pdftron.PDF.Font font)
        {
            var builder = new StringBuilder();
            Element element;
            var flag = false;
            var costStr = "";
            var costElements = new List<Element>();
            while ((element = reader.Next()) != null)
            {
                switch (element.GetType())
                {                    
                    case Element.Type.e_text:
                        {
                            var str = element.GetTextString();
                            builder.Append(str);

                            /*if(flag)
                            {
                                if((string.IsNullOrWhiteSpace(str) && string.IsNullOrEmpty(costStr)) || float.TryParse(str, out var _) || str == ",")
                                {
                                    costStr += str;
                                    costElements.Add(element);
                                    element.SetTextData(System.Text.Encoding.UTF8.GetBytes(""), 0);
                                }
                                else
                                {
                                    if(!string.IsNullOrEmpty(costStr.Trim()))
                                    {
                                        var cost = float.Parse(costStr);
                                        var newStr = $" {cost * 1.4:F1}";
                                        var buffer = System.Text.Encoding.UTF8.GetBytes(newStr);
                                        element.SetTextData(buffer, newStr.Length);
                                    }
                                    flag = false;
                                    costStr = "";
                                    costElements.Clear();
                                }
                            }*/

                            if (str.Contains("€"))
                            {
                                if (str.Length > 1)
                                {
                                    var cost = 0f;
                                    if (float.TryParse(str.Replace("€", ""), out cost))
                                    {
                                        var newStr = $"€ {cost * 1.4:F1}";
                                        var buffer = System.Text.Encoding.Default.GetBytes(newStr);
                                        var fnt = element.GetGState().GetFont();
                                        //var enc = fnt.GetEncoding();
                                        /*var name = fnt.GetName();
                                        var st1ft = fnt.GetStandardType1FontType();
                                        var d = fnt.GetCharCodeIterator();
                                        element.GetGState().SetFont(fnt, 8);*/
                                        element.SetTextData(buffer, buffer.Length);

                                        /*Element newElement = eb.CreateTextBegin(fnt, 4);
                                        var matrix = element.GetTextMatrix();
                                        newElement.SetTextMatrix(matrix);
                                        newElement.GetGState().SetLeading(2);       // Set the spacing between lines
                                        writer.WriteElement(newElement);
                                        writer.WriteElement(eb.CreateUnicodeTextRun(newStr));*/
                                    }
                                }
                                else
                                {
                                    flag = true;
                                }
                            }
                            
                            writer.WriteElement(element);
                            break;
                        }
                    case Element.Type.e_form:               // Process form XObjects
                        {
                            Console.WriteLine("Process Element.Type.e_form");
                            reader.FormBegin();
                            builder.Append(ProcessElements(reader, writer, visited, font));
                            reader.End();
                            break;
                        }
                    default:
                        writer.WriteElement(element);
                        break;
                }
            }
            return builder.ToString();
        }

    }
}
