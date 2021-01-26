using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GroupDocs.Redaction;
using GroupDocs.Redaction.Options;
using GroupDocs.Redaction.Redactions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
//using PDFTRon;

namespace PDFParse
{
    public partial class FormMain : Form
    {

        public FormMain()
        {
            InitializeComponent();
        }

        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            try
            {
                SelectAndParse();
                MessageBox.Show("Done!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectAndParse()
        {
            string fileName = SelectFile();
            var parsedData = ParseFile(fileName);
            SaveParsedData(parsedData);
        }

        private string SelectFile()
        {
            using (var ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() != DialogResult.OK)
                    return "";

                string fileName = ofd.FileName;

                return fileName;
            }
        }

        private string ParseFile(string fileName)
        {
            var modifier = 1.4f;
            var parsedData = "";

            using (PdfReader reader = new PdfReader(fileName))
            {
                parsedData = ParsePdfDocument(reader);
                //parsedData = ModifyText(parsedData, modifier);
            }

            return parsedData;
            
        }

        private string ModifyText(string input, float mod)
        {
            ProductPriceParser parser = new ProductPriceParser();

            return parser.UpdateCostsOnLines(input.Split('\n'), mod, false);
        }

        private void SaveParsedData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            File.WriteAllText("parsedPDFData.txt", data);
        }

        private string ParsePdfDocument(PdfReader reader)
        {
            StringBuilder text = new StringBuilder();
            ITextExtractionStrategy its = new iTextSharp.text.pdf.parser.LocationTextExtractionStrategy();
            ProductPriceParser parser = new ProductPriceParser();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {                
                string pageText = PdfTextExtractor.GetTextFromPage(reader, i);                
                pageText = parser.ParsePageText(pageText);
                if (!string.IsNullOrEmpty(pageText))
                    text.Append(pageText);
            }

            return text.ToString();
        }

        private string UpdateCostsOfDocument(PdfReader reader, float modifier)
        {
            StringBuilder text = new StringBuilder();
            ITextExtractionStrategy its = new iTextSharp.text.pdf.parser.LocationTextExtractionStrategy();
            ProductPriceParser parser = new ProductPriceParser();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                var temp = reader.GetPageContent(i);
                string pageText = PdfTextExtractor.GetTextFromPage(reader, i);
                pageText = parser.UpdateCostsOnPage(pageText, modifier);
                if (!string.IsNullOrEmpty(pageText))
                    text.Append(pageText);
            }

            return text.ToString();
        }

        public void ManipulatePdf(string src, string dest) 
        {
            using (Redactor redactor = new Redactor(src, new LoadOptions(), new RedactorSettings(new RedactionDump())))
            {
                redactor.Apply(new RegexRedaction(@"€ \d+,?\d?", new ReplacementOptions("[calculate]")));
                redactor.Save(new SaveOptions() { AddSuffix = true });
            }
        }

        /// <summary>
        /// This method is used to search for the location words in pdf and update it with the words given from replacingText variable
        /// </summary>
        /// <param name="pSearch">Searchable String</param>
        /// <param name="replacingText">Replacing String</param>
        /// <param name="SC">Case Ignorance</param>
        /// <param name="SourceFile">Path of the source file</param>
        /// <param name="DestinationFile">Path of the destination file</param>
        /*public static void PDFTextGetter(string pSearch, string replacingText, StringComparison SC, string SourceFile, string DestinationFile)
        {
            try
            {
                iTextSharp.text.pdf.PdfContentByte cb = null;
                iTextSharp.text.pdf.PdfContentByte cb2 = null;
                iTextSharp.text.pdf.PdfWriter writer = null;
                iTextSharp.text.pdf.BaseFont bf = null;

                if (System.IO.File.Exists(SourceFile))
                {
                    PdfReader pReader = new PdfReader(SourceFile);


                    for (int page = 1; page <= pReader.NumberOfPages; page++)
                    {
                        LocationTextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                        cb = stamper.GetOverContent(page);
                        cb2 = stamper.GetOverContent(page);

                        //Send some data contained in PdfContentByte, looks like the first is always cero for me and the second 100, 
                        //but i'm not sure if this could change in some cases
                        strategy.UndercontentCharacterSpacing = (int)cb.CharacterSpacing;
                        strategy.UndercontentHorizontalScaling = (int)cb.HorizontalScaling;

                        //It's not really needed to get the text back, but we have to call this line ALWAYS, 
                        //because it triggers the process that will get all chunks from PDF into our strategy Object
                        string currentText = PdfTextExtractor.GetTextFromPage(pReader, page, strategy);

                        //The real getter process starts in the following line
                        List<iTextSharp.text.Rectangle> MatchesFound = strategy.GetTextLocations(pSearch, SC);

                        //Set the fill color of the shapes, I don't use a border because it would make the rect bigger
                        //but maybe using a thin border could be a solution if you see the currect rect is not big enough to cover all the text it should cover
                        cb.SetColorFill(BaseColor.WHITE);

                        //MatchesFound contains all text with locations, so do whatever you want with it, this highlights them using PINK color:

                        foreach (iTextSharp.text.Rectangle rect in MatchesFound)
                        {
                            //width
                            cb.Rectangle(rect.Left, rect.Bottom, 60, rect.Height);
                            cb.Fill();
                            cb2.SetColorFill(BaseColor.BLACK);
                            bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

                            cb2.SetFontAndSize(bf, 9);

                            cb2.BeginText();
                            cb2.ShowTextAligned(0, replacingText, rect.Left, rect.Bottom, 0);
                            cb2.EndText();
                            cb2.Fill();
                        }

                    }
                }

            }
            catch (Exception ex)
            {

            }

        }*/

