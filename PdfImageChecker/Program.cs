using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PDFiumCore;

class Program
{
    private const int FPDF_PAGEOBJ_IMAGE = 3;

    unsafe static string GetPageText(FpdfPageT page)
    {
        var textPage = fpdf_text.FPDFTextLoadPage(page);
        int count = fpdf_text.FPDFTextCountChars(textPage);
        if (count <= 0)
        {
            fpdf_text.FPDFTextClosePage(textPage);
            return string.Empty;
        }
        ushort[] buffer = new ushort[count + 1];
        fixed (ushort* ptr = buffer)
        {
            fpdf_text.FPDFTextGetText(textPage, 0, count, ref buffer[0]);
        }
        fpdf_text.FPDFTextClosePage(textPage);
        return new string(buffer.TakeWhile(b => b != 0).Select(b => (char)b).ToArray());
    }

    static bool PageHasImage(FpdfPageT page)
    {
        int objCount = fpdf_edit.FPDFPageCountObjects(page);
        for (int i = 0; i < objCount; i++)
        {
            var obj = fpdf_edit.FPDFPageGetObject(page, i);
            if (obj != null)
            {
                int type = fpdf_edit.FPDFPageObjGetType(obj);
                if (type == FPDF_PAGEOBJ_IMAGE)
                {
                    return true;
                }
            }
        }
        return false;
    }

    unsafe static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: PdfImageChecker <pdf-file>");
            return;
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return;
        }

        fpdfview.FPDF_InitLibrary();

        var document = fpdfview.FPDF_LoadDocument(path, null);
        if (document == null)
        {
            Console.WriteLine("Unable to open document");
            fpdfview.FPDF_DestroyLibrary();
            return;
        }

        int pageCount = fpdfview.FPDF_GetPageCount(document);
        for (int i = 0; i < pageCount; i++)
        {
            var page = fpdfview.FPDF_LoadPage(document, i);
            if (page == null)
            {
                Console.WriteLine($"Failed to load page {i + 1}");
                continue;
            }

            string text = GetPageText(page);
            bool hasImage = PageHasImage(page);

            Console.WriteLine($"Page {i + 1}: Image present = {hasImage}");
            Console.WriteLine(text);

            fpdfview.FPDF_ClosePage(page);
        }

        fpdfview.FPDF_CloseDocument(document);
        fpdfview.FPDF_DestroyLibrary();
    }
}
