using ApprovalTests;
using ApprovalTests.Core;
using ApprovalTests.Writers;


namespace ApprovalTests.PDF;

public class PDFApprovals
{
    public static void VerifyPDFFile(string PDFFile, bool deleteOnSuccess = true)
    {
        var pdfApprover = new PDFApprover(new ExistingFileWriter(PDFFile), Approvals.GetDefaultNamer(), deleteOnSuccess);
        Approver.Verify(pdfApprover, Approvals.GetReporter());
    }

    public static void VerifyPDF(byte[] PDFBytes, string pathToUse = null)
    {
        var pdfApprover = new PDFApprover(new ApprovalBinaryWriter(PDFBytes, "pdf"), Approvals.GetDefaultNamer(), true, path: pathToUse);
        Approver.Verify(pdfApprover, Approvals.GetReporter());
    }
}