        void VerySimpleReplaceText(string OrigFile, string ResultFile, string origText, string replaceText)
        {
            using (PdfReader reader = new PdfReader(OrigFile))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    byte[] contentBytes = reader.GetPageContent(i);
                    string contentString = PdfEncodings.ConvertToString(contentBytes, PdfObject.TEXT_PDFDOCENCODING);
                    contentString = contentString.Replace(origText, replaceText);
                    reader.SetPageContent(i, PdfEncodings.ConvertToBytes(contentString, PdfObject.TEXT_PDFDOCENCODING));
                }
                new PdfStamper(reader, new FileStream(ResultFile, FileMode.Create, FileAccess.Write)).Close();
            }
        }

        private void highlightPDFAnnotation(string inputFile, string outputFile, string[] highlightText)
        {
            try
            {
                PdfReader reader = new PdfReader(inputFile);

                using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (PdfStamper stamper = new PdfStamper(reader, fs))
                    {
                        int pageCount = reader.NumberOfPages;
                        for (int pageno = 1; pageno <= pageCount; pageno++)
                        {
                            myLocationTextExtractionStrategy strategy = new myLocationTextExtractionStrategy();
                            strategy.UndercontentHorizontalScaling = 100;

                            string currentText = PdfTextExtractor.GetTextFromPage(reader, pageno, strategy);
                            for (int i = 0; i < highlightText.Length; i++)
                            {
                                List<iTextSharp.text.Rectangle> MatchesFound = strategy.GetTextLocations(highlightText[i].Trim(), StringComparison.CurrentCultureIgnoreCase);
                                foreach (Rectangle rect in MatchesFound)
                                {
                                    float[] quad = { rect.Left, rect.Bottom, rect.Right, rect.Bottom, rect.Left, rect.Top, rect.Right, rect.Top };
                                    //Create our hightlight
                                    PdfAnnotation highlight = PdfAnnotation.CreateMarkup(stamper.Writer, rect, null, PdfAnnotation.MARKUP_HIGHLIGHT, quad);
                                    //Set the color
                                    highlight.Color = BaseColor.RED;

                                    PdfAppearance appearance = PdfAppearance.CreateAppearance(stamper.Writer, rect.Width, rect.Height);
                                    PdfGState state = new PdfGState();
                                    state.BlendMode = new PdfName("Multiply");
                                    appearance.SetGState(state);
                                    appearance.Rectangle(0, 0, rect.Width, rect.Height);
                                    appearance.SetColorFill(BaseColor.RED);
                                    appearance.Fill();
                                    highlight.SetAppearance(PdfAnnotation.APPEARANCE_NORMAL, appearance);
                                    stamper.AddAnnotation(highlight, pageno);
                                }
                            }
                        }
                    }
                }
                reader.Close();

                /*File.Copy(outputFile, inputFile, true);
                File.Delete(outputFile);*/
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string file = SelectFile();
            string text = File.ReadAllText(file);
            var lines = text.Split('\n');
            var builder = new StringBuilder();

            string dChar = ";";
            string vChar = "";

            foreach (var line in lines)
            {
                if(string.IsNullOrEmpty(line))
                    continue;

                var parts = line.Split('|');

                if (parts.Length == 4)
                {
                    builder.AppendLine($"{vChar}{parts[0].Trim()}{vChar}{dChar}{vChar}{parts[1].Trim()}{vChar}{dChar}{vChar}{vChar}{dChar}{vChar}{parts[2].Trim()}{vChar}{dChar}{vChar}{parts[3].Trim()}{vChar}");
                }
                else if (parts.Length == 5)
                {
                    builder.AppendLine($"{vChar}{parts[0].Trim()}{vChar}{dChar}{vChar}{parts[1].Trim()}{vChar}{dChar}{vChar}{parts[2].Trim()}{vChar}{dChar}{vChar}{parts[3].Trim()}{vChar}{dChar}{vChar}{parts[4].Trim()}{vChar}");
                }
                else
                {
                    builder.AppendLine(line.Trim());
                }
            }

            File.WriteAllText("completed.csv", builder.ToString());

            MessageBox.Show("done");
        }

        static iTextSharp.text.pdf.PdfStamper stamper = null;

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                var dChar = ';';
                var modifier = 1.4f;
                var fileName = SelectFile();

                //using (PDFDoc doc = new PDFDoc(input_path + "BusinessCardTemplate.pdf"))
                //using (ContentReplacer replacer = new ContentReplacer())
                //{
                //    doc.InitSecurityHandler();

                //    // first, replace the image on the first page
                //    Page page = doc.GetPage(1);
                //    Image img = Image.Create(doc, input_path + "peppers.jpg");
                //    replacer.AddImage(page.GetMediaBox(), img.GetSDFObj());
                //    // next, replace the text place holders on the second page
                //    replacer.AddString("NAME", "John Smith");
                //    replacer.AddString("QUALIFICATIONS", "Philosophy Doctor");
                //    replacer.AddString("JOB_TITLE", "Software Developer");
                //    replacer.AddString("ADDRESS_LINE1", "#100 123 Software Rd");
                //    replacer.AddString("ADDRESS_LINE2", "Vancouver, BC");
                //    replacer.AddString("PHONE_OFFICE", "604-730-8989");
                //    replacer.AddString("PHONE_MOBILE", "604-765-4321");
                //    replacer.AddString("EMAIL", "info@pdftron.com");
                //    replacer.AddString("WEBSITE_URL", "http://www.pdftron.com");
                //    // finally, apply
                //    replacer.Process(page);

                //    doc.Save(output_path + "BusinessCard.pdf", 0);
                //    Console.WriteLine("Done. Result saved in BusinessCard.pdf");
                //}

                //VerySimpleReplaceText(fileName, "test.pdf", "€", "$");

                /*string[] highlightText = new string[] { "€" };
                string inputFile = fileName;
                string outputFile = "test.pdf";
                // get input document            
                highlightPDFAnnotation(inputFile, outputFile, highlightText);*/

                /*string ReplacingVariable = "$";
                string descFile = "moded";
                PdfReader pReader = new PdfReader(fileName);
                stamper = new iTextSharp.text.pdf.PdfStamper(pReader, new System.IO.FileStream(descFile, System.IO.FileMode.Create));
                PDFTextGetter("€", ReplacingVariable, StringComparison.CurrentCultureIgnoreCase, fileName, descFile);
                stamper.Close();
                pReader.Close();*/


                //ManipulatePdf(fileName, "mod2.pdf");

                // open the reader
                PdfReader reader = new PdfReader(fileName);
                var parsedData = UpdateCostsOfDocument(reader, modifier);
                Rectangle size = reader.GetPageSizeWithRotation(1);
                Document document = new Document(size);

                // open the writer
                FileStream fs = new FileStream("modified00.pdf", FileMode.Create, FileAccess.Write);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                // the pdf content
                PdfContentByte cb = writer.DirectContent;
                BaseFont font = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

                cb.SetFontAndSize(font, 8);
                cb.ShowText(parsedData);

                document.Close();
                fs.Close();
                writer.Close();
                reader.Close();

                //var fileText = File.ReadAllText(fileName);
                //var lines = fileText.Split('\n');
                //var builder = new StringBuilder();


                //foreach (var line in lines)
                //{
                //    if (string.IsNullOrEmpty(line))
                //        continue;

                //    var parts = line.Split(dChar);
                //    var costCell = parts.Last();
                //    var cost = 0f;

                //    if (float.TryParse(costCell, out cost))
                //    {
                //        cost *= modifier;
                //        parts[parts.Length - 1] = cost.ToString("F");

                //        var newLine = string.Join(dChar.ToString(), parts);
                //        builder.AppendLine(newLine);
                //    }
                //    else
                //    {
                //        builder.AppendLine(line.Trim());
                //    }
                //}

                //File.WriteAllText("modified.csv", builder.ToString());

                MessageBox.Show("done");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var modifier = 1.4f;
            string fileName = SelectFile();
            var temp = File.ReadAllText(fileName);
            var modifiedText = ModifyText(temp, modifier);
            File.WriteAllText("modified.csv", modifiedText);
            MessageBox.Show("done");
        }
    }


    public class RedactionDump : IRedactionCallback
    {
        public RedactionDump()
        {
        }

        public bool AcceptRedaction(RedactionDescription description)
        {
            Console.Write("{0} redaction, {1} action, item {2}. ", description.RedactionType, description.ActionType, description.OriginalText);
            if (description.Replacement != null)
            {
                Console.Write("Text {0} is replaced with {1}. ", description.Replacement.OriginalText, description.Replacement.Replacement);
            }
            Console.WriteLine();
            // you can return "false" here to prevent particular change during redaction process
            return true;
        }
    }
}
