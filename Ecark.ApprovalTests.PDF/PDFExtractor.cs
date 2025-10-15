using System.Text;
using iText.Kernel.Pdf;

namespace Ecark.ApprovalTests.PDF;
public class PdfObjectExtractor
{

    public Dictionary<string, List<byte[]>> BytesByKey { get; } = new();

    private HashSet<PdfObject> _visited = new();
//uses dfs to traverse the pdf and grab all the feild names and associated data
    public void Extract(string pdfPath)
    {
        using var pdfDoc = new PdfDocument(new PdfReader(pdfPath));
        var catalog = pdfDoc.GetCatalog().GetPdfObject();
        var stack = new Stack<(PdfObject obj, string? parentKey)>();
        stack.Push((catalog, "Catalog"));

        while (stack.Count > 0)
        {
            var (obj, parentKey) = stack.Pop();

            if (obj == null || _visited.Contains(obj))
                continue;

            _visited.Add(obj);

            switch (obj)
            {
                case PdfDictionary dict:
                    SaveObjectBytes(parentKey, dict);

                    foreach (var key in dict.KeySet())
                    {
                        // pdf fields are named things like "/Creator", we want to know what data belongs where
                        // so we can ignore problematic fields
                        var childObj = dict.Get(key);
                        var keyName = key.GetValue(); 
                        AddBytes(keyName, GetRawBytes(childObj));
                        stack.Push((childObj, keyName));
                    }
                    break;

                case PdfArray array:
                    SaveObjectBytes(parentKey, array);

                    for (int i = 0; i < array.Size(); i++)
                    {
                        var item = array.Get(i);
                        stack.Push((item, parentKey));
                    }
                    break;

                case PdfIndirectReference indirect:
                    var resolved = indirect.GetRefersTo();
                    stack.Push((resolved, parentKey));
                    break;

                default:
                    SavePrimitiveBytes(parentKey, obj);
                    break;
            }
        }

        pdfDoc.Close();
    }

    private void AddBytes(string key, byte[] bytes)
    {
        if (!BytesByKey.TryGetValue(key, out var list))
        {
            list = new List<byte[]>();
            BytesByKey[key] = list;
        }

        list.Add(bytes);
    }

    private void SaveObjectBytes(string key, PdfObject obj)
    {
        if (obj == null || key == null) return;
        AddBytes(key, GetRawBytes(obj));
    }

    private void SaveStreamBytes(string key, PdfStream stream)
    {
        if (stream == null || key == null) return;
        AddBytes(key + "_Stream", stream.GetBytes());
    }

    private void SavePrimitiveBytes(string key, PdfObject obj)
    {
        if (obj == null || key == null) return;
        AddBytes(key, GetRawBytes(obj));
    }

    private byte[] GetRawBytes(PdfObject obj)
    {
        if (obj == null) return Array.Empty<byte>();
        return obj.ToString().ToByteArrayUtf8();
    }
}

public static class StringExtensions
{
    public static byte[] ToByteArrayUtf8(this string str) =>
        Encoding.UTF8.GetBytes(str);
}


